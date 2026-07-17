using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;

public sealed class LoadProjectDocumentWorkflowSnapshotUseCase
  : IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult>
{
  private readonly IAiProposalRepository _aiProposalRepository;
  private readonly IAuditEventQueryRepository _auditEventQueryRepository;
  private readonly IDocumentCandidateRepository _documentCandidateRepository;
  private readonly IDocumentImportPolicy _documentImportPolicy;
  private readonly IDocumentImportSessionRepository _documentImportSessionRepository;
  private readonly IDocumentProcessingAttemptRepository _documentProcessingAttemptRepository;
  private readonly IImportedDocumentSourceRepository _importedDocumentSourceRepository;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRelationshipRepository _knowledgeRelationshipRepository;
  private readonly IReportRepository _reportRepository;
  private readonly IStorageObjectRepository _storageObjectRepository;

  public LoadProjectDocumentWorkflowSnapshotUseCase(
    IDocumentImportSessionRepository documentImportSessionRepository,
    IImportedDocumentSourceRepository importedDocumentSourceRepository,
    IDocumentProcessingAttemptRepository documentProcessingAttemptRepository,
    IDocumentCandidateRepository documentCandidateRepository,
    IStorageObjectRepository storageObjectRepository,
    IAuditEventQueryRepository auditEventQueryRepository,
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IKnowledgeRelationshipRepository knowledgeRelationshipRepository,
    IReportRepository reportRepository,
    IAiProposalRepository aiProposalRepository,
    IDocumentImportPolicy documentImportPolicy)
  {
    _documentImportSessionRepository = documentImportSessionRepository;
    _importedDocumentSourceRepository = importedDocumentSourceRepository;
    _documentProcessingAttemptRepository = documentProcessingAttemptRepository;
    _documentCandidateRepository = documentCandidateRepository;
    _storageObjectRepository = storageObjectRepository;
    _auditEventQueryRepository = auditEventQueryRepository;
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _knowledgeRelationshipRepository = knowledgeRelationshipRepository;
    _reportRepository = reportRepository;
    _aiProposalRepository = aiProposalRepository;
    _documentImportPolicy = documentImportPolicy;
  }

  public async Task<LoadProjectDocumentWorkflowSnapshotResult> HandleAsync(
    LoadProjectDocumentWorkflowSnapshotQuery query,
    CancellationToken cancellationToken = default)
  {
    ValidateBounds(query);

    var importSessions = await _documentImportSessionRepository.GetByProjectAsync(query.ProjectId, query.MaxImportSessions, cancellationToken);
    var importedSources = await _importedDocumentSourceRepository.GetByProjectAsync(query.ProjectId, query.MaxSources, cancellationToken);

    var importSessionSnapshots = new List<ProjectDocumentImportSessionSnapshot>(importSessions.Count);
    foreach (var importSession in importSessions)
    {
      var auditHistory = await LoadAuditHistoryAsync(nameof(DocumentImportSession), importSession.Id.ToString(), query.MaxAuditEntriesPerSubject, cancellationToken);
      importSessionSnapshots.Add(new ProjectDocumentImportSessionSnapshot(
        importSession.Id,
        importSession.InitiatedBy,
        importSession.StartedAtUtc,
        importSession.CompletedAtUtc,
        importSession.State,
        importSession.SourceCount,
        importSession.AcceptedCount,
        importSession.DuplicateCount,
        importSession.RejectedCount,
        importSession.FailureSummary,
        auditHistory));
    }

    var sourceSnapshots = new List<ProjectImportedDocumentSourceSnapshot>(importedSources.Count);
    var projectAuditHistory = new List<ProjectDocumentAuditEntrySnapshot>();

    foreach (var importedSource in importedSources)
    {
      var storageObject = await _storageObjectRepository.GetByIdAsync(importedSource.StorageReference.StorageObjectId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(StorageObject), importedSource.StorageReference.StorageObjectId.ToString());
      var attempts = await _documentProcessingAttemptRepository.GetByImportedSourceAsync(
        importedSource.Id,
        query.MaxProcessingAttemptsPerSource,
        cancellationToken);
      var candidates = await _documentCandidateRepository.GetByImportedSourceAsync(
        importedSource.Id,
        query.MaxCandidatesPerSource,
        cancellationToken);

      var attemptSnapshots = new List<ProjectDocumentProcessingAttemptSnapshot>(attempts.Count);
      foreach (var attempt in attempts)
      {
        attemptSnapshots.Add(new ProjectDocumentProcessingAttemptSnapshot(
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
          attempt.CompletedAtUtc,
          await LoadAuditHistoryAsync(nameof(DocumentProcessingAttempt), attempt.Id.ToString(), query.MaxAuditEntriesPerSubject, cancellationToken)));
      }

      var candidateSnapshots = new List<ProjectDocumentCandidateSnapshot>(candidates.Count);
      foreach (var candidate in candidates)
      {
        candidateSnapshots.Add(new ProjectDocumentCandidateSnapshot(
          candidate.Id,
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
          candidate.ReviewNotes,
          await LoadAuditHistoryAsync(nameof(DocumentCandidate), candidate.Id.ToString(), query.MaxAuditEntriesPerSubject, cancellationToken)));
      }

      var sourceAuditHistory = await LoadAuditHistoryAsync(nameof(ImportedDocumentSource), importedSource.Id.ToString(), query.MaxAuditEntriesPerSubject, cancellationToken);
      projectAuditHistory.AddRange(sourceAuditHistory);
      sourceSnapshots.Add(new ProjectImportedDocumentSourceSnapshot(
        importedSource.Id,
        importedSource.ImportSessionId,
        importedSource.OriginalFileName,
        importedSource.DeclaredMediaType,
        importedSource.DetectedMediaType,
        importedSource.ContentLength,
        importedSource.ContentHash,
        importedSource.HashAlgorithm,
        importedSource.HashAlgorithmVersion,
        importedSource.SourceOrigin,
        importedSource.Status,
        new ProjectDocumentStorageSnapshot(
          storageObject.Id,
          storageObject.StorageProviderKey,
          storageObject.ImmutableObjectKey,
          storageObject.ContentLength,
          storageObject.ContentHash,
          storageObject.HashAlgorithm,
          storageObject.HashAlgorithmVersion,
          storageObject.CreatedAtUtc,
          storageObject.AvailabilityState),
        await _importedDocumentSourceRepository.ExistsInOtherProjectsAsync(
          query.ProjectId,
          importedSource.ContentHash,
          importedSource.HashAlgorithm,
          importedSource.HashAlgorithmVersion,
          cancellationToken),
        attemptSnapshots,
        candidateSnapshots,
        sourceAuditHistory));
    }

    projectAuditHistory.AddRange(importSessionSnapshots.SelectMany(snapshot => snapshot.AuditHistory));
    projectAuditHistory.AddRange(sourceSnapshots.SelectMany(snapshot => snapshot.ProcessingAttempts).SelectMany(snapshot => snapshot.AuditHistory));
    projectAuditHistory.AddRange(sourceSnapshots.SelectMany(snapshot => snapshot.Candidates).SelectMany(snapshot => snapshot.AuditHistory));

    var knowledgeDocuments = await _knowledgeDocumentRepository.GetByProjectAsync(query.ProjectId, cancellationToken);
    var relationshipCount = 0;
    foreach (var knowledgeDocument in knowledgeDocuments)
    {
      relationshipCount += (await _knowledgeRelationshipRepository.GetBySubjectAsync(
        query.ProjectId,
        KnowledgeSubjectReference.ForDocument(query.ProjectId, knowledgeDocument.Id),
        query.MaxSources,
        cancellationToken)).Count;
    }

    return new LoadProjectDocumentWorkflowSnapshotResult(
      query.ProjectId,
      importSessionSnapshots,
      sourceSnapshots,
      new ProjectDocumentAuthorityIsolationSnapshot(
        knowledgeDocuments.Count,
        relationshipCount,
        await _reportRepository.CountAsync(cancellationToken),
        await _aiProposalRepository.CountAsync(cancellationToken)),
      projectAuditHistory
        .DistinctBy(entry => entry.AuditEventId)
        .OrderBy(entry => entry.OccurredAtUtc)
        .ThenBy(entry => entry.AuditEventId.Value)
        .ToArray());
  }

  private void ValidateBounds(LoadProjectDocumentWorkflowSnapshotQuery query)
  {
    if (query.MaxImportSessions <= 0 || query.MaxImportSessions > _documentImportPolicy.MaxProcessingHistoryResults)
    {
      throw new DomainInvariantException($"Project document snapshot import-session max results must be between 1 and {_documentImportPolicy.MaxProcessingHistoryResults}.");
    }

    if (query.MaxSources <= 0 || query.MaxSources > _documentImportPolicy.MaxProcessingHistoryResults)
    {
      throw new DomainInvariantException($"Project document snapshot source max results must be between 1 and {_documentImportPolicy.MaxProcessingHistoryResults}.");
    }

    if (query.MaxProcessingAttemptsPerSource <= 0 || query.MaxProcessingAttemptsPerSource > _documentImportPolicy.MaxProcessingHistoryResults)
    {
      throw new DomainInvariantException($"Project document snapshot attempt max results must be between 1 and {_documentImportPolicy.MaxProcessingHistoryResults}.");
    }

    if (query.MaxCandidatesPerSource <= 0 || query.MaxCandidatesPerSource > _documentImportPolicy.MaxCandidateQueryResults)
    {
      throw new DomainInvariantException($"Project document snapshot candidate max results must be between 1 and {_documentImportPolicy.MaxCandidateQueryResults}.");
    }

    if (query.MaxAuditEntriesPerSubject <= 0 || query.MaxAuditEntriesPerSubject > 200)
    {
      throw new DomainInvariantException("Project document snapshot audit max results must be between 1 and 200.");
    }
  }

  private async Task<IReadOnlyList<ProjectDocumentAuditEntrySnapshot>> LoadAuditHistoryAsync(
    string subjectType,
    string subjectId,
    int maxResults,
    CancellationToken cancellationToken)
  {
    var auditEvents = await _auditEventQueryRepository.GetBySubjectAsync(subjectType, subjectId, cancellationToken);
    return auditEvents
      .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
      .ThenBy(auditEvent => auditEvent.Id.Value)
      .Take(maxResults)
      .Select(auditEvent => new ProjectDocumentAuditEntrySnapshot(
        auditEvent.Id,
        auditEvent.EventType,
        auditEvent.Actor,
        auditEvent.OccurredAtUtc,
        auditEvent.Description))
      .ToArray();
  }
}
