using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeRevisionHistory;

public sealed record LoadKnowledgeRevisionHistoryResult(
  KnowledgeDocumentId KnowledgeDocumentId,
  IReadOnlyCollection<KnowledgeRevisionHistoryEntry> Revisions);

public sealed record KnowledgeRevisionHistoryEntry(
  KnowledgeDocumentRevisionId KnowledgeDocumentRevisionId,
  string RevisionLabel,
  DateOnly? EffectiveDate,
  DateTimeOffset ReceivedAtUtc,
  KnowledgeSourceAuthorityLevel SourceAuthority,
  KnowledgeVerificationStatus VerificationStatus,
  KnowledgeRevisionLifecycle Lifecycle,
  string ContentHash,
  string MetadataHash,
  KnowledgeDocumentRevisionId? SupersedesRevisionId,
  KnowledgeDocumentRevisionId? SupersededByRevisionId,
  string? SourceSystemReference,
  string? DescriptiveNotes,
  DateTimeOffset CreatedAtUtc,
  KnowledgeIngestionStatus IngestionStatus);
