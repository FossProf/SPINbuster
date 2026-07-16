using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ContextManifestRecord
{
  public ContextManifestId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public InspectionSessionId? InspectionSessionId { get; set; }

  public string ContextPolicyVersion { get; set; } = string.Empty;

  public ContextManifestStatus Status { get; set; }

  public string ManifestHash { get; set; } = string.Empty;

  public string IncompleteReasonsJson { get; set; } = "[]";

  public DateTimeOffset CreatedAtUtc { get; set; }

  public List<ContextManifestSourceEntryRecord> Entries { get; } = [];
}
