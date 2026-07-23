using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class PromotionDiagnosticRecord
{
  public PromotionDiagnosticId Id { get; set; }

  public FragmentCandidateId FragmentCandidateId { get; set; }

  public ParserRunId ParserRunId { get; set; }

  public ProjectId ProjectId { get; set; }

  public DateTimeOffset PromotedAtUtc { get; set; }

  public PromotionDiagnosticStatus Status { get; set; }

  public string? FailureReason { get; set; }

  public KnowledgeDocumentId? KnowledgeDocumentId { get; set; }

  public KnowledgeDocumentRevisionId? KnowledgeDocumentRevisionId { get; set; }

  public KnowledgeCitationId? KnowledgeCitationId { get; set; }

  public bool SupersededExistingRevision { get; set; }

  public KnowledgeDocumentRevisionId? SupersededRevisionId { get; set; }
}
