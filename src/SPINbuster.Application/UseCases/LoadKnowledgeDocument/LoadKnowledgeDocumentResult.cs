using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeDocument;

public sealed record LoadKnowledgeDocumentResult(
  KnowledgeDocumentId KnowledgeDocumentId,
  ProjectId ProjectId,
  KnowledgeDocumentType DocumentType,
  string CanonicalTitle,
  string? ExternalReferenceNumber,
  string? DisciplineOrCategory,
  KnowledgeDocumentRevisionId? CurrentAuthoritativeRevisionId,
  KnowledgeDocumentLifecycle Lifecycle,
  IReadOnlyCollection<KnowledgeDocumentRevisionSummary> Revisions,
  IReadOnlyCollection<KnowledgeAuditEntrySnapshot> AuditHistory);

public sealed record KnowledgeDocumentRevisionSummary(
  KnowledgeDocumentRevisionId KnowledgeDocumentRevisionId,
  string RevisionLabel,
  KnowledgeRevisionLifecycle Lifecycle,
  KnowledgeVerificationStatus VerificationStatus,
  KnowledgeSourceAuthorityLevel SourceAuthority,
  DateTimeOffset ReceivedAtUtc);

public sealed record KnowledgeAuditEntrySnapshot(
  AuditEventId AuditEventId,
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
