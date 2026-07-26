using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class PromotionProvenanceRecord
{
  public PromotionProvenanceId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public KnowledgeDocumentRevisionId PromotedRevisionId { get; set; }

  public PromotionDiagnosticId DiagnosticId { get; set; }

  public FragmentCandidateId FragmentCandidateId { get; set; }

  public string FragmentSourceContentHash { get; set; } = string.Empty;

  public FragmentCandidateReviewState ReviewState { get; set; }

  public string? ReviewedBy { get; set; }

  public DateTimeOffset? ReviewedAtUtc { get; set; }

  public ParserRunId ParserRunId { get; set; }

  public string ParserKey { get; set; } = string.Empty;

  public string ParserVersion { get; set; } = string.Empty;

  public string ParserContractVersion { get; set; } = string.Empty;

  public string ParserContractHash { get; set; } = string.Empty;

  public ImportedSourceId ImportedSourceId { get; set; }

  public string ImportedSourceContentHash { get; set; } = string.Empty;

  public string PromotionIdentityHash { get; set; } = string.Empty;

  public PromotionAttemptId PromotionAttemptId { get; set; }

  public string PromotedBy { get; set; } = string.Empty;

  public DateTimeOffset PromotedAtUtc { get; set; }

  public long PromotedAtUtcTicks { get; set; }

  public string AuthorityBasis { get; set; } = string.Empty;

  public string PolicyVersion { get; set; } = string.Empty;
}
