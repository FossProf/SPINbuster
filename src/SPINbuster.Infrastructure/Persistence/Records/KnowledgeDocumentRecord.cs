using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class KnowledgeDocumentRecord
{
  public List<KnowledgeDocumentRevisionRecord> Revisions { get; } = [];

  public KnowledgeDocumentId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public KnowledgeDocumentType DocumentType { get; set; }

  public string CanonicalTitle { get; set; } = string.Empty;

  public string? ExternalReferenceNumber { get; set; }

  public string? DisciplineOrCategory { get; set; }

  public KnowledgeDocumentRevisionId? CurrentAuthoritativeRevisionId { get; set; }

  public KnowledgeDocumentLifecycle Lifecycle { get; set; }

  public string CreatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }
}
