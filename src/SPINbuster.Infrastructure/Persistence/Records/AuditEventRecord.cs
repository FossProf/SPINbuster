using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class AuditEventRecord
{
  public AuditEventId Id { get; set; }

  public string SubjectType { get; set; } = string.Empty;

  public string SubjectId { get; set; } = string.Empty;

  public string EventType { get; set; } = string.Empty;

  public string Actor { get; set; } = string.Empty;

  public DateTimeOffset OccurredAtUtc { get; set; }

  public string Description { get; set; } = string.Empty;
}
