using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class KnowledgeCitationRecord
{
  public KnowledgeCitationId Id { get; set; }

  public KnowledgeDocumentRevisionId CitedRevisionId { get; set; }

  public KnowledgeCitationLocationType LocatorType { get; set; }

  public string LocatorValue { get; set; } = string.Empty;

  public string RevisionContentHash { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public string? QuotedOrSummarizedText { get; set; }
}
