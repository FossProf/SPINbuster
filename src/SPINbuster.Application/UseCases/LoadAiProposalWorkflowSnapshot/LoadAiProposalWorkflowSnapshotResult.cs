using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;

public sealed record LoadAiProposalWorkflowSnapshotResult(
  ModelRunId ModelRunId,
  ModelRunState ModelRunState,
  ModelRunFailureClassification FailureClassification,
  string? FailureMessage,
  string CorrelationId,
  string PromptPackageId,
  string PromptPackageVersion,
  string ProviderId,
  string ModelName,
  string ModelDigest,
  ContextManifestId ContextManifestId,
  string ContextManifestHash,
  DateTimeOffset RequestedAtUtc,
  IReadOnlyCollection<AiModelRunAttemptSnapshot> Attempts,
  IReadOnlyCollection<AiWorkflowAuditEntrySnapshot> ModelRunAuditHistory,
  AiProposalWorkflowSnapshot? Proposal);

public sealed record AiModelRunAttemptSnapshot(
  ModelRunAttemptId ModelRunAttemptId,
  int AttemptNumber,
  string InputHash,
  DateTimeOffset StartedAtUtc,
  DateTimeOffset? CompletedAtUtc,
  long? LatencyMilliseconds,
  int? InputTokenCount,
  int? OutputTokenCount,
  string? RawOutput,
  string? RawOutputHash,
  ModelRunFailureClassification OutcomeClassification,
  string? FailureMessage);

public sealed record AiProposalWorkflowSnapshot(
  ProposalId ProposalId,
  ProposalStatus Status,
  ReportId ReportId,
  ProjectId ProjectId,
  InspectionSessionId? InspectionSessionId,
  ConfidenceBand ConfidenceBand,
  DateTimeOffset GeneratedAtUtc,
  IReadOnlyCollection<string> Warnings,
  IReadOnlyCollection<string> UncertaintyCodes,
  IReadOnlyCollection<string> ValidationFailures,
  IReadOnlyCollection<string> ReferencedSourceIds,
  string StructuredPayloadJson,
  string StructuredPayloadHash,
  string? AbstentionReason,
  string? ReviewDispositionNotes,
  IReadOnlyCollection<AiWorkflowAuditEntrySnapshot> AuditHistory);

public sealed record AiWorkflowAuditEntrySnapshot(
  AuditEventId AuditEventId,
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
