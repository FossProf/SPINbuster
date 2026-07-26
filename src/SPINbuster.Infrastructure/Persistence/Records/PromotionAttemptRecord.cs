using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class PromotionAttemptRecord
{
  public PromotionAttemptId Id { get; set; }

  public PromotionRecordId RecordId { get; set; }

  public PromotionAttemptOutcome Outcome { get; set; }

  public PromotionDiagnosticId DiagnosticId { get; set; }

  public FragmentCandidateId FragmentCandidateId { get; set; }

  public string ContentHash { get; set; } = string.Empty;

  public DateTimeOffset AttemptedAtUtc { get; set; }

  public string? FailureReason { get; set; }
}
