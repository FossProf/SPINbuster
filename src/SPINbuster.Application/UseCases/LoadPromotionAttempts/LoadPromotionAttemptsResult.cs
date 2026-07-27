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
  string ContentHash,
  DateTimeOffset AttemptedAtUtc,
  string? FailureReason)
{
  private const int MaxFailureReasonLength = 500;

  public string? SanitizedFailureReason =>
    FailureReason is null
      ? null
      : FailureReason.Length <= MaxFailureReasonLength
        ? FailureReason
        : FailureReason[..MaxFailureReasonLength] + "...";
}
