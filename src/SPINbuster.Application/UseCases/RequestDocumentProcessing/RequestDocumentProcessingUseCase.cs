using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestDocumentProcessing;

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
  private readonly ILogger<RequestDocumentProcessingUseCase> _logger;
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
    IAuditRecorder auditRecorder,
    ILogger<RequestDocumentProcessingUseCase> logger)
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
    _logger = logger;
  }

  public async Task<RequestDocumentProcessingResult> HandleAsync(RequestDocumentProcessingCommand command, CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    var useCaseName = nameof(RequestDocumentProcessingUseCase);
    var importedSourceId = command.ImportedSourceId.ToString();
    var projectId = command.ProjectId.ToString();
    var descriptor = _documentProcessor.Describe();
    var processorKey = descriptor.ProcessorIdentity;

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.ImportedSourceId] = importedSourceId,
      [LogProperties.ProjectId] = projectId,
      [LogProperties.ProcessorKey] = processorKey,
    }))
    {
      _logger.LogInformation(LogEvents.DocumentProcessingStarting,
        "{UseCase} starting for imported source {ImportedSourceId}, project {ProjectId}, processor {ProcessorKey}",
        useCaseName, importedSourceId, projectId, processorKey);

      var source = await _importedSourceRepository.GetByIdAsync(command.ImportedSourceId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(ImportedDocumentSource), command.ImportedSourceId.ToString());
      if (source.ProjectId != command.ProjectId)
      {
        throw new DomainInvariantException("Imported source project does not match the requested project.");
      }

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

      try
      {
        var openResult = await _immutableContentStore.OpenReadAsync(source.StorageReference.StorageObjectId, cancellationToken);
        if (openResult.AvailabilityState != StorageAvailabilityState.Available)
        {
          attempt.Fail(_clock.UtcNow, DocumentProcessingFailureClassification.StorageUnavailable, "Stored content is not currently available.");
          await PersistTerminalAttemptStateAsync(attempt, [], cancellationToken);
          stopwatch.Stop();
          _logger.LogWarning(LogEvents.DocumentProcessingFailed,
            "{UseCase} storage unavailable in {DurationMs}ms for imported source {ImportedSourceId}, attempt {AttemptId}",
            useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, attempt.Id);
          return CreateResult(attempt, source.Id, []);
        }

        if (!string.Equals(openResult.ContentHash, source.ContentHash, StringComparison.Ordinal)
          || !string.Equals(openResult.HashAlgorithm, source.HashAlgorithm, StringComparison.Ordinal)
          || openResult.HashAlgorithmVersion != source.HashAlgorithmVersion
          || openResult.ContentLength != source.ContentLength)
        {
          attempt.Fail(_clock.UtcNow, DocumentProcessingFailureClassification.ValidationFailed, "Stored content no longer matches the authoritative immutable identity.");
          await PersistTerminalAttemptStateAsync(attempt, [], cancellationToken);
          stopwatch.Stop();
          _logger.LogWarning(LogEvents.DocumentProcessingFailed,
            "{UseCase} validation failed in {DurationMs}ms for imported source {ImportedSourceId}, attempt {AttemptId}",
            useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, attempt.Id);
          return CreateResult(attempt, source.Id, []);
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
            // Canonicalize once at the application boundary so provider quirks do not leak into durable storage.
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
        }

        await PersistTerminalAttemptStateAsync(attempt, createdCandidates, cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation(LogEvents.DocumentProcessingCompleted,
          "{UseCase} completed in {DurationMs}ms for imported source {ImportedSourceId}, attempt {AttemptId}, state {AttemptState}, candidates {CandidateCount}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, attempt.Id, attempt.State, createdCandidates.Count);
        return CreateResult(attempt, source.Id, createdCandidates);
      }
      catch (OperationCanceledException)
      {
        attempt.Cancel(_clock.UtcNow, "Document processing request was cancelled.");
        await PersistTerminalAttemptStateAsync(attempt, [], CancellationToken.None, finalizeSuccessfulValidation: false);
        stopwatch.Stop();
        _logger.LogWarning(LogEvents.DocumentProcessingCancelled,
          "{UseCase} cancelled in {DurationMs}ms for imported source {ImportedSourceId}, attempt {AttemptId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, attempt.Id);
        return CreateResult(attempt, source.Id, []);
      }
      catch (ImmutableContentStoreException exception) when (exception.FailureClassification == ImmutableContentStoreFailureClassification.IntegrityMismatch)
      {
        if (attempt.State is not DocumentProcessingAttemptState.Completed
          and not DocumentProcessingAttemptState.Failed
          and not DocumentProcessingAttemptState.Cancelled
          and not DocumentProcessingAttemptState.Abstained)
        {
          attempt.Fail(_clock.UtcNow, DocumentProcessingFailureClassification.ValidationFailed, "Stored content integrity verification failed.");
        }

        await PersistTerminalAttemptStateAsync(attempt, [], CancellationToken.None, finalizeSuccessfulValidation: false);
        stopwatch.Stop();
        _logger.LogError(LogEvents.DocumentProcessingFailed,
          exception,
          "{UseCase} integrity mismatch in {DurationMs}ms for imported source {ImportedSourceId}, attempt {AttemptId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, attempt.Id);
        return CreateResult(attempt, source.Id, []);
      }
      catch (ImmutableContentStoreException exception)
      {
        if (attempt.State is not DocumentProcessingAttemptState.Completed
          and not DocumentProcessingAttemptState.Failed
          and not DocumentProcessingAttemptState.Cancelled
          and not DocumentProcessingAttemptState.Abstained)
        {
          attempt.Fail(_clock.UtcNow, DocumentProcessingFailureClassification.StorageUnavailable, $"Stored content is unavailable: {exception.Message}");
        }

        await PersistTerminalAttemptStateAsync(attempt, [], CancellationToken.None, finalizeSuccessfulValidation: false);
        stopwatch.Stop();
        _logger.LogError(LogEvents.DocumentProcessingFailed,
          exception,
          "{UseCase} storage unavailable in {DurationMs}ms for imported source {ImportedSourceId}, attempt {AttemptId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, attempt.Id);
        return CreateResult(attempt, source.Id, []);
      }
      catch (Exception exception)
      {
        if (attempt.State is not DocumentProcessingAttemptState.Completed
          and not DocumentProcessingAttemptState.Failed
          and not DocumentProcessingAttemptState.Cancelled
          and not DocumentProcessingAttemptState.Abstained)
        {
          attempt.Fail(_clock.UtcNow, DocumentProcessingFailureClassification.Unknown, $"Unexpected document processing failure: {exception.Message}");
        }

        await PersistTerminalAttemptStateAsync(attempt, [], CancellationToken.None, finalizeSuccessfulValidation: false);
        stopwatch.Stop();
        _logger.LogError(LogEvents.DocumentProcessingFailed,
          exception,
          "{UseCase} unexpected failure in {DurationMs}ms for imported source {ImportedSourceId}, attempt {AttemptId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, attempt.Id);
        return CreateResult(attempt, source.Id, []);
      }
    }
  }

  private async Task PersistTerminalAttemptStateAsync(
    DocumentProcessingAttempt attempt,
    List<DocumentCandidate> createdCandidates,
    CancellationToken cancellationToken,
    bool finalizeSuccessfulValidation = true)
  {
    foreach (var candidate in createdCandidates)
    {
      await _documentCandidateRepository.AddAsync(candidate, cancellationToken);
    }

    if (finalizeSuccessfulValidation && attempt.State == DocumentProcessingAttemptState.Validating)
    {
      // Successful processing is only finalized after candidate persistence has been staged.
      attempt.Complete(_clock.UtcNow);
    }

    await _documentProcessingAttemptRepository.UpdateAsync(attempt, cancellationToken);

    // The initial request/start audit events were committed before provider work began.
    Internal.DocumentAuditStager.Stage(_auditRecorder, attempt.AuditTrail.Skip(2));
    foreach (var candidate in createdCandidates)
    {
      Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail);
    }
    await _unitOfWork.CommitAsync(cancellationToken);
  }

  private static RequestDocumentProcessingResult CreateResult(
    DocumentProcessingAttempt attempt,
    ImportedSourceId importedSourceId,
    List<DocumentCandidate> createdCandidates)
  {
    return new RequestDocumentProcessingResult(
      attempt.Id,
      importedSourceId,
      attempt.State,
      attempt.FailureClassification,
      createdCandidates.Count,
      createdCandidates.Select(candidate => candidate.Id).ToArray());
  }
}
