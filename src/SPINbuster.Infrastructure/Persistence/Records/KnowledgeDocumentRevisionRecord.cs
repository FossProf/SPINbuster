using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class KnowledgeDocumentRevisionRecord
{
  public List<KnowledgeCitationRecord> Citations { get; } = [];

  public KnowledgeDocumentRevisionId Id { get; set; }

  public KnowledgeDocumentId KnowledgeDocumentId { get; set; }

  public KnowledgeSourceId KnowledgeSourceId { get; set; }

  public string RevisionLabel { get; set; } = string.Empty;

  public DateOnly? EffectiveDate { get; set; }

  public DateTimeOffset ReceivedAtUtc { get; set; }

  public KnowledgeSourceAuthorityLevel SourceAuthority { get; set; }

  public KnowledgeVerificationStatus VerificationStatus { get; set; }

  public string ContentHash { get; set; } = string.Empty;

  public string MetadataHash { get; set; } = string.Empty;

  public KnowledgeDocumentRevisionId? SupersedesRevisionId { get; set; }

  public KnowledgeDocumentRevisionId? SupersededByRevisionId { get; set; }

  public string? SourceSystemReference { get; set; }

  public string? DescriptiveNotes { get; set; }

  public DateTimeOffset CreatedAtUtc { get; set; }

  public KnowledgeIngestionStatus IngestionStatus { get; set; }

  public KnowledgeRevisionLifecycle Lifecycle { get; set; }
}
