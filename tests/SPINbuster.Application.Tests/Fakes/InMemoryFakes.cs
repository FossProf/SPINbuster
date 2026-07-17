using SPINbuster.Application.Abstractions;
using SPINbuster.Application;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeClock : IClock
{
  public FakeClock(DateTimeOffset utcNow)
  {
    UtcNow = utcNow;
  }

  public DateTimeOffset UtcNow { get; set; }
}

internal sealed class FakeCurrentUser : ICurrentUser
{
  public FakeCurrentUser(string userId)
  {
    UserId = new ApplicationUserId(userId);
  }

  public ApplicationUserId UserId { get; }
}

internal sealed class FakeAuditRecorder : IAuditRecorder
{
  private readonly List<string>? _sharedOperationLog;

  public FakeAuditRecorder(List<string>? sharedOperationLog = null)
  {
    _sharedOperationLog = sharedOperationLog;
  }

  public List<AuditEvent> StagedEvents { get; } = [];

  public List<string> SequenceLog { get; } = [];

  public bool ThrowOnStage { get; set; }

  public void Stage(AuditEvent auditEvent)
  {
    if (ThrowOnStage)
    {
      throw new InvalidOperationException("Audit staging failed.");
    }

    SequenceLog.Add("audit-stage");
    _sharedOperationLog?.Add("audit-stage");
    StagedEvents.Add(auditEvent);
  }
}

internal sealed class FakeAuditEventQueryRepository : IAuditEventQueryRepository
{
  private readonly FakeAuditRecorder _auditRecorder;

  public FakeAuditEventQueryRepository(FakeAuditRecorder auditRecorder)
  {
    _auditRecorder = auditRecorder;
  }

  public Task<IReadOnlyCollection<AuditEvent>> GetBySubjectAsync(
    string subjectType,
    string subjectId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<AuditEvent>>(
      _auditRecorder.StagedEvents
        .Where(auditEvent => auditEvent.SubjectType == subjectType && auditEvent.SubjectId == subjectId)
        .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
        .ThenBy(auditEvent => auditEvent.Id.ToString(), StringComparer.Ordinal)
        .ToArray());
  }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
  private readonly List<string>? _sharedOperationLog;

  public FakeUnitOfWork(List<string>? sharedOperationLog = null)
  {
    _sharedOperationLog = sharedOperationLog;
  }

  public int CommitCount { get; private set; }

  public List<string> SequenceLog { get; } = [];

  public bool ThrowOnCommit { get; set; }

  public Task CommitAsync(CancellationToken cancellationToken = default)
  {
    SequenceLog.Add("commit");
    _sharedOperationLog?.Add("commit");
    if (ThrowOnCommit)
    {
      throw new InvalidOperationException("Commit failed.");
    }

    CommitCount++;
    return Task.CompletedTask;
  }
}

internal sealed class FakeProjectRepository : IProjectRepository
{
  private readonly Dictionary<ProjectId, Project> _projects = [];

  public Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken = default)
  {
    _projects.TryGetValue(projectId, out var project);
    return Task.FromResult(project);
  }

  public Task AddAsync(Project project, CancellationToken cancellationToken = default)
  {
    _projects[project.Id] = project;
    return Task.CompletedTask;
  }

  public List<Project> UpdatedProjects { get; } = [];

  public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
  {
    _projects[project.Id] = project;
    UpdatedProjects.Add(project);
    return Task.CompletedTask;
  }
}

internal sealed class FakeInspectionSessionRepository : IInspectionSessionRepository
{
  private readonly Dictionary<InspectionSessionId, InspectionSession> _inspectionSessions = [];

  public Task<InspectionSession?> GetByIdAsync(
    InspectionSessionId inspectionSessionId,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions.TryGetValue(inspectionSessionId, out var inspectionSession);
    return Task.FromResult(inspectionSession);
  }

  public Task AddAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions[inspectionSession.Id] = inspectionSession;
    return Task.CompletedTask;
  }

  public List<InspectionSession> UpdatedInspectionSessions { get; } = [];

  public Task UpdateAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions[inspectionSession.Id] = inspectionSession;
    UpdatedInspectionSessions.Add(inspectionSession);
    return Task.CompletedTask;
  }
}

internal sealed class FakeReportRepository : IReportRepository
{
  private readonly Dictionary<ReportId, Report> _reports = [];
  private readonly Dictionary<OperationId, ReportId> _operationToReportIds = [];

  public List<Report> AddedReports { get; } = [];

  public Task<Report?> GetByIdAsync(ReportId reportId, CancellationToken cancellationToken = default)
  {
    _reports.TryGetValue(reportId, out var report);
    return Task.FromResult(report);
  }

  public Task<Report?> GetByOperationIdAsync(OperationId operationId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _operationToReportIds.TryGetValue(operationId, out var reportId) && _reports.TryGetValue(reportId, out var report)
        ? report
        : null);
  }

  public Task AddAsync(Report report, OperationId operationId, CancellationToken cancellationToken = default)
  {
    _reports[report.Id] = report;
    _operationToReportIds[operationId] = report.Id;
    AddedReports.Add(report);
    return Task.CompletedTask;
  }

  public Task<int> CountAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_reports.Count);
  }
}

internal sealed class FakeContextManifestRepository : IContextManifestRepository
{
  private readonly Dictionary<ContextManifestId, ContextManifest> _manifests = [];

  public List<ContextManifest> AddedManifests { get; } = [];

  public Task<ContextManifest?> GetByIdAsync(
    ContextManifestId contextManifestId,
    CancellationToken cancellationToken = default)
  {
    _manifests.TryGetValue(contextManifestId, out var manifest);
    return Task.FromResult(manifest);
  }

  public Task AddAsync(ContextManifest contextManifest, CancellationToken cancellationToken = default)
  {
    _manifests[contextManifest.Id] = contextManifest;
    AddedManifests.Add(contextManifest);
    return Task.CompletedTask;
  }
}

internal sealed class FakeModelRunRepository : IModelRunRepository
{
  private readonly Dictionary<ModelRunId, ModelRun> _modelRuns = [];
  private readonly Dictionary<string, ModelRunId> _correlationIds = new(StringComparer.Ordinal);
  private readonly Dictionary<ModelRunId, List<ModelRunAttempt>> _attempts = [];

  public List<ModelRun> AddedModelRuns { get; } = [];

  public List<ModelRun> UpdatedModelRuns { get; } = [];

  public Task<ModelRun?> GetByIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default)
  {
    _modelRuns.TryGetValue(modelRunId, out var modelRun);
    return Task.FromResult(modelRun);
  }

  public Task<ModelRun?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _correlationIds.TryGetValue(correlationId, out var modelRunId) && _modelRuns.TryGetValue(modelRunId, out var modelRun)
        ? modelRun
        : null);
  }

  public Task<IReadOnlyCollection<ModelRunAttempt>> GetAttemptsAsync(
    ModelRunId modelRunId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<ModelRunAttempt>>(
      _attempts.TryGetValue(modelRunId, out var attempts) ? attempts.ToArray() : []);
  }

  public Task AddAsync(ModelRun modelRun, CancellationToken cancellationToken = default)
  {
    _modelRuns[modelRun.Id] = modelRun;
    _correlationIds[modelRun.CorrelationId] = modelRun.Id;
    AddedModelRuns.Add(modelRun);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(ModelRun modelRun, CancellationToken cancellationToken = default)
  {
    _modelRuns[modelRun.Id] = modelRun;
    UpdatedModelRuns.Add(modelRun);
    return Task.CompletedTask;
  }

  public Task AddAttemptAsync(ModelRunAttempt attempt, CancellationToken cancellationToken = default)
  {
    if (!_attempts.TryGetValue(attempt.ModelRunId, out var attempts))
    {
      attempts = [];
      _attempts[attempt.ModelRunId] = attempts;
    }

    attempts.Add(attempt);
    return Task.CompletedTask;
  }
}

internal sealed class FakeAiProposalRepository : IAiProposalRepository
{
  private readonly Dictionary<ProposalId, AiProposal> _proposals = [];
  private readonly Dictionary<ModelRunId, ProposalId> _proposalIdsByModelRun = [];

  public List<AiProposal> AddedProposals { get; } = [];

  public List<AiProposal> UpdatedProposals { get; } = [];

  public Task<AiProposal?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken = default)
  {
    _proposals.TryGetValue(proposalId, out var proposal);
    return Task.FromResult(proposal);
  }

  public Task<AiProposal?> GetByModelRunIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _proposalIdsByModelRun.TryGetValue(modelRunId, out var proposalId) && _proposals.TryGetValue(proposalId, out var proposal)
        ? proposal
        : null);
  }

  public Task AddAsync(AiProposal proposal, CancellationToken cancellationToken = default)
  {
    _proposals[proposal.Id] = proposal;
    _proposalIdsByModelRun[proposal.ModelRunId] = proposal.Id;
    AddedProposals.Add(proposal);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(AiProposal proposal, CancellationToken cancellationToken = default)
  {
    _proposals[proposal.Id] = proposal;
    UpdatedProposals.Add(proposal);
    return Task.CompletedTask;
  }

  public Task<int> CountAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_proposals.Count);
  }
}

internal sealed class FakePromptPackageRegistry : IAiPromptPackageRegistry
{
  public PromptPackageDefinition? PromptPackage { get; set; } = new(
    "report-draft-proposal-default",
    "0.1.0",
    "report-draft-proposer",
    "report-draft-context-policy/1.0",
    "report-draft-proposal",
    "1.0.0",
    [
      AiProviderCapability.StructuredOutput,
      AiProviderCapability.DeterministicFixtures,
    ],
    PromptPackageStatus.Approved,
    "Deterministic prompt package.");

  public Task<PromptPackageDefinition?> GetByIdAsync(
    string packageId,
    string semanticVersion,
    CancellationToken cancellationToken = default)
  {
    var matches = PromptPackage is not null
      && string.Equals(PromptPackage.PackageId, packageId, StringComparison.Ordinal)
      && string.Equals(PromptPackage.SemanticVersion, semanticVersion, StringComparison.Ordinal);
    return Task.FromResult(matches ? PromptPackage : null);
  }
}

internal sealed class FakeAiGenerationProvider : IAiGenerationProvider
{
  public AiProviderDescriptor Descriptor { get; set; } = new(
    "tier0-deterministic",
    "deterministic-fixture",
    "sha256:deterministic-fixture-v1",
    [
      AiProviderCapability.StructuredOutput,
      AiProviderCapability.DeterministicFixtures,
    ]);

  public Func<AiGenerationRequest, CancellationToken, Task<AiGenerationResult>> GenerateAsyncImpl { get; set; } =
    (request, _) => Task.FromResult(new AiGenerationResult(
      true,
      """
{
  "sections": [
    { "heading": "Summary", "content": "Deterministic advisory summary." }
  ],
  "reasoningSummary": "Grounded in governed sources only.",
  "confidenceBand": "Medium",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "FIELD_NOTE_ID" },
    { "sourceType": "EvidenceAttachment", "sourceId": "EVIDENCE_ATTACHMENT_ID" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": []
}
"""
        .Replace("FIELD_NOTE_ID", request.PromptContext.Contains("FieldNote ", StringComparison.Ordinal) ? request.PromptContext.Split("FieldNote ", StringSplitOptions.None)[1].Split(':')[0].Trim() : "missing-id", StringComparison.Ordinal)
        .Replace("EVIDENCE_ATTACHMENT_ID", request.PromptContext.Contains("Evidence ", StringComparison.Ordinal) ? request.PromptContext.Split("Evidence ", StringSplitOptions.None)[1].Split(':')[0].Trim() : "missing-id", StringComparison.Ordinal),
      AiGenerationFailureClassification.None,
      null,
      42,
      128,
      96,
      request.Temperature,
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));

  public List<AiGenerationRequest> Requests { get; } = [];

  public AiProviderDescriptor Describe() => Descriptor;

  public Task<AiGenerationResult> GenerateAsync(
    AiGenerationRequest request,
    CancellationToken cancellationToken = default)
  {
    Requests.Add(request);
    return GenerateAsyncImpl(request, cancellationToken);
  }
}

internal sealed class FakeSaveTransactionRepository : ISaveTransactionRepository
{
  private readonly Dictionary<SaveTransactionId, SaveTransaction> _saveTransactions = [];

  public Task<SaveTransaction?> GetByIdAsync(
    SaveTransactionId saveTransactionId,
    CancellationToken cancellationToken = default)
  {
    _saveTransactions.TryGetValue(saveTransactionId, out var saveTransaction);
    return Task.FromResult(saveTransaction);
  }

  public Task AddAsync(
    SaveTransaction saveTransaction,
    CancellationToken cancellationToken = default)
  {
    _saveTransactions[saveTransaction.Id] = saveTransaction;
    return Task.CompletedTask;
  }
}

internal sealed class FakeKnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
  private readonly Dictionary<KnowledgeDocumentId, KnowledgeDocument> _documents = [];

  public List<KnowledgeDocument> AddedDocuments { get; } = [];

  public List<KnowledgeDocument> UpdatedDocuments { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<KnowledgeDocument?> GetByIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _documents.TryGetValue(knowledgeDocumentId, out var knowledgeDocument);
    return Task.FromResult(knowledgeDocument);
  }

  public Task<IReadOnlyCollection<KnowledgeDocument>> GetByProjectAsync(
    ProjectId projectId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeDocument>>(
      _documents.Values.Where(document => document.ProjectId == projectId).ToArray());
  }

  public Task AddAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _documents[knowledgeDocument.Id] = knowledgeDocument;
    AddedDocuments.Add(knowledgeDocument);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _documents[knowledgeDocument.Id] = knowledgeDocument;
    UpdatedDocuments.Add(knowledgeDocument);
    return Task.CompletedTask;
  }
}

internal sealed class FakeKnowledgeRevisionRepository : IKnowledgeRevisionRepository
{
  private readonly Dictionary<KnowledgeDocumentRevisionId, KnowledgeDocumentRevision> _revisions = [];

  public List<KnowledgeDocumentRevision> AddedRevisions { get; } = [];

  public List<KnowledgeDocumentRevision> UpdatedRevisions { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<KnowledgeDocumentRevision?> GetByIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _revisions.TryGetValue(knowledgeDocumentRevisionId, out var knowledgeRevision);
    return Task.FromResult(knowledgeRevision);
  }

  public Task<KnowledgeDocumentRevision?> GetCurrentByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult(
      _revisions.Values.SingleOrDefault(revision =>
        revision.DocumentId == knowledgeDocumentId
        && revision.Lifecycle == KnowledgeRevisionLifecycle.CurrentAuthoritative));
  }

  public Task<IReadOnlyCollection<KnowledgeDocumentRevision>> GetByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeDocumentRevision>>(
      _revisions.Values.Where(revision => revision.DocumentId == knowledgeDocumentId).ToArray());
  }

  public Task AddAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _revisions[knowledgeDocumentRevision.Id] = knowledgeDocumentRevision;
    AddedRevisions.Add(knowledgeDocumentRevision);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _revisions[knowledgeDocumentRevision.Id] = knowledgeDocumentRevision;
    UpdatedRevisions.Add(knowledgeDocumentRevision);
    return Task.CompletedTask;
  }
}

internal sealed class FakeKnowledgeRelationshipRepository : IKnowledgeRelationshipRepository
{
  private readonly Dictionary<KnowledgeRelationshipId, KnowledgeRelationship> _relationships = [];

  public List<KnowledgeRelationship> AddedRelationships { get; } = [];

  public List<KnowledgeRelationship> UpdatedRelationships { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<KnowledgeRelationship?> GetByIdAsync(
    KnowledgeRelationshipId knowledgeRelationshipId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _relationships.TryGetValue(knowledgeRelationshipId, out var knowledgeRelationship);
    return Task.FromResult(knowledgeRelationship);
  }

  public Task<KnowledgeRelationship?> FindByEndpointsAsync(
    ProjectId projectId,
    KnowledgeSubjectReference source,
    KnowledgeSubjectReference target,
    KnowledgeRelationshipType relationshipType,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult(
      _relationships.Values.SingleOrDefault(relationship =>
        relationship.ProjectId == projectId
        && relationship.Source == source
        && relationship.Target == target
        && relationship.RelationshipType == relationshipType));
  }

  public Task<IReadOnlyCollection<KnowledgeRelationship>> GetBySubjectAsync(
    ProjectId projectId,
    KnowledgeSubjectReference subject,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeRelationship>>(
      _relationships.Values
        .Where(relationship =>
          relationship.ProjectId == projectId
          && (relationship.Source == subject || relationship.Target == subject))
        .OrderBy(relationship => relationship.CreatedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public Task AddAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _relationships[knowledgeRelationship.Id] = knowledgeRelationship;
    AddedRelationships.Add(knowledgeRelationship);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _relationships[knowledgeRelationship.Id] = knowledgeRelationship;
    UpdatedRelationships.Add(knowledgeRelationship);
    return Task.CompletedTask;
  }
}

internal sealed class FakeKnowledgeCitationRepository : IKnowledgeCitationRepository
{
  private readonly Dictionary<KnowledgeCitationId, KnowledgeCitation> _citations = [];

  public List<KnowledgeCitation> AddedCitations { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<IReadOnlyCollection<KnowledgeCitation>> GetByRevisionIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeCitation>>(
      _citations.Values.Where(citation => citation.CitedRevisionId == knowledgeDocumentRevisionId).ToArray());
  }

  public Task AddAsync(
    KnowledgeCitation knowledgeCitation,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _citations[knowledgeCitation.Id] = knowledgeCitation;
    AddedCitations.Add(knowledgeCitation);
    return Task.CompletedTask;
  }
}
