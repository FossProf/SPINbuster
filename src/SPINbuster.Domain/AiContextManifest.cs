using System.Security.Cryptography;
using System.Text;

namespace SPINbuster.Domain;

public enum ContextSourceType
{
  Report = 0,
  ReportSection = 1,
  FieldNote = 2,
  EvidenceAttachment = 3,
  EvidenceInterpretation = 4,
}

public enum AuthorityClassification
{
  Authoritative = 0,
  Derived = 1,
  Advisory = 2,
}

public enum ContextManifestStatus
{
  Complete = 0,
  Incomplete = 1,
}

public sealed record ContextManifestSourceEntry
{
  public ContextManifestSourceEntry(
    int order,
    ProjectId projectId,
    ContextSourceType sourceType,
    string sourceId,
    string sourceVersion,
    string contentHash,
    AuthorityClassification authorityClassification,
    string inclusionReason,
    string? limitationNotes,
    bool isSuperseded,
    IEnumerable<string>? conflictCodes)
  {
    if (order < 0)
    {
      throw new DomainInvariantException($"{nameof(order)} cannot be negative.");
    }

    Order = order;
    ProjectId = projectId;
    SourceType = sourceType;
    SourceId = DomainGuards.NotNullOrWhiteSpace(sourceId, nameof(sourceId));
    SourceVersion = DomainGuards.NotNullOrWhiteSpace(sourceVersion, nameof(sourceVersion));
    ContentHash = DomainGuards.NotNullOrWhiteSpace(contentHash, nameof(contentHash));
    AuthorityClassification = authorityClassification;
    InclusionReason = DomainGuards.NotNullOrWhiteSpace(inclusionReason, nameof(inclusionReason));
    LimitationNotes = string.IsNullOrWhiteSpace(limitationNotes) ? null : limitationNotes.Trim();
    IsSuperseded = isSuperseded;
    ConflictCodes = (conflictCodes ?? [])
      .Select(code => DomainGuards.NotNullOrWhiteSpace(code, nameof(conflictCodes)))
      .Distinct(StringComparer.Ordinal)
      .OrderBy(code => code, StringComparer.Ordinal)
      .ToArray();
  }

  public int Order { get; }

  public ProjectId ProjectId { get; }

  public ContextSourceType SourceType { get; }

  public string SourceId { get; }

  public string SourceVersion { get; }

  public string ContentHash { get; }

  public AuthorityClassification AuthorityClassification { get; }

  public string InclusionReason { get; }

  public string? LimitationNotes { get; }

  public bool IsSuperseded { get; }

  public IReadOnlyList<string> ConflictCodes { get; }
}

public sealed class ContextManifest
{
  private const string ManifestHashVersion = "context-manifest/1";
  private readonly List<ContextManifestSourceEntry> _entries = [];
  private readonly List<string> _incompleteReasons = [];

  public ContextManifest(
    ContextManifestId id,
    ProjectId projectId,
    InspectionSessionId? inspectionSessionId,
    string contextPolicyVersion,
    IEnumerable<ContextManifestSourceEntry> entries,
    IEnumerable<string> incompleteReasons,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    InspectionSessionId = inspectionSessionId;
    ContextPolicyVersion = DomainGuards.NotNullOrWhiteSpace(contextPolicyVersion, nameof(contextPolicyVersion));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));

    _entries.AddRange(CreateEntries(entries, projectId));
    _incompleteReasons.AddRange(CreateIncompleteReasons(incompleteReasons));
    Status = _incompleteReasons.Count == 0 ? ContextManifestStatus.Complete : ContextManifestStatus.Incomplete;
    ManifestHash = ComputeManifestHash();
  }

  public ContextManifestId Id { get; }

  public ProjectId ProjectId { get; }

  public InspectionSessionId? InspectionSessionId { get; }

  public string ContextPolicyVersion { get; }

  public IReadOnlyList<ContextManifestSourceEntry> Entries => _entries.AsReadOnly();

  public IReadOnlyList<string> IncompleteReasons => _incompleteReasons.AsReadOnly();

  public DateTimeOffset CreatedAtUtc { get; }

  public ContextManifestStatus Status { get; }

  public string ManifestHash { get; }

  private static ContextManifestSourceEntry[] CreateEntries(
    IEnumerable<ContextManifestSourceEntry> entries,
    ProjectId expectedProjectId)
  {
    var materializedEntries = entries?.OrderBy(entry => entry.Order).ToArray()
      ?? throw new DomainInvariantException($"{nameof(entries)} must be provided.");
    if (materializedEntries.Length == 0)
    {
      throw new DomainInvariantException("At least one context source entry must be provided.");
    }

    for (var index = 0; index < materializedEntries.Length; index++)
    {
      var entry = materializedEntries[index];
      if (entry.Order != index)
      {
        throw new DomainInvariantException("Context source entry ordering must be contiguous and zero-based.");
      }

      if (entry.ProjectId != expectedProjectId)
      {
        throw new DomainInvariantException("Cross-project context sources are not allowed.");
      }
    }

    var duplicateKeys = materializedEntries
      .GroupBy(entry => $"{entry.SourceType}:{entry.SourceId}", StringComparer.Ordinal)
      .Where(group => group.Count() > 1)
      .Select(group => group.Key)
      .ToArray();
    if (duplicateKeys.Length > 0)
    {
      throw new DomainInvariantException($"Duplicate context sources are not allowed: {string.Join(", ", duplicateKeys)}.");
    }

    return materializedEntries;
  }

  private static string[] CreateIncompleteReasons(IEnumerable<string> incompleteReasons)
  {
    return (incompleteReasons ?? [])
      .Select(reason => DomainGuards.NotNullOrWhiteSpace(reason, nameof(incompleteReasons)))
      .Distinct(StringComparer.Ordinal)
      .OrderBy(reason => reason, StringComparer.Ordinal)
      .ToArray();
  }

  private string ComputeManifestHash()
  {
    var canonicalBuilder = new StringBuilder();
    canonicalBuilder.Append(ManifestHashVersion)
      .Append('|')
      .Append(ProjectId)
      .Append('|')
      .Append(InspectionSessionId?.ToString() ?? string.Empty)
      .Append('|')
      .Append(ContextPolicyVersion)
      .Append('|')
      .Append(Status);

    foreach (var entry in _entries)
    {
      canonicalBuilder.Append('|')
        .Append(entry.Order)
        .Append('|')
        .Append(entry.ProjectId)
        .Append('|')
        .Append(entry.SourceType)
        .Append('|')
        .Append(entry.SourceId)
        .Append('|')
        .Append(entry.SourceVersion)
        .Append('|')
        .Append(entry.ContentHash)
        .Append('|')
        .Append(entry.AuthorityClassification)
        .Append('|')
        .Append(NormalizeCanonicalText(entry.InclusionReason))
        .Append('|')
        .Append(NormalizeCanonicalText(entry.LimitationNotes))
        .Append('|')
        .Append(entry.IsSuperseded);

      foreach (var conflictCode in entry.ConflictCodes)
      {
        canonicalBuilder.Append('|').Append(conflictCode);
      }
    }

    foreach (var reason in _incompleteReasons)
    {
      canonicalBuilder.Append('|').Append(reason);
    }

    var canonicalBytes = Encoding.UTF8.GetBytes(canonicalBuilder.ToString());
    var hashBytes = SHA256.HashData(canonicalBytes);
    return Convert.ToHexString(hashBytes);
  }

  private static string NormalizeCanonicalText(string? value)
  {
    return (value ?? string.Empty)
      .Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n')
      .Trim();
  }
}
