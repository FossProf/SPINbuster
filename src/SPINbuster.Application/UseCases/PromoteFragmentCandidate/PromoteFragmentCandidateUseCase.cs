using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.PromoteFragmentCandidate;

public sealed class PromoteFragmentCandidateUseCase
  : ICommandHandler<PromoteFragmentCandidateCommand, PromoteFragmentCandidateResult>
{
  private const string PromotionContractVersion = PromotionIdentity.ContractVersion;

  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IFragmentCandidateRepository _fragmentCandidateRepository;
  private readonly IImportedDocumentSourceRepository _importedDocumentSourceRepository;
  private readonly IKnowledgeCitationRepository _knowledgeCitationRepository;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRelationshipRepository _knowledgeRelationshipRepository;
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;
  private readonly ILogger<PromoteFragmentCandidateUseCase> _logger;
  private readonly IParserRunRepository _parserRunRepository;
  private readonly IProjectRepository _projectRepository;
  private readonly IPromotionAttemptRepository _promotionAttemptRepository;
  private readonly IPromotionDiagnosticRepository _promotionDiagnosticRepository;
  private readonly IPromotionRecordRepository _promotionRecordRepository;
  private readonly IUnitOfWork _unitOfWork;

  public PromoteFragmentCandidateUseCase(
    IFragmentCandidateRepository fragmentCandidateRepository,
    IParserRunRepository parserRunRepository,
    IImportedDocumentSourceRepository importedDocumentSourceRepository,
    IProjectRepository projectRepository,
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IKnowledgeRevisionRepository knowledgeRevisionRepository,
    IKnowledgeCitationRepository knowledgeCitationRepository,
    IKnowledgeRelationshipRepository knowledgeRelationshipRepository,
    IPromotionDiagnosticRepository promotionDiagnosticRepository,
    IPromotionRecordRepository promotionRecordRepository,
    IPromotionAttemptRepository promotionAttemptRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder,
    ILogger<PromoteFragmentCandidateUseCase> logger)
  {
    _fragmentCandidateRepository = fragmentCandidateRepository;
    _parserRunRepository = parserRunRepository;
    _importedDocumentSourceRepository = importedDocumentSourceRepository;
    _projectRepository = projectRepository;
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _knowledgeRevisionRepository = knowledgeRevisionRepository;
    _knowledgeCitationRepository = knowledgeCitationRepository;
    _knowledgeRelationshipRepository = knowledgeRelationshipRepository;
    _promotionDiagnosticRepository = promotionDiagnosticRepository;
    _promotionRecordRepository = promotionRecordRepository;
    _promotionAttemptRepository = promotionAttemptRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
    _logger = logger;
  }

  public async Task<PromoteFragmentCandidateResult> HandleAsync(
    PromoteFragmentCandidateCommand command,
    CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    var useCaseName = nameof(PromoteFragmentCandidateUseCase);
    var candidateId = command.FragmentCandidateId.ToString();

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.FragmentCandidateId] = candidateId,
    }))
    {
      _logger.LogInformation(
        "{UseCase} starting for fragment candidate {FragmentCandidateId}",
        useCaseName, candidateId);

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        command.FragmentCandidateId,
        ParserRunId.New(),
        ProjectId.New(),
        _clock.UtcNow);

      FragmentCandidate candidate = null!;
      var recordId = PromotionRecordId.New();

      try
      {
        candidate = await _fragmentCandidateRepository.GetByIdAsync(command.FragmentCandidateId, cancellationToken)
          ?? throw new ApplicationEntityNotFoundException(nameof(FragmentCandidate), candidateId);

        var parserRun = await _parserRunRepository.GetByIdAsync(candidate.ParserRunId, cancellationToken)
          ?? throw new ApplicationEntityNotFoundException(nameof(ParserRun), candidate.ParserRunId.ToString());

        var importedSource = await _importedDocumentSourceRepository.GetByIdAsync(candidate.ImportedSourceId, cancellationToken)
          ?? throw new ApplicationEntityNotFoundException(nameof(ImportedDocumentSource), candidate.ImportedSourceId.ToString());

        var project = await _projectRepository.GetByIdAsync(candidate.ProjectId, cancellationToken)
          ?? throw new ApplicationEntityNotFoundException(nameof(Project), candidate.ProjectId.ToString());

        diagnostic = new PromotionDiagnostic(
          diagnostic.Id,
          candidate.Id,
          parserRun.Id,
          project.Id,
          diagnostic.PromotedAtUtc);

        var identity = new PromotionIdentity(
          candidate.ProjectId,
          command.DocumentType,
          command.CanonicalTitle,
          command.ExternalReferenceNumber,
          command.DisciplineOrCategory,
          candidate.IdentityKey);

        var identityReplay = await TryReplayByIdentityAsync(identity, cancellationToken);
        if (identityReplay is not null)
        {
          stopwatch.Stop();
          _logger.LogInformation(
            "{UseCase} completed (replay by identity) in {DurationMs}ms for fragment candidate {FragmentCandidateId}",
            useCaseName, stopwatch.ElapsedMilliseconds, candidateId);

          return identityReplay;
        }

        ValidatePreconditions(candidate, parserRun, importedSource, project);

        var documentMetadataHash = ComputeMetadataHash(command.DocumentType, command.CanonicalTitle, command.ExternalReferenceNumber, command.DisciplineOrCategory);
        var knowledgeDocument = await FindOrCreateKnowledgeDocumentAsync(
          candidate.ProjectId,
          command.DocumentType,
          command.CanonicalTitle,
          command.ExternalReferenceNumber,
          command.DisciplineOrCategory,
          cancellationToken);

        var supersededRevisionId = knowledgeDocument.CurrentAuthoritativeRevisionId;
        var supersededExistingRevision = supersededRevisionId is not null;

        var revisionLabel = $"v1-parsed-{candidate.Ordinal}-{candidate.Id.Value.ToString("N")[..8]}";
        var knowledgeSourceId = KnowledgeSourceId.New();

        var auditCountBeforeDomainMutation = knowledgeDocument.AuditTrail.Count;

        var knowledgeRevision = new KnowledgeDocumentRevision(
          KnowledgeDocumentRevisionId.New(),
          knowledgeDocument.Id,
          knowledgeSourceId,
          revisionLabel,
          null,
          _clock.UtcNow,
          KnowledgeSourceAuthorityLevel.Informational,
          candidate.SourceContentHash,
          documentMetadataHash,
          supersededRevisionId,
          null,
          null,
          _clock.UtcNow);

        KnowledgeDocumentRevision? supersededDomainRevision = null;

        if (supersededExistingRevision)
        {
          var outcome = knowledgeDocument.SupersedeCurrentRevision(
            knowledgeRevision,
            _currentUser.UserId.Value,
            _clock.UtcNow);

          supersededDomainRevision = outcome.SupersededRevision;
        }
        else
        {
          knowledgeDocument.AddInitialRevision(
            knowledgeRevision,
            _currentUser.UserId.Value,
            _clock.UtcNow);
        }

        var citation = new KnowledgeCitation(
          KnowledgeCitationId.New(),
          knowledgeRevision.Id,
          MapLocatorType(candidate.Locator.LocatorType),
          candidate.Locator.NormalizedValue.Length > 0 ? candidate.Locator.NormalizedValue : candidate.Locator.RawValue,
          candidate.SourceContentHash,
          _clock.UtcNow,
          null);

        var citationDuplicate = await _knowledgeCitationRepository.GetByRevisionIdAsync(knowledgeRevision.Id, cancellationToken);
        if (citationDuplicate.Any(c =>
          c.LocatorType == citation.LocatorType
          && string.Equals(c.LocatorValue, citation.LocatorValue, StringComparison.Ordinal)))
        {
          throw new DomainInvariantException(
            $"Duplicate citation for locator {citation.LocatorType}:{citation.LocatorValue} on revision {knowledgeRevision.Id} is not allowed.");
        }

        var derivedFromSource = KnowledgeSubjectReference.ForRevision(candidate.ProjectId, knowledgeRevision.Id);
        var derivedFromTarget = KnowledgeSubjectReference.ForDocument(candidate.ProjectId, knowledgeDocument.Id);
        var existingDerivedFrom = await _knowledgeRelationshipRepository.FindByEndpointsAsync(
          candidate.ProjectId,
          derivedFromSource,
          derivedFromTarget,
          KnowledgeRelationshipType.DerivedFrom,
          cancellationToken);

        if (existingDerivedFrom is null)
        {
          var derivedFromRelationship = new KnowledgeRelationship(
            KnowledgeRelationshipId.New(),
            candidate.ProjectId,
            derivedFromSource,
            derivedFromTarget,
            KnowledgeRelationshipType.DerivedFrom,
            $"Promoted from fragment candidate {candidate.Id} produced by parser {parserRun.ParserKey}@{parserRun.ParserContractVersion}",
            _currentUser.UserId.Value,
            _clock.UtcNow);

          await _knowledgeRelationshipRepository.AddAsync(derivedFromRelationship, cancellationToken);
          StageAuditEvents(derivedFromRelationship.AuditTrail);
        }

        await _knowledgeDocumentRepository.UpdateAsync(knowledgeDocument, cancellationToken);
        if (supersededDomainRevision is not null)
        {
          await _knowledgeRevisionRepository.UpdateAsync(supersededDomainRevision, cancellationToken);
        }

        await _knowledgeRevisionRepository.AddAsync(knowledgeRevision, cancellationToken);
        await _knowledgeCitationRepository.AddAsync(citation, cancellationToken);

        diagnostic.RecordSuccess(
          knowledgeDocument.Id,
          knowledgeRevision.Id,
          citation.Id,
          supersededExistingRevision,
          supersededRevisionId);

        var promotionRecord = new PromotionRecord(
          recordId,
          identity,
          knowledgeDocument.Id,
          _clock.UtcNow);

        var promotionAttempt = new PromotionAttempt(
          PromotionAttemptId.New(),
          recordId,
          PromotionAttemptOutcome.Promoted,
          diagnostic.Id,
          command.FragmentCandidateId,
          candidate.SourceContentHash,
          _clock.UtcNow);

        promotionRecord.UpdateLatestAttempt(promotionAttempt.Id);

        await _promotionDiagnosticRepository.AddAsync(diagnostic, cancellationToken);
        await _promotionRecordRepository.AddAsync(promotionRecord, cancellationToken);
        await _promotionAttemptRepository.AddAsync(promotionAttempt, cancellationToken);
        StageAuditEvents(knowledgeDocument.AuditTrail.Skip(auditCountBeforeDomainMutation));
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
          "{UseCase} completed in {DurationMs}ms for fragment candidate {FragmentCandidateId}, document {KnowledgeDocumentId}, revision {KnowledgeDocumentRevisionId}",
          useCaseName, stopwatch.ElapsedMilliseconds, candidateId, knowledgeDocument.Id, knowledgeRevision.Id);

        return new PromoteFragmentCandidateResult(
          diagnostic.Id,
          diagnostic.Status,
          diagnostic.KnowledgeDocumentId,
          diagnostic.KnowledgeDocumentRevisionId,
          diagnostic.KnowledgeCitationId,
          diagnostic.SupersededExistingRevision,
          diagnostic.SupersededRevisionId,
          null);
      }
      catch (OperationCanceledException)
      {
        stopwatch.Stop();
        _logger.LogWarning(
          "{UseCase} cancelled in {DurationMs}ms for fragment candidate {FragmentCandidateId}",
          useCaseName, stopwatch.ElapsedMilliseconds, candidateId);
        throw;
      }
      catch (LifecycleTransitionException exception)
      {
        stopwatch.Stop();

        var existingDiagnostic = await _promotionDiagnosticRepository.GetByFragmentCandidateAsync(
          command.FragmentCandidateId,
          cancellationToken);

        if (existingDiagnostic is not null)
        {
          _logger.LogWarning(
            "{UseCase} failed (transition) in {DurationMs}ms for fragment candidate {FragmentCandidateId}: {Reason} (existing diagnostic reused)",
            useCaseName, stopwatch.ElapsedMilliseconds, candidateId, exception.Message);

          return new PromoteFragmentCandidateResult(
            existingDiagnostic.Id,
            existingDiagnostic.Status,
            existingDiagnostic.KnowledgeDocumentId,
            existingDiagnostic.KnowledgeDocumentRevisionId,
            existingDiagnostic.KnowledgeCitationId,
            existingDiagnostic.SupersededExistingRevision,
            existingDiagnostic.SupersededRevisionId,
            exception.Message);
        }

        diagnostic.RecordFailure(exception.Message);

        var failedAttempt = new PromotionAttempt(
          PromotionAttemptId.New(),
          recordId,
          PromotionAttemptOutcome.PermanentInvariantViolation,
          diagnostic.Id,
          command.FragmentCandidateId,
          candidate.SourceContentHash,
          _clock.UtcNow,
          exception.Message);

        await _promotionDiagnosticRepository.AddAsync(diagnostic, cancellationToken);
        await _promotionAttemptRepository.AddAsync(failedAttempt, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogWarning(
          "{UseCase} failed (transition) in {DurationMs}ms for fragment candidate {FragmentCandidateId}: {Reason}",
          useCaseName, stopwatch.ElapsedMilliseconds, candidateId, exception.Message);

        return new PromoteFragmentCandidateResult(
          diagnostic.Id,
          PromotionDiagnosticStatus.Failed,
          null,
          null,
          null,
          false,
          null,
          exception.Message);
      }
      catch (DomainInvariantException exception)
      {
        stopwatch.Stop();

        var existingDiagnostic = await _promotionDiagnosticRepository.GetByFragmentCandidateAsync(
          command.FragmentCandidateId,
          cancellationToken);

        if (existingDiagnostic is not null)
        {
          _logger.LogWarning(
            "{UseCase} failed (invariant) in {DurationMs}ms for fragment candidate {FragmentCandidateId}: {Reason} (existing diagnostic reused)",
            useCaseName, stopwatch.ElapsedMilliseconds, candidateId, exception.Message);

          return new PromoteFragmentCandidateResult(
            existingDiagnostic.Id,
            existingDiagnostic.Status,
            existingDiagnostic.KnowledgeDocumentId,
            existingDiagnostic.KnowledgeDocumentRevisionId,
            existingDiagnostic.KnowledgeCitationId,
            existingDiagnostic.SupersededExistingRevision,
            existingDiagnostic.SupersededRevisionId,
            exception.Message);
        }

        diagnostic.RecordFailure(exception.Message);

        var failedAttempt = new PromotionAttempt(
          PromotionAttemptId.New(),
          recordId,
          PromotionAttemptOutcome.RetryablePreconditionFailure,
          diagnostic.Id,
          command.FragmentCandidateId,
          candidate.SourceContentHash,
          _clock.UtcNow,
          exception.Message);

        await _promotionDiagnosticRepository.AddAsync(diagnostic, cancellationToken);
        await _promotionAttemptRepository.AddAsync(failedAttempt, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogWarning(
          "{UseCase} failed (invariant) in {DurationMs}ms for fragment candidate {FragmentCandidateId}: {Reason}",
          useCaseName, stopwatch.ElapsedMilliseconds, candidateId, exception.Message);

        return new PromoteFragmentCandidateResult(
          diagnostic.Id,
          PromotionDiagnosticStatus.Failed,
          null,
          null,
          null,
          false,
          null,
          exception.Message);
      }
      catch (ApplicationEntityNotFoundException)
      {
        stopwatch.Stop();
        throw;
      }
      catch (Exception exception)
      {
        stopwatch.Stop();
        _logger.LogError(
          exception,
          "{UseCase} failed in {DurationMs}ms for fragment candidate {FragmentCandidateId}",
          useCaseName, stopwatch.ElapsedMilliseconds, candidateId);
        throw;
      }
    }
  }

  private static void ValidatePreconditions(
    FragmentCandidate candidate,
    ParserRun parserRun,
    ImportedDocumentSource importedSource,
    Project project)
  {
    if (candidate.ReviewState != FragmentCandidateReviewState.HumanAccepted)
    {
      throw new LifecycleTransitionException(
        nameof(FragmentCandidate),
        candidate.ReviewState.ToString(),
        "Promote");
    }

    if (parserRun.State != ParserRunState.Completed)
    {
      throw new DomainInvariantException(
        $"Parser run {parserRun.Id} must be Completed to promote a fragment candidate. Current state: {parserRun.State}.");
    }

    if (parserRun.ExecutionStatus is not (ParserExecutionStatus.Completed or ParserExecutionStatus.CompletedWithWarnings))
    {
      throw new DomainInvariantException(
        $"Parser run {parserRun.Id} execution status must be Completed or CompletedWithWarnings. Current status: {parserRun.ExecutionStatus}.");
    }

    if (importedSource.Status != ImportedDocumentSourceStatus.Available)
    {
      throw new DomainInvariantException(
        $"Imported source {importedSource.Id} must be Available to promote a fragment candidate. Current status: {importedSource.Status}.");
    }

    if (project.Lifecycle != ProjectLifecycle.Active)
    {
      throw new DomainInvariantException(
        $"Project {project.Id} must be Active to promote a fragment candidate. Current lifecycle: {project.Lifecycle}.");
    }

    if (!string.Equals(candidate.SourceContentHash, parserRun.SourceContentHash, StringComparison.Ordinal))
    {
      throw new DomainInvariantException(
        $"Fragment candidate source content hash {candidate.SourceContentHash} does not match parser run source content hash {parserRun.SourceContentHash}.");
    }
  }

  private async Task<KnowledgeDocument> FindOrCreateKnowledgeDocumentAsync(
    ProjectId projectId,
    KnowledgeDocumentType documentType,
    string canonicalTitle,
    string? externalReferenceNumber,
    string? disciplineOrCategory,
    CancellationToken cancellationToken)
  {
    var existingDocuments = await _knowledgeDocumentRepository.GetByProjectAsync(projectId, cancellationToken);

    var match = existingDocuments.FirstOrDefault(doc =>
      doc.DocumentType == documentType
      && string.Equals(doc.CanonicalTitle, canonicalTitle, StringComparison.OrdinalIgnoreCase));

    if (match is not null)
    {
      return match;
    }

    var newDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      projectId,
      documentType,
      canonicalTitle,
      externalReferenceNumber,
      disciplineOrCategory,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _knowledgeDocumentRepository.AddAsync(newDocument, cancellationToken);
    return newDocument;
  }

  private static KnowledgeCitationLocationType MapLocatorType(FragmentLocatorType locatorType)
  {
    return locatorType switch
    {
      FragmentLocatorType.WholeDocument => KnowledgeCitationLocationType.FreeformLocator,
      FragmentLocatorType.Page => KnowledgeCitationLocationType.PageNumber,
      FragmentLocatorType.Paragraph => KnowledgeCitationLocationType.Paragraph,
      FragmentLocatorType.LineRange => KnowledgeCitationLocationType.LineRange,
      FragmentLocatorType.StructuralPath => KnowledgeCitationLocationType.Section,
      _ => KnowledgeCitationLocationType.FreeformLocator,
    };
  }

  private static string ComputeMetadataHash(
    KnowledgeDocumentType documentType,
    string canonicalTitle,
    string? externalReferenceNumber,
    string? disciplineOrCategory)
  {
    var parts = new[]
    {
      documentType.ToString(),
      canonicalTitle.Trim(),
      externalReferenceNumber?.Trim() ?? string.Empty,
      disciplineOrCategory?.Trim() ?? string.Empty,
    };

    var combined = string.Join("|", parts);
    var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }

  private async Task<PromoteFragmentCandidateResult?> TryReplayByIdentityAsync(
    PromotionIdentity identity,
    CancellationToken cancellationToken)
  {
    var existingRecord = await _promotionRecordRepository.FindByIdentityHashAsync(
      identity.ProjectId,
      identity.Hash,
      cancellationToken);

    if (existingRecord is null)
    {
      return null;
    }

    var successfulAttempt = await _promotionAttemptRepository.GetLatestSuccessfulByRecordIdAsync(
      existingRecord.Id,
      cancellationToken);

    if (successfulAttempt is null)
    {
      return null;
    }

    var cachedDiagnostic = await _promotionDiagnosticRepository.GetByIdAsync(
      successfulAttempt.DiagnosticId,
      cancellationToken);

    if (cachedDiagnostic is null)
    {
      return null;
    }

    return new PromoteFragmentCandidateResult(
      cachedDiagnostic.Id,
      cachedDiagnostic.Status,
      cachedDiagnostic.KnowledgeDocumentId,
      cachedDiagnostic.KnowledgeDocumentRevisionId,
      cachedDiagnostic.KnowledgeCitationId,
      cachedDiagnostic.SupersededExistingRevision,
      cachedDiagnostic.SupersededRevisionId,
      null);
  }

}
