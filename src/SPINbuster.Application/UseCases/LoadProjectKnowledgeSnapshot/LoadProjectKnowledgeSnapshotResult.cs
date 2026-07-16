using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;

public sealed record LoadProjectKnowledgeSnapshotResult(
  ProjectId ProjectId,
  IReadOnlyList<ProjectKnowledgeDocumentSnapshot> Documents,
  IReadOnlyList<ProjectKnowledgeRelationshipSnapshot> Relationships);

public sealed record ProjectKnowledgeDocumentSnapshot(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeDocumentType DocumentType,
  string CanonicalTitle,
  string? ExternalReferenceNumber,
  string? DisciplineOrCategory,
  KnowledgeDocumentLifecycle Lifecycle,
  KnowledgeDocumentRevisionId? CurrentAuthoritativeRevisionId,
  IReadOnlyList<ProjectKnowledgeRevisionSnapshot> Revisions,
  IReadOnlyList<ProjectKnowledgeAuditEntry> AuditHistory);

public sealed record ProjectKnowledgeRevisionSnapshot(
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
  KnowledgeIngestionStatus IngestionStatus,
  IReadOnlyList<ProjectKnowledgeCitationSnapshot> Citations,
  IReadOnlyList<ProjectKnowledgeAuditEntry> AuditHistory);

public sealed record ProjectKnowledgeCitationSnapshot(
  KnowledgeCitationId KnowledgeCitationId,
  KnowledgeCitationLocationType LocatorType,
  string LocatorValue,
  string RevisionContentHash,
  DateTimeOffset CreatedAtUtc,
  string? QuotedOrSummarizedText);

public sealed record ProjectKnowledgeRelationshipSnapshot(
  KnowledgeRelationshipId KnowledgeRelationshipId,
  KnowledgeRelationshipType RelationshipType,
  ProjectKnowledgeSubjectSnapshot Source,
  ProjectKnowledgeSubjectSnapshot Target,
  string EvidenceOrRationale,
  KnowledgeVerificationStatus VerificationStatus,
  DateTimeOffset CreatedAtUtc,
  IReadOnlyList<ProjectKnowledgeAuditEntry> AuditHistory);

public sealed record ProjectKnowledgeSubjectSnapshot(
  KnowledgeSubjectKind SubjectKind,
  string StableKey,
  KnowledgeDocumentId? KnowledgeDocumentId,
  KnowledgeDocumentRevisionId? KnowledgeDocumentRevisionId);

public sealed record ProjectKnowledgeAuditEntry(
  AuditEventId AuditEventId,
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
