using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using System.Text.Json;

namespace SPINbuster.Application.Internal
{

  internal static class DocumentAuditStager
  {
    public static void Stage(IAuditRecorder auditRecorder, IEnumerable<AuditEvent> auditEvents)
    {
      foreach (var auditEvent in auditEvents)
      {
        auditRecorder.Stage(auditEvent);
      }
    }
  }

}

namespace SPINbuster.Application.UseCases.BeginDocumentImportSession
{

  public sealed record BeginDocumentImportSessionCommand(ProjectId ProjectId) : ICommand<BeginDocumentImportSessionResult>;

  public sealed record BeginDocumentImportSessionResult(
    DocumentImportSessionId ImportSessionId,
    ProjectId ProjectId,
    DocumentImportSessionState State,
    DateTimeOffset StartedAtUtc);

  public sealed class BeginDocumentImportSessionUseCase : ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult>
  {
    private readonly IAuditRecorder _auditRecorder;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentImportSessionRepository _importSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BeginDocumentImportSessionUseCase(
      IDocumentImportSessionRepository importSessionRepository,
      IUnitOfWork unitOfWork,
      IClock clock,
      ICurrentUser currentUser,
      IAuditRecorder auditRecorder)
    {
      _importSessionRepository = importSessionRepository;
      _unitOfWork = unitOfWork;
      _clock = clock;
      _currentUser = currentUser;
      _auditRecorder = auditRecorder;
    }

    public async Task<BeginDocumentImportSessionResult> HandleAsync(BeginDocumentImportSessionCommand command, CancellationToken cancellationToken = default)
    {
      var importSession = new DocumentImportSession(DocumentImportSessionId.New(), command.ProjectId, _currentUser.UserId.Value, _clock.UtcNow);
      await _importSessionRepository.AddAsync(importSession, cancellationToken);
      Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail);
      await _unitOfWork.CommitAsync(cancellationToken);
      return new BeginDocumentImportSessionResult(importSession.Id, importSession.ProjectId, importSession.State, importSession.StartedAtUtc);
    }
  }

}

namespace SPINbuster.Application.UseCases.ImportDocumentSource
{

  public sealed record ImportDocumentSourceCommand(
    DocumentImportSessionId ImportSessionId,
    ProjectId ProjectId,
    string OriginalFileName,
    string? DeclaredMediaType,
    ImportedSourceOrigin SourceOrigin,
    string? ExternalSourceReference,
    Stream Content) : ICommand<ImportDocumentSourceResult>;

  public sealed record ImportDocumentSourceResult(
    DocumentImportSessionId ImportSessionId,
    ImportedSourceId ImportedSourceId,
    bool ReusedExistingProjectSource,
    bool SameContentExistsInAnotherProject,
    StorageObjectId StorageObjectId,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    long ContentLength,
    string? DetectedMediaType,
    IReadOnlyList<string> Warnings);

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
      IAuditRecorder auditRecorder)
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
    }

    public async Task<ImportDocumentSourceResult> HandleAsync(ImportDocumentSourceCommand command, CancellationToken cancellationToken = default)
    {
      var importSession = await _importSessionRepository.GetByIdAsync(command.ImportSessionId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(DocumentImportSession), command.ImportSessionId.ToString());
      if (importSession.ProjectId != command.ProjectId)
      {
        throw new DomainInvariantException("Document import session project does not match the requested project.");
      }

      await using var content = await EnsureReplayableContentAsync(command.Content, cancellationToken);
      if (content.Length <= 0)
      {
        throw new DomainInvariantException("Imported content cannot be empty.");
      }

      if (content.Length > _documentImportPolicy.MaxContentLengthBytes)
      {
        throw new DomainInvariantException("Imported content exceeds the configured maximum content size.");
      }

      importSession.BeginValidation(_currentUser.UserId.Value, _clock.UtcNow);
      var inspection = await _importedContentInspector.InspectAsync(command.OriginalFileName, command.DeclaredMediaType, content.Length, cancellationToken);
      if (!inspection.IsSupported)
      {
        importSession.RecordRejectedSource(_currentUser.UserId.Value, _clock.UtcNow, "Imported content type is not supported by the current Document Engine foundation.");
        await _importSessionRepository.UpdateAsync(importSession, cancellationToken);
        Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail);
        await _unitOfWork.CommitAsync(cancellationToken);
        throw new DomainInvariantException("Imported content type is not supported by the current Document Engine foundation.");
      }

      content.Position = 0;
      var hashResult = await _contentHashService.ComputeAsync(content, cancellationToken);
      content.Position = 0;

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
        importSession.Complete(_currentUser.UserId.Value, _clock.UtcNow);
        await _importSessionRepository.UpdateAsync(importSession, cancellationToken);
        Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail);
        await _unitOfWork.CommitAsync(cancellationToken);

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

      var storageObject = await GetOrStoreStorageObjectAsync(hashResult, content, cancellationToken);
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
      importSession.Complete(_currentUser.UserId.Value, _clock.UtcNow);

      await _importedSourceRepository.AddAsync(importedSource, cancellationToken);
      await _importSessionRepository.UpdateAsync(importSession, cancellationToken);
      Internal.DocumentAuditStager.Stage(_auditRecorder, importedSource.AuditTrail);
      Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail);
      await _unitOfWork.CommitAsync(cancellationToken);

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

    private async Task<StorageObject> GetOrStoreStorageObjectAsync(ContentHashResult hashResult, MemoryStream content, CancellationToken cancellationToken)
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

    private static async Task<MemoryStream> EnsureReplayableContentAsync(Stream content, CancellationToken cancellationToken)
    {
      var bufferedContent = new MemoryStream();
      await content.CopyToAsync(bufferedContent, cancellationToken);
      bufferedContent.Position = 0;
      return bufferedContent;
    }
  }

}

namespace SPINbuster.Application.UseCases.LoadImportedDocumentSource
{

  public sealed record LoadImportedDocumentSourceQuery(ImportedSourceId ImportedSourceId) : IQuery<LoadImportedDocumentSourceResult>;

  public sealed record LoadImportedDocumentSourceResult(
    ImportedSourceId ImportedSourceId,
    DocumentImportSessionId ImportSessionId,
    ProjectId ProjectId,
    string OriginalFileName,
    string? DeclaredMediaType,
    string? DetectedMediaType,
    long ContentLength,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    StorageObjectId StorageObjectId,
    string StorageProviderKey,
    string ImmutableObjectKey,
    ImportedSourceOrigin SourceOrigin,
    ImportedDocumentSourceStatus Status);

  public sealed class LoadImportedDocumentSourceUseCase : IQueryHandler<LoadImportedDocumentSourceQuery, LoadImportedDocumentSourceResult>
  {
    private readonly IImportedDocumentSourceRepository _importedSourceRepository;

    public LoadImportedDocumentSourceUseCase(IImportedDocumentSourceRepository importedSourceRepository)
    {
      _importedSourceRepository = importedSourceRepository;
    }

    public async Task<LoadImportedDocumentSourceResult> HandleAsync(LoadImportedDocumentSourceQuery query, CancellationToken cancellationToken = default)
    {
      var source = await _importedSourceRepository.GetByIdAsync(query.ImportedSourceId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(ImportedDocumentSource), query.ImportedSourceId.ToString());

      return new LoadImportedDocumentSourceResult(
        source.Id,
        source.ImportSessionId,
        source.ProjectId,
        source.OriginalFileName,
        source.DeclaredMediaType,
        source.DetectedMediaType,
        source.ContentLength,
        source.ContentHash,
        source.HashAlgorithm,
        source.HashAlgorithmVersion,
        source.StorageReference.StorageObjectId,
        source.StorageReference.StorageProviderKey,
        source.StorageReference.ImmutableObjectKey,
        source.SourceOrigin,
        source.Status);
    }
  }

}

namespace SPINbuster.Application.UseCases.LoadDocumentImportSession
{

  public sealed record LoadDocumentImportSessionQuery(DocumentImportSessionId ImportSessionId) : IQuery<LoadDocumentImportSessionResult>;

  public sealed record LoadDocumentImportSessionResult(
    DocumentImportSessionId ImportSessionId,
    ProjectId ProjectId,
    string InitiatedBy,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DocumentImportSessionState State,
    int SourceCount,
    int AcceptedCount,
    int DuplicateCount,
    int RejectedCount,
    string? FailureSummary);

  public sealed class LoadDocumentImportSessionUseCase : IQueryHandler<LoadDocumentImportSessionQuery, LoadDocumentImportSessionResult>
  {
    private readonly IDocumentImportSessionRepository _importSessionRepository;

    public LoadDocumentImportSessionUseCase(IDocumentImportSessionRepository importSessionRepository)
    {
      _importSessionRepository = importSessionRepository;
    }

    public async Task<LoadDocumentImportSessionResult> HandleAsync(LoadDocumentImportSessionQuery query, CancellationToken cancellationToken = default)
    {
      var session = await _importSessionRepository.GetByIdAsync(query.ImportSessionId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(DocumentImportSession), query.ImportSessionId.ToString());

      return new LoadDocumentImportSessionResult(
        session.Id,
        session.ProjectId,
        session.InitiatedBy,
        session.StartedAtUtc,
        session.CompletedAtUtc,
        session.State,
        session.SourceCount,
        session.AcceptedCount,
        session.DuplicateCount,
        session.RejectedCount,
        session.FailureSummary);
    }
  }

}

namespace SPINbuster.Application.UseCases.RequestDocumentProcessing
{

  public sealed record RequestDocumentProcessingCommand(ImportedSourceId ImportedSourceId, ProjectId ProjectId) : ICommand<RequestDocumentProcessingResult>;

  public sealed record RequestDocumentProcessingResult(
    DocumentProcessingAttemptId ProcessingAttemptId,
    ImportedSourceId ImportedSourceId,
    DocumentProcessingAttemptState State,
    DocumentProcessingFailureClassification FailureClassification,
    int CandidateCount,
    IReadOnlyList<DocumentCandidateId> CandidateIds);

  public sealed class RequestDocumentProcessingUseCase : ICommandHandler<RequestDocumentProcessingCommand, RequestDocumentProcessingResult>
  {
    private readonly IAuditRecorder _auditRecorder;
    private readonly IClock _clock;
    private readonly IDocumentCandidateRepository _documentCandidateRepository;
    private readonly IDocumentImportPolicy _documentImportPolicy;
    private readonly IDocumentProcessingAttemptRepository _documentProcessingAttemptRepository;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly IImmutableContentStore _immutableContentStore;
    private readonly IImportedDocumentSourceRepository _importedSourceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RequestDocumentProcessingUseCase(
      IImportedDocumentSourceRepository importedSourceRepository,
      IDocumentProcessingAttemptRepository documentProcessingAttemptRepository,
      IDocumentCandidateRepository documentCandidateRepository,
      IImmutableContentStore immutableContentStore,
      IDocumentProcessor documentProcessor,
      IDocumentImportPolicy documentImportPolicy,
      IUnitOfWork unitOfWork,
      IClock clock,
      IAuditRecorder auditRecorder)
    {
      _importedSourceRepository = importedSourceRepository;
      _documentProcessingAttemptRepository = documentProcessingAttemptRepository;
      _documentCandidateRepository = documentCandidateRepository;
      _immutableContentStore = immutableContentStore;
      _documentProcessor = documentProcessor;
      _documentImportPolicy = documentImportPolicy;
      _unitOfWork = unitOfWork;
      _clock = clock;
      _auditRecorder = auditRecorder;
    }

    public async Task<RequestDocumentProcessingResult> HandleAsync(RequestDocumentProcessingCommand command, CancellationToken cancellationToken = default)
    {
      var source = await _importedSourceRepository.GetByIdAsync(command.ImportedSourceId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(ImportedDocumentSource), command.ImportedSourceId.ToString());
      if (source.ProjectId != command.ProjectId)
      {
        throw new DomainInvariantException("Imported source project does not match the requested project.");
      }

      var descriptor = _documentProcessor.Describe();
      var attempt = new DocumentProcessingAttempt(
        DocumentProcessingAttemptId.New(),
        source.Id,
        source.ProjectId,
        descriptor.ProcessorRole,
        descriptor.ProcessorIdentity,
        descriptor.ProcessorVersion,
        _clock.UtcNow,
        await _documentProcessingAttemptRepository.GetNextAttemptNumberAsync(source.Id, cancellationToken),
        source.ContentHash);
      attempt.Start(_clock.UtcNow);

      await _documentProcessingAttemptRepository.AddAsync(attempt, cancellationToken);
      Internal.DocumentAuditStager.Stage(_auditRecorder, attempt.AuditTrail);
      await _unitOfWork.CommitAsync(cancellationToken);

      var openResult = await _immutableContentStore.OpenReadAsync(source.StorageReference.StorageObjectId, cancellationToken);
      if (openResult.AvailabilityState != StorageAvailabilityState.Available)
      {
        attempt.Fail(_clock.UtcNow, DocumentProcessingFailureClassification.StorageUnavailable, "Stored content is not currently available.");
        await _documentProcessingAttemptRepository.UpdateAsync(attempt, cancellationToken);
        Internal.DocumentAuditStager.Stage(_auditRecorder, attempt.AuditTrail.Skip(2));
        await _unitOfWork.CommitAsync(cancellationToken);
        return new RequestDocumentProcessingResult(attempt.Id, source.Id, attempt.State, attempt.FailureClassification, 0, []);
      }

      await using var content = openResult.Content;
      var processorResult = await _documentProcessor.ProcessAsync(new DocumentProcessorRequest(
        source.Id,
        source.ProjectId,
        source.OriginalFileName,
        source.DeclaredMediaType,
        source.DetectedMediaType,
        source.ContentHash,
        source.HashAlgorithm,
        source.HashAlgorithmVersion,
        source.ContentLength,
        content), cancellationToken);

      var createdCandidates = new List<DocumentCandidate>();
      if (!processorResult.Success)
      {
        if (processorResult.FailureClassification == DocumentProcessingFailureClassification.Cancelled)
        {
          attempt.Cancel(_clock.UtcNow, processorResult.FailureDetails ?? "Document processing was cancelled.");
        }
        else if (processorResult.FailureClassification == DocumentProcessingFailureClassification.None)
        {
          attempt.Abstain(_clock.UtcNow, processorResult.FailureDetails ?? "Document processing abstained.");
        }
        else
        {
          attempt.Fail(_clock.UtcNow, processorResult.FailureClassification, processorResult.FailureDetails ?? "Document processing failed.");
        }
      }
      else
      {
        attempt.MarkOutputReceived(_clock.UtcNow, processorResult.RawOutputHash ?? source.ContentHash);
        attempt.BeginValidation(_clock.UtcNow);
        foreach (var candidateResult in processorResult.Candidates.Take(_documentImportPolicy.MaxCandidateQueryResults))
        {
          var canonicalPayload = Application.Internal.CanonicalJson.Canonicalize(candidateResult.Payload);
          var candidate = new DocumentCandidate(
            DocumentCandidateId.New(),
            source.ProjectId,
            source.Id,
            attempt.Id,
            candidateResult.CandidateType,
            candidateResult.SchemaId,
            candidateResult.SchemaVersion,
            canonicalPayload,
            source.ContentHash,
            candidateResult.SourceLocator,
            candidateResult.ConfidenceBand,
            candidateResult.UncertaintyCodes,
            _clock.UtcNow);
          switch (candidateResult.Outcome)
          {
            case DocumentProcessorCandidateOutcome.ReadyForReview:
              candidate.MarkValidated(_clock.UtcNow);
              candidate.MarkReadyForReview(_clock.UtcNow);
              break;
            case DocumentProcessorCandidateOutcome.SchemaRejected:
              candidate.MarkSchemaRejected(_clock.UtcNow, candidate.UncertaintyCodes);
              break;
            case DocumentProcessorCandidateOutcome.PolicyRejected:
              candidate.MarkPolicyRejected(_clock.UtcNow, candidate.UncertaintyCodes);
              break;
            case DocumentProcessorCandidateOutcome.Abstained:
              candidate.MarkAbstained(_clock.UtcNow, candidate.UncertaintyCodes);
              break;
            default:
              candidate.MarkFailed(_clock.UtcNow, candidate.UncertaintyCodes);
              break;
          }

          createdCandidates.Add(candidate);
        }

        attempt.Complete(_clock.UtcNow);
      }

      await _documentProcessingAttemptRepository.UpdateAsync(attempt, cancellationToken);
      foreach (var candidate in createdCandidates)
      {
        await _documentCandidateRepository.AddAsync(candidate, cancellationToken);
      }

      Internal.DocumentAuditStager.Stage(_auditRecorder, attempt.AuditTrail.Skip(2));
      foreach (var candidate in createdCandidates)
      {
        Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail);
      }
      await _unitOfWork.CommitAsync(cancellationToken);

      return new RequestDocumentProcessingResult(
        attempt.Id,
        source.Id,
        attempt.State,
        attempt.FailureClassification,
        createdCandidates.Count,
        createdCandidates.Select(candidate => candidate.Id).ToArray());
    }
  }

}

namespace SPINbuster.Application.UseCases.LoadDocumentProcessingHistory
{

  public sealed record LoadDocumentProcessingHistoryQuery(ImportedSourceId ImportedSourceId, int MaxResults) : IQuery<LoadDocumentProcessingHistoryResult>;

  public sealed record LoadDocumentProcessingHistoryResult(ImportedSourceId ImportedSourceId, IReadOnlyList<DocumentProcessingAttemptSnapshot> Attempts);

  public sealed record DocumentProcessingAttemptSnapshot(
    DocumentProcessingAttemptId ProcessingAttemptId,
    int AttemptNumber,
    string ProcessorRole,
    string ProcessorIdentity,
    string ProcessorVersion,
    DocumentProcessingAttemptState State,
    DocumentProcessingFailureClassification FailureClassification,
    string? FailureDetails,
    string InputContentHash,
    string? OutputHash,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

  public sealed class LoadDocumentProcessingHistoryUseCase : IQueryHandler<LoadDocumentProcessingHistoryQuery, LoadDocumentProcessingHistoryResult>
  {
    private readonly IDocumentImportPolicy _documentImportPolicy;
    private readonly IDocumentProcessingAttemptRepository _documentProcessingAttemptRepository;

    public LoadDocumentProcessingHistoryUseCase(
      IDocumentProcessingAttemptRepository documentProcessingAttemptRepository,
      IDocumentImportPolicy documentImportPolicy)
    {
      _documentProcessingAttemptRepository = documentProcessingAttemptRepository;
      _documentImportPolicy = documentImportPolicy;
    }

    public async Task<LoadDocumentProcessingHistoryResult> HandleAsync(LoadDocumentProcessingHistoryQuery query, CancellationToken cancellationToken = default)
    {
      if (query.MaxResults <= 0 || query.MaxResults > _documentImportPolicy.MaxProcessingHistoryResults)
      {
        throw new DomainInvariantException($"Processing history max results must be between 1 and {_documentImportPolicy.MaxProcessingHistoryResults}.");
      }

      var attempts = await _documentProcessingAttemptRepository.GetByImportedSourceAsync(query.ImportedSourceId, query.MaxResults, cancellationToken);
      return new LoadDocumentProcessingHistoryResult(
        query.ImportedSourceId,
        attempts.Select(attempt => new DocumentProcessingAttemptSnapshot(
          attempt.Id,
          attempt.AttemptNumber,
          attempt.ProcessorRole,
          attempt.ProcessorIdentity,
          attempt.ProcessorVersion,
          attempt.State,
          attempt.FailureClassification,
          attempt.FailureDetails,
          attempt.InputContentHash,
          attempt.OutputHash,
          attempt.RequestedAtUtc,
          attempt.StartedAtUtc,
          attempt.CompletedAtUtc)).ToArray());
    }
  }

}

namespace SPINbuster.Application.UseCases.LoadDocumentCandidates
{

  public sealed record LoadDocumentCandidatesQuery(
    ImportedSourceId? ImportedSourceId,
    DocumentProcessingAttemptId? ProcessingAttemptId,
    int MaxResults) : IQuery<LoadDocumentCandidatesResult>;

  public sealed record LoadDocumentCandidatesResult(IReadOnlyList<DocumentCandidateSnapshot> Candidates);

  public sealed record DocumentCandidateSnapshot(
    DocumentCandidateId DocumentCandidateId,
    ProjectId ProjectId,
    ImportedSourceId ImportedSourceId,
    DocumentProcessingAttemptId ProcessingAttemptId,
    DocumentCandidateType CandidateType,
    string SchemaId,
    string SchemaVersion,
    string PayloadHash,
    string CanonicalPayload,
    string SourceContentHash,
    string? SourceLocator,
    ConfidenceBand ConfidenceBand,
    IReadOnlyList<string> UncertaintyCodes,
    DocumentCandidateStatus Status,
    DateTimeOffset CreatedAtUtc,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAtUtc,
    string? ReviewNotes);

  public sealed class LoadDocumentCandidatesUseCase : IQueryHandler<LoadDocumentCandidatesQuery, LoadDocumentCandidatesResult>
  {
    private readonly IDocumentCandidateRepository _documentCandidateRepository;
    private readonly IDocumentImportPolicy _documentImportPolicy;

    public LoadDocumentCandidatesUseCase(
      IDocumentCandidateRepository documentCandidateRepository,
      IDocumentImportPolicy documentImportPolicy)
    {
      _documentCandidateRepository = documentCandidateRepository;
      _documentImportPolicy = documentImportPolicy;
    }

    public async Task<LoadDocumentCandidatesResult> HandleAsync(LoadDocumentCandidatesQuery query, CancellationToken cancellationToken = default)
    {
      if (query.ImportedSourceId is null && query.ProcessingAttemptId is null)
      {
        throw new DomainInvariantException("Either an imported source ID or processing attempt ID must be provided.");
      }

      if (query.MaxResults <= 0 || query.MaxResults > _documentImportPolicy.MaxCandidateQueryResults)
      {
        throw new DomainInvariantException($"Candidate max results must be between 1 and {_documentImportPolicy.MaxCandidateQueryResults}.");
      }

      IReadOnlyCollection<DocumentCandidate> candidates = query.ProcessingAttemptId is not null
        ? await _documentCandidateRepository.GetByProcessingAttemptAsync(query.ProcessingAttemptId.Value, query.MaxResults, cancellationToken)
        : await _documentCandidateRepository.GetByImportedSourceAsync(query.ImportedSourceId!.Value, query.MaxResults, cancellationToken);

      return new LoadDocumentCandidatesResult(
        candidates.Select(candidate => new DocumentCandidateSnapshot(
          candidate.Id,
          candidate.ProjectId,
          candidate.ImportedSourceId,
          candidate.ProcessingAttemptId,
          candidate.CandidateType,
          candidate.SchemaId,
          candidate.SchemaVersion,
          candidate.PayloadHash,
          candidate.CanonicalPayload,
          candidate.SourceContentHash,
          candidate.SourceLocator,
          candidate.ConfidenceBand,
          candidate.UncertaintyCodes,
          candidate.Status,
          candidate.CreatedAtUtc,
          candidate.ReviewedBy,
          candidate.ReviewedAtUtc,
          candidate.ReviewNotes)).ToArray());
    }
  }

}

namespace SPINbuster.Application.UseCases.RejectDocumentCandidate
{

  public sealed record RejectDocumentCandidateCommand(DocumentCandidateId DocumentCandidateId, string? ReviewNotes) : ICommand<RejectDocumentCandidateResult>;

  public sealed record RejectDocumentCandidateResult(
    DocumentCandidateId DocumentCandidateId,
    DocumentCandidateStatus Status,
    string Reviewer,
    DateTimeOffset ReviewedAtUtc);

  public sealed class RejectDocumentCandidateUseCase : ICommandHandler<RejectDocumentCandidateCommand, RejectDocumentCandidateResult>
  {
    private readonly IAuditRecorder _auditRecorder;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentCandidateRepository _documentCandidateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectDocumentCandidateUseCase(
      IDocumentCandidateRepository documentCandidateRepository,
      IUnitOfWork unitOfWork,
      IClock clock,
      ICurrentUser currentUser,
      IAuditRecorder auditRecorder)
    {
      _documentCandidateRepository = documentCandidateRepository;
      _unitOfWork = unitOfWork;
      _clock = clock;
      _currentUser = currentUser;
      _auditRecorder = auditRecorder;
    }

    public async Task<RejectDocumentCandidateResult> HandleAsync(RejectDocumentCandidateCommand command, CancellationToken cancellationToken = default)
    {
      var candidate = await _documentCandidateRepository.GetByIdAsync(command.DocumentCandidateId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(DocumentCandidate), command.DocumentCandidateId.ToString());
      var priorAuditCount = candidate.AuditTrail.Count;
      candidate.Reject(_currentUser.UserId.Value, _clock.UtcNow, command.ReviewNotes);
      await _documentCandidateRepository.UpdateAsync(candidate, cancellationToken);
      Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail.Skip(priorAuditCount));
      await _unitOfWork.CommitAsync(cancellationToken);
      return new RejectDocumentCandidateResult(candidate.Id, candidate.Status, _currentUser.UserId.Value, candidate.ReviewedAtUtc!.Value);
    }
  }

}

namespace SPINbuster.Application.UseCases.RecordDocumentCandidateReview
{

  public sealed record RecordDocumentCandidateReviewCommand(
    DocumentCandidateId DocumentCandidateId,
    DocumentCandidateReviewDisposition Disposition,
    string? ReviewNotes) : ICommand<RecordDocumentCandidateReviewResult>;

  public enum DocumentCandidateReviewDisposition
  {
    HumanAccepted = 0,
    Rejected = 1,
  }

  public sealed record RecordDocumentCandidateReviewResult(
    DocumentCandidateId DocumentCandidateId,
    DocumentCandidateStatus Status,
    string Reviewer,
    DateTimeOffset ReviewedAtUtc);

  public sealed class RecordDocumentCandidateReviewUseCase : ICommandHandler<RecordDocumentCandidateReviewCommand, RecordDocumentCandidateReviewResult>
  {
    private readonly IAuditRecorder _auditRecorder;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentCandidateRepository _documentCandidateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordDocumentCandidateReviewUseCase(
      IDocumentCandidateRepository documentCandidateRepository,
      IUnitOfWork unitOfWork,
      IClock clock,
      ICurrentUser currentUser,
      IAuditRecorder auditRecorder)
    {
      _documentCandidateRepository = documentCandidateRepository;
      _unitOfWork = unitOfWork;
      _clock = clock;
      _currentUser = currentUser;
      _auditRecorder = auditRecorder;
    }

    public async Task<RecordDocumentCandidateReviewResult> HandleAsync(RecordDocumentCandidateReviewCommand command, CancellationToken cancellationToken = default)
    {
      var candidate = await _documentCandidateRepository.GetByIdAsync(command.DocumentCandidateId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(DocumentCandidate), command.DocumentCandidateId.ToString());
      var priorAuditCount = candidate.AuditTrail.Count;
      if (command.Disposition == DocumentCandidateReviewDisposition.HumanAccepted)
      {
        candidate.Accept(_currentUser.UserId.Value, _clock.UtcNow, command.ReviewNotes);
      }
      else
      {
        candidate.Reject(_currentUser.UserId.Value, _clock.UtcNow, command.ReviewNotes);
      }

      await _documentCandidateRepository.UpdateAsync(candidate, cancellationToken);
      Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail.Skip(priorAuditCount));
      await _unitOfWork.CommitAsync(cancellationToken);
      return new RecordDocumentCandidateReviewResult(candidate.Id, candidate.Status, _currentUser.UserId.Value, candidate.ReviewedAtUtc!.Value);
    }
  }
}
