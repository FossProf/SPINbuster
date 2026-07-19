using System.Security.Cryptography;
using System.Text;

namespace SPINbuster.Domain;

public enum ParserRunState
{
  Created = 0,
  Running = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
}

public enum FragmentLocatorType
{
  WholeDocument = 0,
  Page = 1,
  Paragraph = 2,
  LineRange = 3,
  StructuralPath = 4,
}

public enum ContentKind
{
  PlainText = 0,
  Table = 1,
  Figure = 2,
  Code = 3,
  Metadata = 4,
}

public sealed record FragmentLocator
{
  public FragmentLocator(FragmentLocatorType locatorType, string rawValue)
  {
    LocatorType = locatorType;
    RawValue = DomainGuards.NotNullOrWhiteSpace(rawValue, nameof(rawValue));
    NormalizedValue = Normalize(locatorType, rawValue);
  }

  public FragmentLocatorType LocatorType { get; }

  public string RawValue { get; }

  public string NormalizedValue { get; }

  private static string Normalize(FragmentLocatorType locatorType, string rawValue)
  {
    var trimmed = rawValue.Trim();

    return locatorType switch
    {
      FragmentLocatorType.WholeDocument => string.Empty,
      FragmentLocatorType.Page => NormalizePage(trimmed),
      FragmentLocatorType.Paragraph => NormalizeParagraph(trimmed),
      FragmentLocatorType.LineRange => NormalizeLineRange(trimmed),
      FragmentLocatorType.StructuralPath => NormalizeStructuralPath(trimmed),
      _ => trimmed,
    };
  }

  private static string NormalizePage(string value)
  {
    if (int.TryParse(value, out _))
    {
      return value;
    }

    throw new DomainInvariantException($"Page locator value '{value}' must be a numeric page number.");
  }

  private static string NormalizeParagraph(string value)
  {
    var parts = value.Split(':', 2);
    if (parts.Length == 2 && int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _))
    {
      return value;
    }

    throw new DomainInvariantException($"Paragraph locator value '{value}' must be in 'page:paragraph' format.");
  }

  private static string NormalizeLineRange(string value)
  {
    var parts = value.Split('-', 2);
    if (parts.Length == 2 && int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var end) && start <= end)
    {
      return value;
    }

    throw new DomainInvariantException($"Line range locator value '{value}' must be in 'startLine-endLine' format with start <= end.");
  }

  private static string NormalizeStructuralPath(string value)
  {
    return value
      .Replace('\\', '/')
      .Trim('/')
      .ToLowerInvariant();
  }
}

public sealed class ParserRun : AuditableEntity
{
  private const string AuditSubjectType = "ParserRun";

  public ParserRun(
    ParserRunId id,
    ProjectId projectId,
    ImportedSourceId importedSourceId,
    string parserKey,
    string parserVersion,
    string parserContractVersion,
    string parserContractHash,
    string sourceContentHash,
    string sourceHashAlgorithm,
    int sourceHashAlgorithmVersion,
    string createdBy,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    ImportedSourceId = importedSourceId;
    ParserKey = DomainGuards.NotNullOrWhiteSpace(parserKey, nameof(parserKey));
    ParserVersion = DomainGuards.NotNullOrWhiteSpace(parserVersion, nameof(parserVersion));
    ParserContractVersion = DomainGuards.NotNullOrWhiteSpace(parserContractVersion, nameof(parserContractVersion));
    ParserContractHash = DomainGuards.NotNullOrWhiteSpace(parserContractHash, nameof(parserContractHash));
    SourceContentHash = DomainGuards.NotNullOrWhiteSpace(sourceContentHash, nameof(sourceContentHash));
    SourceHashAlgorithm = DomainGuards.NotNullOrWhiteSpace(sourceHashAlgorithm, nameof(sourceHashAlgorithm));
    SourceHashAlgorithmVersion = sourceHashAlgorithmVersion > 0
      ? sourceHashAlgorithmVersion
      : throw new DomainInvariantException($"{nameof(sourceHashAlgorithmVersion)} must be greater than zero.");
    CreatedBy = DomainGuards.NotNullOrWhiteSpace(createdBy, nameof(createdBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    State = ParserRunState.Created;

    AppendAuditEvent(CreateAuditEvent("ParserRunCreated", createdBy, createdAtUtc, "Parser run created."));
  }

  public ParserRunId Id { get; }

  public ProjectId ProjectId { get; }

  public ImportedSourceId ImportedSourceId { get; }

  public string ParserKey { get; }

  public string ParserVersion { get; }

  public string ParserContractVersion { get; }

  public string ParserContractHash { get; }

  public string SourceContentHash { get; }

  public string SourceHashAlgorithm { get; }

  public int SourceHashAlgorithmVersion { get; }

  public string CreatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public ParserRunState State { get; private set; }

  public DateTimeOffset? StartedAtUtc { get; private set; }

  public DateTimeOffset? CompletedAtUtc { get; private set; }

  public string? FailureReason { get; private set; }

  protected override string SubjectType => AuditSubjectType;

  protected override string SubjectId => Id.ToString();

  internal static ParserRun Rehydrate(
    ParserRunId id,
    ProjectId projectId,
    ImportedSourceId importedSourceId,
    string parserKey,
    string parserVersion,
    string parserContractVersion,
    string parserContractHash,
    string sourceContentHash,
    string sourceHashAlgorithm,
    int sourceHashAlgorithmVersion,
    string createdBy,
    DateTimeOffset createdAtUtc,
    ParserRunState state,
    DateTimeOffset? startedAtUtc,
    DateTimeOffset? completedAtUtc,
    string? failureReason,
    IEnumerable<AuditEvent> auditTrail)
  {
    var run = new ParserRun(
      id,
      projectId,
      importedSourceId,
      parserKey,
      parserVersion,
      parserContractVersion,
      parserContractHash,
      sourceContentHash,
      sourceHashAlgorithm,
      sourceHashAlgorithmVersion,
      createdBy,
      createdAtUtc)
    {
      State = state,
      StartedAtUtc = startedAtUtc,
      CompletedAtUtc = completedAtUtc,
      FailureReason = failureReason,
    };

    run.RestoreAuditTrail(auditTrail);
    return run;
  }

  public void Start(DateTimeOffset startedAtUtc)
  {
    EnsureState(ParserRunState.Created, nameof(Start));

    State = ParserRunState.Running;
    StartedAtUtc = DomainGuards.NotDefault(startedAtUtc, nameof(startedAtUtc));
    AppendAuditEvent(CreateAuditEvent("ParserRunStarted", CreatedBy, startedAtUtc, "Parser run started."));
  }

  public void Complete(DateTimeOffset completedAtUtc)
  {
    EnsureState(ParserRunState.Running, nameof(Complete));

    State = ParserRunState.Completed;
    CompletedAtUtc = DomainGuards.NotDefault(completedAtUtc, nameof(completedAtUtc));
    AppendAuditEvent(CreateAuditEvent("ParserRunCompleted", CreatedBy, completedAtUtc, "Parser run completed."));
  }

  public void Fail(DateTimeOffset occurredAtUtc, string reason)
  {
    EnsureNonTerminal(nameof(Fail));

    State = ParserRunState.Failed;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureReason = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    AppendAuditEvent(CreateAuditEvent("ParserRunFailed", CreatedBy, occurredAtUtc, $"Parser run failed: {FailureReason}."));
  }

  public void Cancel(DateTimeOffset occurredAtUtc, string reason)
  {
    EnsureNonTerminal(nameof(Cancel));

    State = ParserRunState.Cancelled;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureReason = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    AppendAuditEvent(CreateAuditEvent("ParserRunCancelled", CreatedBy, occurredAtUtc, $"Parser run cancelled: {FailureReason}."));
  }

  private void EnsureState(ParserRunState expectedState, string transitionName)
  {
    if (State != expectedState)
    {
      throw new LifecycleTransitionException(nameof(ParserRun), State.ToString(), transitionName);
    }
  }

  private void EnsureNonTerminal(string transitionName)
  {
    if (State is ParserRunState.Completed or ParserRunState.Failed or ParserRunState.Cancelled)
    {
      throw new LifecycleTransitionException(nameof(ParserRun), State.ToString(), transitionName);
    }
  }
}

public sealed class FragmentCandidate : AuditableEntity
{
  private const string AuditSubjectType = "FragmentCandidate";
  private const int MaxExtractedTextLength = 100_000;

  public FragmentCandidate(
    FragmentCandidateId id,
    ParserRunId parserRunId,
    ProjectId projectId,
    ImportedSourceId importedSourceId,
    string sourceContentHash,
    FragmentLocator locator,
    int ordinal,
    ContentKind contentKind,
    string extractedText,
    ConfidenceBand confidenceBand,
    string parserKey,
    string parserContractVersion,
    DateTimeOffset createdAtUtc)
  {
    if (ordinal <= 0)
    {
      throw new DomainInvariantException($"{nameof(ordinal)} must be greater than zero.");
    }

    if (string.IsNullOrWhiteSpace(extractedText))
    {
      throw new DomainInvariantException($"{nameof(extractedText)} cannot be empty.");
    }

    if (extractedText.Length > MaxExtractedTextLength)
    {
      throw new DomainInvariantException($"{nameof(extractedText)} length exceeds maximum of {MaxExtractedTextLength} characters.");
    }

    Id = id;
    ParserRunId = parserRunId;
    ProjectId = projectId;
    ImportedSourceId = importedSourceId;
    SourceContentHash = DomainGuards.NotNullOrWhiteSpace(sourceContentHash, nameof(sourceContentHash));
    Locator = locator ?? throw new ArgumentNullException(nameof(locator));
    Ordinal = ordinal;
    ContentKind = contentKind;
    ExtractedText = extractedText;
    TextLength = extractedText.Length;
    ConfidenceBand = confidenceBand;
    IdentityKey = ComputeIdentityKey(importedSourceId, parserKey, parserContractVersion, locator);
    IdentityKeyHash = ComputeHash(IdentityKey);
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));

    AppendAuditEvent(CreateAuditEvent(
      "FragmentCandidateGenerated",
      "system",
      createdAtUtc,
      $"Fragment candidate {contentKind} generated at ordinal {ordinal} with locator {locator.NormalizedValue}."));
  }

  public FragmentCandidateId Id { get; }

  public ParserRunId ParserRunId { get; }

  public ProjectId ProjectId { get; }

  public ImportedSourceId ImportedSourceId { get; }

  public string SourceContentHash { get; }

  public FragmentLocator Locator { get; }

  public int Ordinal { get; }

  public ContentKind ContentKind { get; }

  public string ExtractedText { get; }

  public int TextLength { get; }

  public ConfidenceBand ConfidenceBand { get; }

  public string IdentityKey { get; }

  public string IdentityKeyHash { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  protected override string SubjectType => AuditSubjectType;

  protected override string SubjectId => Id.ToString();

  internal static FragmentCandidate Rehydrate(
    FragmentCandidateId id,
    ParserRunId parserRunId,
    ProjectId projectId,
    ImportedSourceId importedSourceId,
    string sourceContentHash,
    FragmentLocator locator,
    int ordinal,
    ContentKind contentKind,
    string extractedText,
    int textLength,
    ConfidenceBand confidenceBand,
    string identityKey,
    string identityKeyHash,
    DateTimeOffset createdAtUtc,
    IEnumerable<AuditEvent> auditTrail)
  {
    var candidate = new FragmentCandidate(
      id,
      parserRunId,
      projectId,
      importedSourceId,
      sourceContentHash,
      locator,
      ordinal,
      contentKind,
      extractedText,
      confidenceBand,
      string.Empty,
      string.Empty,
      createdAtUtc)
    {
    };

    candidate.RestoreAuditTrail(auditTrail);
    return candidate;
  }

  public static string ComputeIdentityKey(
    ImportedSourceId importedSourceId,
    string parserKey,
    string parserContractVersion,
    FragmentLocator locator)
  {
    return $"{importedSourceId}:{parserKey}@{parserContractVersion}:{locator.LocatorType}:{locator.NormalizedValue}";
  }

  private static string ComputeHash(string value)
  {
    var bytes = Encoding.UTF8.GetBytes(value);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }
}
