using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.ImportDocumentSource;

public sealed class ImportDocumentSourceUseCase : ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IContentHashService _contentHashService;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IDocumentImportPolicy _documentImportPolicy;
  private readonly IDocumentImportSessionRepository _importSessionRepository;
  private readonly IImportedContentInspector _importedContentInspector;
  private readonly IImportedDocumentSourceRepository _importedSourceRepository;
  private readonly IImmutableContentStore _immutableContentStore;
  private readonly ILogger<ImportDocumentSourceUseCase> _logger;
  private readonly IStorageObjectRepository _storageObjectRepository;
  private readonly IUnitOfWork _unitOfWork;

  public ImportDocumentSourceUseCase(
    IDocumentImportSessionRepository importSessionRepository,
    IImportedDocumentSourceRepository importedSourceRepository,
    IStorageObjectRepository storageObjectRepository,
    IImmutableContentStore immutableContentStore,
    IContentHashService contentHashService,
    IImportedContentInspector importedContentInspector,
    IDocumentImportPolicy documentImportPolicy,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder,
    ILogger<ImportDocumentSourceUseCase> logger)
  {
    _importSessionRepository = importSessionRepository;
    _importedSourceRepository = importedSourceRepository;
    _storageObjectRepository = storageObjectRepository;
    _immutableContentStore = immutableContentStore;
    _contentHashService = contentHashService;
    _importedContentInspector = importedContentInspector;
    _documentImportPolicy = documentImportPolicy;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
    _logger = logger;
  }

  public async Task<ImportDocumentSourceResult> HandleAsync(ImportDocumentSourceCommand command, CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    var useCaseName = nameof(ImportDocumentSourceUseCase);
    var importSessionId = command.ImportSessionId.ToString();
    var projectId = command.ProjectId.ToString();

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.ImportSessionId] = importSessionId,
      [LogProperties.ProjectId] = projectId,
      [LogProperties.FileName] = command.OriginalFileName,
      [LogProperties.DeclaredMediaType] = command.DeclaredMediaType ?? "(none)",
      [LogProperties.ApplicationUserId] = _currentUser.UserId.Value,
    }))
    {
      _logger.LogInformation(LogEvents.DocumentImportStarting,
        "{UseCase} starting for import session {ImportSessionId}, project {ProjectId}, file {FileName}, media type {DeclaredMediaType}",
        useCaseName, importSessionId, projectId, command.OriginalFileName, command.DeclaredMediaType ?? "(none)");

      var importSession = await _importSessionRepository.GetByIdAsync(command.ImportSessionId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(DocumentImportSession), command.ImportSessionId.ToString());
      if (importSession.ProjectId != command.ProjectId)
      {
        throw new DomainInvariantException("Document import session project does not match the requested project.");
      }

      var priorImportSessionAuditCount = importSession.AuditTrail.Count;

      // Bounded buffering: consumes input incrementally with early size enforcement,
      // then retains the bytes entirely for replay by the import pipeline.
      await using var importBuffer = await StreamingImportProcessor.ProcessAsync(
        command.Content,
        _documentImportPolicy.MaxContentLengthBytes,
        cancellationToken);

      importSession.BeginValidation(_currentUser.UserId.Value, _clock.UtcNow);
      var inspection = await _importedContentInspector.InspectAsync(command.OriginalFileName, command.DeclaredMediaType, importBuffer.ContentLength, cancellationToken);
      if (!inspection.IsSupported)
      {
        importSession.RecordRejectedSource(_currentUser.UserId.Value, _clock.UtcNow, "Imported content type is not supported by the current Document Engine foundation.");
        await _importSessionRepository.UpdateAsync(importSession, cancellationToken);
        Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail.Skip(priorImportSessionAuditCount));
        await _unitOfWork.CommitAsync(cancellationToken);
        throw new DomainInvariantException("Imported content type is not supported by the current Document Engine foundation.");
      }

      importBuffer.Content.Position = 0;
      var hashResult = await _contentHashService.ComputeAsync(importBuffer.Content, cancellationToken);
      importBuffer.Content.Position = 0;

      importSession.BeginImporting(_currentUser.UserId.Value, _clock.UtcNow);
      var existingProjectSource = await _importedSourceRepository.GetByProjectAndContentHashAsync(
        command.ProjectId,
        hashResult.ContentHash,
        hashResult.HashAlgorithm,
        hashResult.HashAlgorithmVersion,
        cancellationToken);
      var crossProjectDuplicateExists = await _importedSourceRepository.ExistsInOtherProjectsAsync(
        command.ProjectId,
        hashResult.ContentHash,
        hashResult.HashAlgorithm,
        hashResult.HashAlgorithmVersion,
        cancellationToken);

      if (existingProjectSource is not null)
      {
        importSession.RecordDuplicateSource(existingProjectSource.Id, _currentUser.UserId.Value, _clock.UtcNow);
        await _importSessionRepository.UpdateAsync(importSession, cancellationToken);
        Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail.Skip(priorImportSessionAuditCount));
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(LogEvents.DocumentImportDuplicateDetected,
          "{UseCase} duplicate detected in {DurationMs}ms for import session {ImportSessionId}, reused source {ImportedSourceId}, content hash {ContentHash}",
          useCaseName, stopwatch.ElapsedMilliseconds, importSessionId, existingProjectSource.Id, SensitiveDataRules.TruncateHash(hashResult.ContentHash));

        return new ImportDocumentSourceResult(
          importSession.Id,
          existingProjectSource.Id,
          true,
          crossProjectDuplicateExists,
          existingProjectSource.StorageReference.StorageObjectId,
          existingProjectSource.ContentHash,
          existingProjectSource.HashAlgorithm,
          existingProjectSource.HashAlgorithmVersion,
          existingProjectSource.ContentLength,
          existingProjectSource.DetectedMediaType,
          inspection.Warnings);
      }

      var storageObject = await GetOrStoreStorageObjectAsync(hashResult, importBuffer.Content, cancellationToken);
      var importedSource = new ImportedDocumentSource(
        ImportedSourceId.New(),
        importSession.Id,
        command.ProjectId,
        inspection.NormalizedFileName,
        inspection.NormalizedDeclaredMediaType,
        inspection.DetectedMediaType,
        hashResult.ContentLength,
        hashResult.ContentHash,
        hashResult.HashAlgorithm,
        hashResult.HashAlgorithmVersion,
        storageObject.ToReference(),
        command.SourceOrigin,
        _currentUser.UserId.Value,
        _clock.UtcNow,
        ImportedDocumentSourceStatus.Available,
        command.ExternalSourceReference);
      importSession.RecordAcceptedSource(importedSource.Id, _currentUser.UserId.Value, _clock.UtcNow);

      await _importedSourceRepository.AddAsync(importedSource, cancellationToken);
      await _importSessionRepository.UpdateAsync(importSession, cancellationToken);
      Internal.DocumentAuditStager.Stage(_auditRecorder, importedSource.AuditTrail);
      Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail.Skip(priorImportSessionAuditCount));
      await _unitOfWork.CommitAsync(cancellationToken);

      stopwatch.Stop();
      _logger.LogInformation(LogEvents.DocumentImportCompleted,
        "{UseCase} completed in {DurationMs}ms for import session {ImportSessionId}, imported source {ImportedSourceId}, content hash {ContentHash}, cross-project duplicate {CrossProjectDuplicateExists}",
        useCaseName, stopwatch.ElapsedMilliseconds, importSessionId, importedSource.Id, SensitiveDataRules.TruncateHash(hashResult.ContentHash), crossProjectDuplicateExists);

      return new ImportDocumentSourceResult(
        importSession.Id,
        importedSource.Id,
        false,
        crossProjectDuplicateExists,
        storageObject.Id,
        importedSource.ContentHash,
        importedSource.HashAlgorithm,
        importedSource.HashAlgorithmVersion,
        importedSource.ContentLength,
        importedSource.DetectedMediaType,
        inspection.Warnings);
    }
  }

  private async Task<StorageObject> GetOrStoreStorageObjectAsync(ContentHashResult hashResult, Stream content, CancellationToken cancellationToken)
  {
    var existingStorageObject = await _storageObjectRepository.GetByContentHashAsync(
      hashResult.ContentHash,
      hashResult.HashAlgorithm,
      hashResult.HashAlgorithmVersion,
      cancellationToken);
    if (existingStorageObject is not null)
    {
      return existingStorageObject;
    }

    var storageObjectId = StorageObjectId.New();
    content.Position = 0;
    var storedContent = await _immutableContentStore.StoreAsync(new StoreImmutableContentRequest(
      storageObjectId,
      "document-engine-foundation",
      storageObjectId.ToString(),
      hashResult.ContentHash,
      hashResult.HashAlgorithm,
      hashResult.HashAlgorithmVersion,
      hashResult.ContentLength,
      content,
      _clock.UtcNow,
      null), cancellationToken);

    var storageObject = new StorageObject(
      storedContent.StorageObjectId,
      storedContent.StorageProviderKey,
      storedContent.ImmutableObjectKey,
      storedContent.ContentLength,
      storedContent.ContentHash,
      storedContent.HashAlgorithm,
      storedContent.HashAlgorithmVersion,
      storedContent.CreatedAtUtc,
      storedContent.EncryptionMetadataId,
      storedContent.AvailabilityState);
    await _storageObjectRepository.AddAsync(storageObject, cancellationToken);
    return storageObject;
  }
}
