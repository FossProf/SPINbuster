using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class SaveTransactionRecord
{
  public SaveTransactionId Id { get; set; }

  public ReportId ReportId { get; set; }

  public string InitiatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public SaveTransactionState State { get; set; }

  public string? FailureReason { get; set; }

  public DateTimeOffset? PreparedAtUtc { get; set; }

  public DateTimeOffset? PersistedAtUtc { get; set; }

  public DateTimeOffset? CompletedAtUtc { get; set; }
}
