using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;

public sealed record LoadProjectDocumentWorkflowSnapshotResult(
  ProjectId ProjectId,
  IReadOnlyList<ProjectDocumentImportSessionSnapshot> ImportSessions,
  IReadOnlyList<ProjectImportedDocumentSourceSnapshot> ImportedSources,
  ProjectDocumentAuthorityIsolationSnapshot AuthorityIsolation,
  IReadOnlyList<ProjectDocumentAuditEntrySnapshot> AuditHistory);

public sealed record ProjectDocumentImportSessionSnapshot(
  DocumentImportSessionId ImportSessionId,
  string InitiatedBy,
  DateTimeOffset StartedAtUtc,
  DateTimeOffset? CompletedAtUtc,
  DocumentImportSessionState State,
  int SourceCount,
  int AcceptedCount,
  int DuplicateCount,
  int RejectedCount,
  string? FailureSummary,
  IReadOnlyList<ProjectDocumentAuditEntrySnapshot> AuditHistory);

public sealed record ProjectImportedDocumentSourceSnapshot(
  ImportedSourceId ImportedSourceId,
  DocumentImportSessionId ImportSessionId,
  string OriginalFileName,
  string? DeclaredMediaType,
  string? DetectedMediaType,
  long ContentLength,
  string ContentHash,
  string HashAlgorithm,
  int HashAlgorithmVersion,
  ImportedSourceOrigin SourceOrigin,
  ImportedDocumentSourceStatus Status,
  ProjectDocumentStorageSnapshot Storage,
  bool SameContentExistsInAnotherProject,
  IReadOnlyList<ProjectDocumentProcessingAttemptSnapshot> ProcessingAttempts,
  IReadOnlyList<ProjectDocumentCandidateSnapshot> Candidates,
  IReadOnlyList<ProjectDocumentAuditEntrySnapshot> AuditHistory);

public sealed record ProjectDocumentStorageSnapshot(
  StorageObjectId StorageObjectId,
  string StorageProviderKey,
  string ImmutableObjectKey,
  long ContentLength,
  string ContentHash,
  string HashAlgorithm,
  int HashAlgorithmVersion,
  DateTimeOffset CreatedAtUtc,
  StorageAvailabilityState AvailabilityState);

public sealed record ProjectDocumentProcessingAttemptSnapshot(
  DocumentProcessingAttemptId ProcessingAttemptId,
  int AttemptNumber,
  string ProcessorRole,
  string ProcessorIdentity,
  string ProcessorVersion,
  DocumentProcessingAttemptState State,
  DocumentProcessingFailureClassification FailureClassification,
  string? FailureDetails,
  string InputContentHash,
  string? OutputHash,
  DateTimeOffset RequestedAtUtc,
  DateTimeOffset? StartedAtUtc,
  DateTimeOffset? CompletedAtUtc,
  IReadOnlyList<ProjectDocumentAuditEntrySnapshot> AuditHistory);

public sealed record ProjectDocumentCandidateSnapshot(
  DocumentCandidateId DocumentCandidateId,
  DocumentProcessingAttemptId ProcessingAttemptId,
  DocumentCandidateType CandidateType,
  string SchemaId,
  string SchemaVersion,
  string PayloadHash,
  string CanonicalPayload,
  string SourceContentHash,
  string? SourceLocator,
  ConfidenceBand ConfidenceBand,
  IReadOnlyList<string> UncertaintyCodes,
  DocumentCandidateStatus Status,
  DateTimeOffset CreatedAtUtc,
  string? ReviewedBy,
  DateTimeOffset? ReviewedAtUtc,
  string? ReviewNotes,
  IReadOnlyList<ProjectDocumentAuditEntrySnapshot> AuditHistory);

public sealed record ProjectDocumentAuthorityIsolationSnapshot(
  int KnowledgeDocumentCount,
  int KnowledgeRelationshipCount,
  int ReportCount,
  int AiProposalCount);

public sealed record ProjectDocumentAuditEntrySnapshot(
  AuditEventId AuditEventId,
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
