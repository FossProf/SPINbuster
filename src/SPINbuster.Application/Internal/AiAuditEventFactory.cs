using SPINbuster.Domain;

namespace SPINbuster.Application.Internal;

internal static class AiAuditEventFactory
{
  private static AuditEvent Create(
    string subjectType,
    string subjectId,
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      subjectType,
      subjectId,
      eventType,
      actor,
      occurredAtUtc,
      description);
  }

  public static AuditEvent ContextManifestCreated(
    ContextManifest contextManifest,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    return Create(
      nameof(ContextManifest),
      contextManifest.Id.ToString(),
      "AiContextManifestCreated",
      actor,
      occurredAtUtc,
      $"AI context manifest created with status {contextManifest.Status} and hash {contextManifest.ManifestHash}.");
  }

  public static AuditEvent ModelRunRequested(
    ModelRun modelRun,
    DateTimeOffset occurredAtUtc)
  {
    return Create(
      nameof(ModelRun),
      modelRun.Id.ToString(),
      "AiModelRunRequested",
      modelRun.InitiatedBy,
      occurredAtUtc,
      $"AI model run requested for report {modelRun.ReportId} using prompt package {modelRun.PromptPackageId}@{modelRun.PromptPackageVersion}.");
  }

  public static AuditEvent ProviderAttemptRecorded(
    ModelRun modelRun,
    ModelRunAttempt attempt,
    DateTimeOffset occurredAtUtc)
  {
    var detail = attempt.OutcomeClassification == ModelRunFailureClassification.None
      ? "provider returned output."
      : $"provider attempt classified as {attempt.OutcomeClassification}.";

    return Create(
      nameof(ModelRun),
      modelRun.Id.ToString(),
      "AiProviderAttemptRecorded",
      modelRun.InitiatedBy,
      occurredAtUtc,
      $"AI provider attempt {attempt.AttemptNumber} {detail}");
  }

  public static AuditEvent ValidationCompleted(
    ModelRun modelRun,
    ProposalStatus? proposalStatus,
    ModelRunFailureClassification failureClassification,
    string description,
    DateTimeOffset occurredAtUtc)
  {
    var outcome = proposalStatus is not null
      ? proposalStatus.ToString()
      : failureClassification.ToString();

    return Create(
      nameof(ModelRun),
      modelRun.Id.ToString(),
      "AiValidationCompleted",
      modelRun.InitiatedBy,
      occurredAtUtc,
      $"AI validation completed with outcome {outcome}: {description}");
  }

  public static AuditEvent ModelRunCompleted(
    ModelRun modelRun,
    string description,
    DateTimeOffset occurredAtUtc)
  {
    return Create(
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
    return Create(
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
    return Create(
      nameof(AiProposal),
      proposal.Id.ToString(),
      "AiProposalRejected",
      actor,
      occurredAtUtc,
      proposal.ReviewDispositionNotes is null
        ? "AI proposal rejected during human review."
        : $"AI proposal rejected during human review: {proposal.ReviewDispositionNotes}");
  }

  public static AuditEvent ProposalAccepted(
    AiProposal proposal,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    return Create(
      nameof(AiProposal),
      proposal.Id.ToString(),
      "AiProposalAccepted",
      actor,
      occurredAtUtc,
      proposal.ReviewDispositionNotes is null
        ? "AI proposal human-accepted for later deterministic processing."
        : $"AI proposal human-accepted for later deterministic processing: {proposal.ReviewDispositionNotes}");
  }
}
