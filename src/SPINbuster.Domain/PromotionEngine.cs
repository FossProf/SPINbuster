namespace SPINbuster.Domain;

public enum PromotionDiagnosticStatus
{
  Eligible = 0,
  Promoted = 1,
  Failed = 2,
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
