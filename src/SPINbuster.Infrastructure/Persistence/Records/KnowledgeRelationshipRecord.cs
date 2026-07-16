using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class KnowledgeRelationshipRecord
{
  public KnowledgeRelationshipId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public KnowledgeSubjectKind SourceKind { get; set; }

  public string SourceKey { get; set; } = string.Empty;

  public KnowledgeDocumentId? SourceDocumentId { get; set; }

  public KnowledgeDocumentRevisionId? SourceRevisionId { get; set; }

  public KnowledgeSubjectKind TargetKind { get; set; }

  public string TargetKey { get; set; } = string.Empty;

  public KnowledgeDocumentId? TargetDocumentId { get; set; }

  public KnowledgeDocumentRevisionId? TargetRevisionId { get; set; }

  public KnowledgeRelationshipType RelationshipType { get; set; }

  public string EvidenceOrRationale { get; set; } = string.Empty;

  public string CreatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public KnowledgeVerificationStatus VerificationStatus { get; set; }
}
