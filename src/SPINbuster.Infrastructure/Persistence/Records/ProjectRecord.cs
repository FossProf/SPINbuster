using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ProjectRecord
{
  public ProjectId Id { get; set; }

  public string Name { get; set; } = string.Empty;

  public string CreatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public ProjectLifecycle Lifecycle { get; set; }
}
