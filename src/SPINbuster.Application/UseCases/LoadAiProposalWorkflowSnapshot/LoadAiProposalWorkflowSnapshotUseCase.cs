using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;

public sealed class LoadAiProposalWorkflowSnapshotUseCase
  : IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult>
{
  private readonly IAuditEventQueryRepository _auditEventQueryRepository;
  private readonly IModelRunRepository _modelRunRepository;
  private readonly IAiProposalRepository _proposalRepository;

  public LoadAiProposalWorkflowSnapshotUseCase(
    IModelRunRepository modelRunRepository,
    IAiProposalRepository proposalRepository,
    IAuditEventQueryRepository auditEventQueryRepository)
  {
    _modelRunRepository = modelRunRepository;
    _proposalRepository = proposalRepository;
    _auditEventQueryRepository = auditEventQueryRepository;
  }

  public async Task<LoadAiProposalWorkflowSnapshotResult> HandleAsync(
    LoadAiProposalWorkflowSnapshotQuery query,
    CancellationToken cancellationToken = default)
  {
    var modelRun = await _modelRunRepository.GetByIdAsync(query.ModelRunId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(ModelRun), query.ModelRunId.ToString());
    var attempts = await _modelRunRepository.GetAttemptsAsync(query.ModelRunId, cancellationToken);
    var modelRunAuditHistory = await _auditEventQueryRepository.GetBySubjectAsync(
      nameof(ModelRun),
      modelRun.Id.ToString(),
      cancellationToken);

    AiProposalWorkflowSnapshot? proposalSnapshot = null;
    if (query.ProposalId is not null)
    {
      var proposal = await _proposalRepository.GetByIdAsync(query.ProposalId.Value, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException("AiProposal", query.ProposalId.Value.ToString());
      var proposalAuditHistory = await _auditEventQueryRepository.GetBySubjectAsync(
        nameof(AiProposal),
        proposal.Id.ToString(),
        cancellationToken);

      proposalSnapshot = new AiProposalWorkflowSnapshot(
        proposal.Id,
        proposal.Status,
        proposal.ReportId,
        proposal.ProjectId,
        proposal.InspectionSessionId,
        proposal.ConfidenceBand,
        proposal.GeneratedAtUtc,
        proposal.Warnings.ToArray(),
        proposal.UncertaintyCodes.ToArray(),
        proposal.ValidationFailures.ToArray(),
        proposal.ReferencedSourceIds.ToArray(),
        proposal.StructuredPayloadJson,
        proposal.StructuredPayloadHash,
        proposal.AbstentionReason,
        proposal.ReviewDispositionNotes,
        proposalAuditHistory.Select(ToAuditSnapshot).ToArray());
    }

    return new LoadAiProposalWorkflowSnapshotResult(
      modelRun.Id,
      modelRun.State,
      modelRun.FailureClassification,
      modelRun.FailureMessage,
      modelRun.CorrelationId,
      modelRun.PromptPackageId,
      modelRun.PromptPackageVersion,
      modelRun.ProviderId,
      modelRun.ModelName,
      modelRun.ModelDigest,
      modelRun.ContextManifestId,
      modelRun.ContextManifestHash,
      modelRun.RequestedAtUtc,
      attempts.Select(attempt => new AiModelRunAttemptSnapshot(
        attempt.Id,
        attempt.AttemptNumber,
        attempt.InputHash,
        attempt.StartedAtUtc,
        attempt.CompletedAtUtc,
        attempt.LatencyMilliseconds,
        attempt.InputTokenCount,
        attempt.OutputTokenCount,
        attempt.RawOutput,
        attempt.RawOutputHash,
        attempt.OutcomeClassification,
        attempt.FailureMessage)).ToArray(),
      modelRunAuditHistory.Select(ToAuditSnapshot).ToArray(),
      proposalSnapshot);
  }

  private static AiWorkflowAuditEntrySnapshot ToAuditSnapshot(AuditEvent auditEvent)
  {
    return new AiWorkflowAuditEntrySnapshot(
      auditEvent.Id,
      auditEvent.EventType,
      auditEvent.Actor,
      auditEvent.OccurredAtUtc,
      auditEvent.Description);
  }
}
