using System.Security.Cryptography;
using System.Text;

namespace SPINbuster.Domain;

public enum PromotionDiagnosticStatus
{
  Eligible = 0,
  Promoted = 1,
  Failed = 2,
}

public enum PromotionAttemptOutcome
{
  Promoted = 0,
  RetryablePreconditionFailure = 1,
  PermanentInvariantViolation = 2,
  ConcurrencyConflict = 3,
  UnexpectedFailure = 4,
}

public readonly record struct PromotionRecordId
{
  public PromotionRecordId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static PromotionRecordId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct PromotionAttemptId
{
  public PromotionAttemptId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static PromotionAttemptId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public sealed record PromotionIdentity
{
  public const string ContractVersion = "1.0.0";

  public PromotionIdentity(
    ProjectId projectId,
    KnowledgeDocumentType documentType,
    string canonicalTitle,
    string? externalReferenceNumber,
    string? disciplineOrCategory,
    string fragmentIdentityKey)
  {
    ProjectId = projectId;
    DocumentType = documentType;
    CanonicalTitle = canonicalTitle.Trim();
    ExternalReferenceNumber = externalReferenceNumber?.Trim();
    DisciplineOrCategory = disciplineOrCategory?.Trim();
    FragmentIdentityKey = DomainGuards.NotNullOrWhiteSpace(fragmentIdentityKey, nameof(fragmentIdentityKey));
    Hash = ComputeHash();
  }

  public ProjectId ProjectId { get; }

  public KnowledgeDocumentType DocumentType { get; }

  public string CanonicalTitle { get; }

  public string? ExternalReferenceNumber { get; }

  public string? DisciplineOrCategory { get; }

  public string FragmentIdentityKey { get; }

  public string Hash { get; }

  private string ComputeHash()
  {
    var parts = new[]
    {
      ProjectId.Value.ToString("D"),
      DocumentType.ToString(),
      CanonicalTitle,
      ExternalReferenceNumber ?? string.Empty,
      DisciplineOrCategory ?? string.Empty,
      FragmentIdentityKey,
      ContractVersion,
    };

    var combined = string.Join("|", parts);
    var bytes = Encoding.UTF8.GetBytes(combined);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }
}

public sealed class PromotionRecord
{
  public PromotionRecord(
    PromotionRecordId id,
    PromotionIdentity identity,
    KnowledgeDocumentId targetDocumentId,
    DateTimeOffset firstPromotedAtUtc)
  {
    Id = id;
    Identity = identity ?? throw new ArgumentNullException(nameof(identity));
    TargetDocumentId = targetDocumentId;
    FirstPromotedAtUtc = DomainGuards.NotDefault(firstPromotedAtUtc, nameof(firstPromotedAtUtc));
  }

  private PromotionRecord()
  {
  }

  public PromotionRecordId Id { get; private set; }

  public PromotionIdentity Identity { get; private set; } = null!;

  public KnowledgeDocumentId TargetDocumentId { get; private set; }

  public DateTimeOffset FirstPromotedAtUtc { get; private set; }

  public PromotionAttemptId? LatestAttemptId { get; private set; }

  public void UpdateLatestAttempt(PromotionAttemptId attemptId)
  {
    LatestAttemptId = attemptId;
  }

  internal static PromotionRecord Rehydrate(
    PromotionRecordId id,
    PromotionIdentity identity,
    KnowledgeDocumentId targetDocumentId,
    DateTimeOffset firstPromotedAtUtc,
    PromotionAttemptId? latestAttemptId)
  {
    var record = new PromotionRecord
    {
      Id = id,
      Identity = identity,
      TargetDocumentId = targetDocumentId,
      FirstPromotedAtUtc = firstPromotedAtUtc,
      LatestAttemptId = latestAttemptId,
    };

    return record;
  }
}

public sealed class PromotionAttempt
{
  public PromotionAttempt(
    PromotionAttemptId id,
    PromotionRecordId recordId,
    PromotionAttemptOutcome outcome,
    PromotionDiagnosticId diagnosticId,
    FragmentCandidateId fragmentCandidateId,
    string contentHash,
    DateTimeOffset attemptedAtUtc,
    string? failureReason = null)
  {
    Id = id;
    RecordId = recordId;
    Outcome = outcome;
    DiagnosticId = diagnosticId;
    FragmentCandidateId = fragmentCandidateId;
    ContentHash = DomainGuards.NotNullOrWhiteSpace(contentHash, nameof(contentHash));
    AttemptedAtUtc = DomainGuards.NotDefault(attemptedAtUtc, nameof(attemptedAtUtc));
    FailureReason = failureReason?.Trim();
  }

  private PromotionAttempt()
  {
  }

  public PromotionAttemptId Id { get; private set; }

  public PromotionRecordId RecordId { get; private set; }

  public PromotionAttemptOutcome Outcome { get; private set; }

  public PromotionDiagnosticId DiagnosticId { get; private set; }

  public FragmentCandidateId FragmentCandidateId { get; private set; }

  public string ContentHash { get; private set; } = string.Empty;

  public DateTimeOffset AttemptedAtUtc { get; private set; }

  public string? FailureReason { get; private set; }

  public bool IsSuccessful => Outcome == PromotionAttemptOutcome.Promoted;

  internal static PromotionAttempt Rehydrate(
    PromotionAttemptId id,
    PromotionRecordId recordId,
    PromotionAttemptOutcome outcome,
    PromotionDiagnosticId diagnosticId,
    FragmentCandidateId fragmentCandidateId,
    string contentHash,
    DateTimeOffset attemptedAtUtc,
    string? failureReason)
  {
    return new PromotionAttempt
    {
      Id = id,
      RecordId = recordId,
      Outcome = outcome,
      DiagnosticId = diagnosticId,
      FragmentCandidateId = fragmentCandidateId,
      ContentHash = contentHash,
      AttemptedAtUtc = attemptedAtUtc,
      FailureReason = failureReason,
    };
  }
}

public readonly record struct PromotionDiagnosticId
{
  public PromotionDiagnosticId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static PromotionDiagnosticId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct PromotionProvenanceId
{
  public PromotionProvenanceId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static PromotionProvenanceId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public sealed class PromotionProvenance
{
  public PromotionProvenance(
    PromotionProvenanceId id,
    ProjectId projectId,
    KnowledgeDocumentRevisionId promotedRevisionId,
    PromotionDiagnosticId diagnosticId,
    FragmentCandidateId fragmentCandidateId,
    string fragmentSourceContentHash,
    FragmentCandidateReviewState reviewState,
    string? reviewedBy,
    DateTimeOffset? reviewedAtUtc,
    ParserRunId parserRunId,
    string parserKey,
    string parserVersion,
    string parserContractVersion,
    string parserContractHash,
    ImportedSourceId importedSourceId,
    string importedSourceContentHash,
    string promotionIdentityHash,
    PromotionAttemptId promotionAttemptId,
    string promotedBy,
    DateTimeOffset promotedAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    PromotedRevisionId = promotedRevisionId;
    DiagnosticId = diagnosticId;
    FragmentCandidateId = fragmentCandidateId;
    FragmentSourceContentHash = DomainGuards.NotNullOrWhiteSpace(fragmentSourceContentHash, nameof(fragmentSourceContentHash));
    ReviewState = reviewState;
    ReviewedBy = reviewedBy;
    ReviewedAtUtc = reviewedAtUtc;
    ParserRunId = parserRunId;
    ParserKey = DomainGuards.NotNullOrWhiteSpace(parserKey, nameof(parserKey));
    ParserVersion = DomainGuards.NotNullOrWhiteSpace(parserVersion, nameof(parserVersion));
    ParserContractVersion = DomainGuards.NotNullOrWhiteSpace(parserContractVersion, nameof(parserContractVersion));
    ParserContractHash = DomainGuards.NotNullOrWhiteSpace(parserContractHash, nameof(parserContractHash));
    ImportedSourceId = importedSourceId;
    ImportedSourceContentHash = DomainGuards.NotNullOrWhiteSpace(importedSourceContentHash, nameof(importedSourceContentHash));
    PromotionIdentityHash = DomainGuards.NotNullOrWhiteSpace(promotionIdentityHash, nameof(promotionIdentityHash));
    PromotionAttemptId = promotionAttemptId;
    PromotedBy = DomainGuards.NotNullOrWhiteSpace(promotedBy, nameof(promotedBy));
    PromotedAtUtc = DomainGuards.NotDefault(promotedAtUtc, nameof(promotedAtUtc));
  }

  private PromotionProvenance()
  {
  }

  public PromotionProvenanceId Id { get; private set; }

  public ProjectId ProjectId { get; private set; }

  public KnowledgeDocumentRevisionId PromotedRevisionId { get; private set; }

  public PromotionDiagnosticId DiagnosticId { get; private set; }

  public FragmentCandidateId FragmentCandidateId { get; private set; }

  public string FragmentSourceContentHash { get; private set; } = string.Empty;

  public FragmentCandidateReviewState ReviewState { get; private set; }

  public string? ReviewedBy { get; private set; }

  public DateTimeOffset? ReviewedAtUtc { get; private set; }

  public ParserRunId ParserRunId { get; private set; }

  public string ParserKey { get; private set; } = string.Empty;

  public string ParserVersion { get; private set; } = string.Empty;

  public string ParserContractVersion { get; private set; } = string.Empty;

  public string ParserContractHash { get; private set; } = string.Empty;

  public ImportedSourceId ImportedSourceId { get; private set; }

  public string ImportedSourceContentHash { get; private set; } = string.Empty;

  public string PromotionIdentityHash { get; private set; } = string.Empty;

  public PromotionAttemptId PromotionAttemptId { get; private set; }

  public string PromotedBy { get; private set; } = string.Empty;

  public DateTimeOffset PromotedAtUtc { get; private set; }

  public static PromotionProvenance Rehydrate(
    PromotionProvenanceId id,
    ProjectId projectId,
    KnowledgeDocumentRevisionId promotedRevisionId,
    PromotionDiagnosticId diagnosticId,
    FragmentCandidateId fragmentCandidateId,
    string fragmentSourceContentHash,
    FragmentCandidateReviewState reviewState,
    string? reviewedBy,
    DateTimeOffset? reviewedAtUtc,
    ParserRunId parserRunId,
    string parserKey,
    string parserVersion,
    string parserContractVersion,
    string parserContractHash,
    ImportedSourceId importedSourceId,
    string importedSourceContentHash,
    string promotionIdentityHash,
    PromotionAttemptId promotionAttemptId,
    string promotedBy,
    DateTimeOffset promotedAtUtc)
  {
    return new PromotionProvenance(
      id,
      projectId,
      promotedRevisionId,
      diagnosticId,
      fragmentCandidateId,
      fragmentSourceContentHash,
      reviewState,
      reviewedBy,
      reviewedAtUtc,
      parserRunId,
      parserKey,
      parserVersion,
      parserContractVersion,
      parserContractHash,
      importedSourceId,
      importedSourceContentHash,
      promotionIdentityHash,
      promotionAttemptId,
      promotedBy,
      promotedAtUtc);
  }
}

public sealed class PromotionDiagnostic
{
  private const int MaxFailureReasonLength = 2_000;

  public PromotionDiagnostic(
    PromotionDiagnosticId id,
    FragmentCandidateId fragmentCandidateId,
    ParserRunId parserRunId,
    ProjectId projectId,
    DateTimeOffset promotedAtUtc)
  {
    Id = id;
    FragmentCandidateId = fragmentCandidateId;
    ParserRunId = parserRunId;
    ProjectId = projectId;
    PromotedAtUtc = DomainGuards.NotDefault(promotedAtUtc, nameof(promotedAtUtc));
    Status = PromotionDiagnosticStatus.Eligible;
  }

  public PromotionDiagnosticId Id { get; }

  public FragmentCandidateId FragmentCandidateId { get; }

  public ParserRunId ParserRunId { get; }

  public ProjectId ProjectId { get; }

  public DateTimeOffset PromotedAtUtc { get; }

  public PromotionDiagnosticStatus Status { get; private set; }

  public string? FailureReason { get; private set; }

  public KnowledgeDocumentId? KnowledgeDocumentId { get; private set; }

  public KnowledgeDocumentRevisionId? KnowledgeDocumentRevisionId { get; private set; }

  public KnowledgeCitationId? KnowledgeCitationId { get; private set; }

  public bool SupersededExistingRevision { get; private set; }

  public KnowledgeDocumentRevisionId? SupersededRevisionId { get; private set; }

  internal static PromotionDiagnostic Rehydrate(
    PromotionDiagnosticId id,
    FragmentCandidateId fragmentCandidateId,
    ParserRunId parserRunId,
    ProjectId projectId,
    DateTimeOffset promotedAtUtc,
    PromotionDiagnosticStatus status,
    string? failureReason,
    KnowledgeDocumentId? knowledgeDocumentId,
    KnowledgeDocumentRevisionId? knowledgeDocumentRevisionId,
    KnowledgeCitationId? knowledgeCitationId,
    bool supersededExistingRevision,
    KnowledgeDocumentRevisionId? supersededRevisionId)
  {
    return new PromotionDiagnostic(id, fragmentCandidateId, parserRunId, projectId, promotedAtUtc)
    {
      Status = status,
      FailureReason = NormalizeOptional(failureReason),
      KnowledgeDocumentId = knowledgeDocumentId,
      KnowledgeDocumentRevisionId = knowledgeDocumentRevisionId,
      KnowledgeCitationId = knowledgeCitationId,
      SupersededExistingRevision = supersededExistingRevision,
      SupersededRevisionId = supersededRevisionId,
    };
  }

  public void RecordSuccess(
    KnowledgeDocumentId knowledgeDocumentId,
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    KnowledgeCitationId knowledgeCitationId,
    bool supersededExistingRevision,
    KnowledgeDocumentRevisionId? supersededRevisionId)
  {
    if (Status is not PromotionDiagnosticStatus.Eligible)
    {
      throw new LifecycleTransitionException(nameof(PromotionDiagnostic), Status.ToString(), nameof(RecordSuccess));
    }

    Status = PromotionDiagnosticStatus.Promoted;
    KnowledgeDocumentId = knowledgeDocumentId;
    KnowledgeDocumentRevisionId = knowledgeDocumentRevisionId;
    KnowledgeCitationId = knowledgeCitationId;
    SupersededExistingRevision = supersededExistingRevision;
    SupersededRevisionId = supersededRevisionId;
  }

  public void RecordFailure(string reason)
  {
    if (Status is not PromotionDiagnosticStatus.Eligible)
    {
      throw new LifecycleTransitionException(nameof(PromotionDiagnostic), Status.ToString(), nameof(RecordFailure));
    }

    Status = PromotionDiagnosticStatus.Failed;
    FailureReason = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    if (FailureReason.Length > MaxFailureReasonLength)
    {
      throw new DomainInvariantException($"{nameof(FailureReason)} length exceeds maximum of {MaxFailureReasonLength} characters.");
    }
  }

  private static string? NormalizeOptional(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}
