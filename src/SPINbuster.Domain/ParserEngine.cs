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

public enum FragmentCandidateReviewState
{
  Generated = 0,
  HumanAccepted = 1,
  Rejected = 2,
}

/// <summary>
/// Deterministic parser execution status. Replaces the boolean success flag to
/// distinguish full completion from degraded-but-successful extraction.
/// </summary>
public enum ParserExecutionStatus
{
  Completed = 0,
  CompletedWithWarnings = 1,
  Failed = 2,
}

/// <summary>
/// Diagnostic severity. Info and Warning are carried on successful results.
/// Error is reserved for Failed status only.
/// </summary>
public enum DiagnosticSeverity
{
  Info = 0,
  Warning = 1,
  Error = 2,
}

/// <summary>
/// How a diagnostic references a specific fragment within a parser run.
/// Application resolves the reference to a FragmentCandidate.IdentityKey after construction.
/// </summary>
public enum DiagnosticRefType
{
  Ordinal = 0,
  NormalizedLocator = 1,
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

/// <summary>
/// Immutable diagnostic record attached to a parser run. Diagnostics are durable
/// parser evidence, not independently authoritative aggregates. They carry no
/// audit lifecycle and no review state.
/// </summary>
public sealed class ParserDiagnostic
{
  private const int MaxCodeLength = 100;
  private const int MaxMessageLength = 500;

  public ParserDiagnostic(
    ParserDiagnosticId id,
    ParserRunId parserRunId,
    DiagnosticSeverity severity,
    string code,
    string message,
    DateTimeOffset createdAtUtc,
    DiagnosticRefType? candidateRefType = null,
    string? candidateRefValue = null,
    FragmentLocatorType? locatorType = null,
    string? locatorValue = null)
  {
    if (string.IsNullOrWhiteSpace(code))
    {
      throw new DomainInvariantException($"{nameof(code)} cannot be empty.");
    }

    if (code.Length > MaxCodeLength)
    {
      throw new DomainInvariantException($"{nameof(code)} length exceeds maximum of {MaxCodeLength} characters.");
    }

    if (string.IsNullOrWhiteSpace(message))
    {
      throw new DomainInvariantException($"{nameof(message)} cannot be empty.");
    }

    if (message.Length > MaxMessageLength)
    {
      throw new DomainInvariantException($"{nameof(message)} length exceeds maximum of {MaxMessageLength} characters.");
    }

    if (candidateRefType.HasValue && string.IsNullOrWhiteSpace(candidateRefValue))
    {
      throw new DomainInvariantException($"{nameof(candidateRefValue)} must be non-empty when {nameof(candidateRefType)} is set.");
    }

    Id = id;
    ParserRunId = parserRunId;
    Severity = severity;
    Code = code.Trim();
    Message = message.Trim();
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    CandidateRefType = candidateRefType;
    CandidateRefValue = candidateRefValue?.Trim();
    LocatorType = locatorType;
    LocatorValue = locatorValue?.Trim();
  }

  public ParserDiagnosticId Id { get; }

  public ParserRunId ParserRunId { get; }

  public DiagnosticSeverity Severity { get; }

  public string Code { get; }

  public string Message { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public DiagnosticRefType? CandidateRefType { get; }

  public string? CandidateRefValue { get; }

  public FragmentLocatorType? LocatorType { get; }

  public string? LocatorValue { get; }
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
  private const int MaxReviewNotesLength = 2_000;

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

  private FragmentCandidate()
  {
  }

  public void Accept(string reviewedBy, DateTimeOffset reviewedAtUtc, string? reviewNotes)
  {
    EnsureReviewNotDecided(nameof(Accept));

    ReviewState = FragmentCandidateReviewState.HumanAccepted;
    ReviewedBy = DomainGuards.NotNullOrWhiteSpace(reviewedBy, nameof(reviewedBy));
    ReviewedAtUtc = DomainGuards.NotDefault(reviewedAtUtc, nameof(reviewedAtUtc));
    ReviewNotes = NormalizeReviewNotes(reviewNotes);
    AppendAuditEvent(CreateAuditEvent(
      "FragmentCandidateHumanAccepted",
      ReviewedBy,
      reviewedAtUtc,
      "Fragment candidate recorded as human-accepted review intent only."));
  }

  public void Reject(string reviewedBy, DateTimeOffset reviewedAtUtc, string? reviewNotes)
  {
    EnsureReviewNotDecided(nameof(Reject));

    ReviewState = FragmentCandidateReviewState.Rejected;
    ReviewedBy = DomainGuards.NotNullOrWhiteSpace(reviewedBy, nameof(reviewedBy));
    ReviewedAtUtc = DomainGuards.NotDefault(reviewedAtUtc, nameof(reviewedAtUtc));
    ReviewNotes = NormalizeReviewNotes(reviewNotes);
    AppendAuditEvent(CreateAuditEvent(
      "FragmentCandidateRejected",
      ReviewedBy,
      reviewedAtUtc,
      "Fragment candidate rejected during review."));
  }

  private void EnsureReviewNotDecided(string transitionName)
  {
    if (ReviewState is FragmentCandidateReviewState.HumanAccepted or FragmentCandidateReviewState.Rejected)
    {
      throw new LifecycleTransitionException(nameof(FragmentCandidate), ReviewState.ToString(), transitionName);
    }
  }

  private static string? NormalizeReviewNotes(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    var trimmed = value.Trim();
    return trimmed.Length > MaxReviewNotesLength
      ? throw new DomainInvariantException($"ReviewNotes length exceeds maximum of {MaxReviewNotesLength} characters.")
      : trimmed;
  }

  public FragmentCandidateId Id { get; private set; }

  public ParserRunId ParserRunId { get; private set; }

  public ProjectId ProjectId { get; private set; }

  public ImportedSourceId ImportedSourceId { get; private set; }

  public string SourceContentHash { get; private set; } = string.Empty;

  public FragmentLocator Locator { get; private set; } = null!;

  public int Ordinal { get; private set; }

  public ContentKind ContentKind { get; private set; }

  public string ExtractedText { get; private set; } = string.Empty;

  public int TextLength { get; private set; }

  public ConfidenceBand ConfidenceBand { get; private set; }

  public string IdentityKey { get; private set; } = string.Empty;

  public string IdentityKeyHash { get; private set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; private set; }

  public FragmentCandidateReviewState ReviewState { get; private set; }

  public string? ReviewedBy { get; private set; }

  public DateTimeOffset? ReviewedAtUtc { get; private set; }

  public string? ReviewNotes { get; private set; }

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
    FragmentCandidateReviewState reviewState,
    string? reviewedBy,
    DateTimeOffset? reviewedAtUtc,
    string? reviewNotes,
    IEnumerable<AuditEvent> auditTrail)
  {
    var candidate = new FragmentCandidate
    {
      Id = id,
      ParserRunId = parserRunId,
      ProjectId = projectId,
      ImportedSourceId = importedSourceId,
      SourceContentHash = sourceContentHash,
      Locator = locator,
      Ordinal = ordinal,
      ContentKind = contentKind,
      ExtractedText = extractedText,
      TextLength = textLength,
      ConfidenceBand = confidenceBand,
      IdentityKey = identityKey,
      IdentityKeyHash = identityKeyHash,
      CreatedAtUtc = createdAtUtc,
      ReviewState = reviewState,
      ReviewedBy = reviewedBy,
      ReviewedAtUtc = reviewedAtUtc,
      ReviewNotes = reviewNotes,
    };

    candidate.ValidateRehydratedState();
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

  private void ValidateRehydratedState()
  {
    if (Id.Value == Guid.Empty)
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty Id.");
    }

    if (ParserRunId.Value == Guid.Empty)
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty ParserRunId.");
    }

    if (ProjectId.Value == Guid.Empty)
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty ProjectId.");
    }

    if (ImportedSourceId.Value == Guid.Empty)
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty ImportedSourceId.");
    }

    if (string.IsNullOrWhiteSpace(SourceContentHash))
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty SourceContentHash.");
    }

    if (Locator is null)
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has a null Locator.");
    }

    if (Ordinal <= 0)
    {
      throw new DomainInvariantException($"Rehydrated FragmentCandidate has a non-positive Ordinal value of {Ordinal}.");
    }

    if (string.IsNullOrWhiteSpace(ExtractedText))
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty ExtractedText.");
    }

    if (TextLength != ExtractedText.Length)
    {
      throw new DomainInvariantException($"Rehydrated FragmentCandidate TextLength {TextLength} does not match ExtractedText length {ExtractedText.Length}.");
    }

    if (string.IsNullOrWhiteSpace(IdentityKey))
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty IdentityKey.");
    }

    if (string.IsNullOrWhiteSpace(IdentityKeyHash))
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has an empty IdentityKeyHash.");
    }

    if (IdentityKeyHash != ComputeHash(IdentityKey))
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate IdentityKeyHash does not match the expected hash of IdentityKey.");
    }

    if (CreatedAtUtc == default)
    {
      throw new DomainInvariantException("Rehydrated FragmentCandidate has a default CreatedAtUtc.");
    }

    if (ReviewState is FragmentCandidateReviewState.HumanAccepted or FragmentCandidateReviewState.Rejected)
    {
      if (string.IsNullOrWhiteSpace(ReviewedBy))
      {
        throw new DomainInvariantException("Rehydrated FragmentCandidate has a review disposition but an empty ReviewedBy.");
      }

      if (ReviewedAtUtc is null || ReviewedAtUtc.Value == default)
      {
        throw new DomainInvariantException("Rehydrated FragmentCandidate has a review disposition but a default ReviewedAtUtc.");
      }
    }
  }
}
