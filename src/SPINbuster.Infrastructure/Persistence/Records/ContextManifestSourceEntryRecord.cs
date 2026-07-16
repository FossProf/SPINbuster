using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ContextManifestSourceEntryRecord
{
  public ContextManifestId ContextManifestId { get; set; }

  public int Order { get; set; }

  public ProjectId ProjectId { get; set; }

  public ContextSourceType SourceType { get; set; }

  public string SourceId { get; set; } = string.Empty;

  public string SourceVersion { get; set; } = string.Empty;

  public string ContentHash { get; set; } = string.Empty;

  public AuthorityClassification AuthorityClassification { get; set; }

  public string InclusionReason { get; set; } = string.Empty;

  public string? LimitationNotes { get; set; }

  public bool IsSuperseded { get; set; }

  public string ConflictCodesJson { get; set; } = "[]";
}
