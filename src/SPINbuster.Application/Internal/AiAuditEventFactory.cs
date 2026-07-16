using SPINbuster.Domain;

namespace SPINbuster.Application.Internal;

internal static class AiAuditEventFactory
{
  public static AuditEvent ContextManifestCreated(
    ContextManifest contextManifest,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(ContextManifest),
      contextManifest.Id.ToString(),
      "AiContextManifestCreated",
      actor,
      occurredAtUtc,
      $"AI context manifest created with status {contextManifest.Status} and hash {contextManifest.ManifestHash}.");
  }

  public static AuditEvent ModelRunCompleted(
    ModelRun modelRun,
    string description,
    DateTimeOffset occurredAtUtc)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(ModelRun),
      modelRun.Id.ToString(),
      "AiModelRunCompleted",
      modelRun.InitiatedBy,
      occurredAtUtc,
      description);
  }

  public static AuditEvent ProposalRecorded(
    AiProposal proposal,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(AiProposal),
      proposal.Id.ToString(),
      "AiProposalRecorded",
      actor,
      occurredAtUtc,
      $"AI proposal recorded with status {proposal.Status} for report {proposal.ReportId}.");
  }

  public static AuditEvent ProposalRejected(
    AiProposal proposal,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(AiProposal),
      proposal.Id.ToString(),
      "AiProposalRejected",
      actor,
      occurredAtUtc,
      proposal.ReviewDispositionNotes is null
        ? "AI proposal rejected during human review."
        : $"AI proposal rejected during human review: {proposal.ReviewDispositionNotes}");
  }
}
