using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ReportRecord
{
  public ReportId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public InspectionSessionId InspectionSessionId { get; set; }

  public string Title { get; set; } = string.Empty;

  public string Body { get; set; } = string.Empty;

  public string CreatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public ReportLifecycle Lifecycle { get; set; }

  public string? ApprovedBy { get; set; }

  public DateTimeOffset? ApprovedAtUtc { get; set; }
}
