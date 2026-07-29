using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionAttempts;

public sealed record LoadPromotionAttemptsResult(
  IReadOnlyList<PromotionAttemptResult> Attempts);

public sealed record PromotionAttemptResult(
  PromotionAttemptId Id,
  PromotionRecordId RecordId,
  PromotionAttemptOutcome Outcome,
  PromotionDiagnosticId DiagnosticId,
  FragmentCandidateId FragmentCandidateId,
  DateTimeOffset AttemptedAtUtc,
  string? FailureSummary);
