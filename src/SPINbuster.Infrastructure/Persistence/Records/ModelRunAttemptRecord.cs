using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ModelRunAttemptRecord
{
  public ModelRunAttemptId Id { get; set; }

  public ModelRunId ModelRunId { get; set; }

  public int AttemptNumber { get; set; }

  public string InputHash { get; set; } = string.Empty;

  public DateTimeOffset StartedAtUtc { get; set; }

  public DateTimeOffset? CompletedAtUtc { get; set; }

  public long? LatencyMilliseconds { get; set; }

  public int? InputTokenCount { get; set; }

  public int? OutputTokenCount { get; set; }

  public string? RawOutput { get; set; }

  public string? RawOutputHash { get; set; }

  public ModelRunFailureClassification OutcomeClassification { get; set; }

  public string? FailureMessage { get; set; }
}
