using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class StorageObjectRecord
{
  public StorageObjectId Id { get; set; }

  public string StorageProviderKey { get; set; } = string.Empty;

  public string ImmutableObjectKey { get; set; } = string.Empty;

  public long ContentLength { get; set; }

  public string ContentHash { get; set; } = string.Empty;

  public string HashAlgorithm { get; set; } = string.Empty;

  public int HashAlgorithmVersion { get; set; }

  public DateTimeOffset CreatedAtUtc { get; set; }

  public string? EncryptionMetadataId { get; set; }

  public StorageAvailabilityState AvailabilityState { get; set; }
}

internal sealed class ImportedDocumentSourceRecord
{
  public ImportedSourceId Id { get; set; }

  public DocumentImportSessionId ImportSessionId { get; set; }

  public ProjectId ProjectId { get; set; }

  public string OriginalFileName { get; set; } = string.Empty;

  public string? DeclaredMediaType { get; set; }

  public string? DetectedMediaType { get; set; }

  public long ContentLength { get; set; }

  public string ContentHash { get; set; } = string.Empty;

  public string HashAlgorithm { get; set; } = string.Empty;

  public int HashAlgorithmVersion { get; set; }

  public StorageObjectId StorageObjectId { get; set; }

  public ImportedSourceOrigin SourceOrigin { get; set; }

  public string ImportedBy { get; set; } = string.Empty;

  public DateTimeOffset ImportedAtUtc { get; set; }

  public ImportedDocumentSourceStatus Status { get; set; }

  public string? ExternalSourceReference { get; set; }
}

internal sealed class DocumentImportSessionRecord
{
  public DocumentImportSessionId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public string InitiatedBy { get; set; } = string.Empty;

  public DateTimeOffset StartedAtUtc { get; set; }

  public DateTimeOffset? CompletedAtUtc { get; set; }

  public DocumentImportSessionState State { get; set; }

  public int SourceCount { get; set; }

  public int AcceptedCount { get; set; }

  public int DuplicateCount { get; set; }

  public int RejectedCount { get; set; }

  public string? FailureSummary { get; set; }
}

internal sealed class DocumentProcessingAttemptRecord
{
  public DocumentProcessingAttemptId Id { get; set; }

  public ImportedSourceId ImportedSourceId { get; set; }

  public ProjectId ProjectId { get; set; }

  public string ProcessorRole { get; set; } = string.Empty;

  public string ProcessorIdentity { get; set; } = string.Empty;

  public string ProcessorVersion { get; set; } = string.Empty;

  public DateTimeOffset RequestedAtUtc { get; set; }

  public DateTimeOffset? StartedAtUtc { get; set; }

  public DateTimeOffset? CompletedAtUtc { get; set; }

  public int AttemptNumber { get; set; }

  public DocumentProcessingAttemptState State { get; set; }

  public DocumentProcessingFailureClassification FailureClassification { get; set; }

  public string? FailureDetails { get; set; }

  public string InputContentHash { get; set; } = string.Empty;

  public string? OutputHash { get; set; }
}

internal sealed class DocumentCandidateRecord
{
  public DocumentCandidateId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public ImportedSourceId ImportedSourceId { get; set; }

  public DocumentProcessingAttemptId ProcessingAttemptId { get; set; }

  public DocumentCandidateType CandidateType { get; set; }

  public string SchemaId { get; set; } = string.Empty;

  public string SchemaVersion { get; set; } = string.Empty;

  public string PayloadHash { get; set; } = string.Empty;

  public string CanonicalPayload { get; set; } = string.Empty;

  public string SourceContentHash { get; set; } = string.Empty;

  public string? SourceLocator { get; set; }

  public ConfidenceBand ConfidenceBand { get; set; }

  public string UncertaintyCodesJson { get; set; } = "[]";

  public DocumentCandidateStatus Status { get; set; }

  public DateTimeOffset CreatedAtUtc { get; set; }

  public string? ReviewedBy { get; set; }

  public DateTimeOffset? ReviewedAtUtc { get; set; }

  public string? ReviewNotes { get; set; }
}

internal sealed class ParserRunRecord
{
  public ParserRunId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public ImportedSourceId ImportedSourceId { get; set; }

  public string ParserKey { get; set; } = string.Empty;

  public string ParserVersion { get; set; } = string.Empty;

  public string ParserContractVersion { get; set; } = string.Empty;

  public string ParserContractHash { get; set; } = string.Empty;

  public string SourceContentHash { get; set; } = string.Empty;

  public string SourceHashAlgorithm { get; set; } = string.Empty;

  public int SourceHashAlgorithmVersion { get; set; }

  public string CreatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public ParserRunState State { get; set; }

  public ParserExecutionStatus ExecutionStatus { get; set; }

  public DateTimeOffset? StartedAtUtc { get; set; }

  public DateTimeOffset? CompletedAtUtc { get; set; }

  public string? FailureReason { get; set; }
}

internal sealed class FragmentCandidateRecord
{
  public FragmentCandidateId Id { get; set; }

  public ParserRunId ParserRunId { get; set; }

  public ProjectId ProjectId { get; set; }

  public ImportedSourceId ImportedSourceId { get; set; }

  public string SourceContentHash { get; set; } = string.Empty;

  public FragmentLocatorType LocatorType { get; set; }

  public string LocatorRawValue { get; set; } = string.Empty;

  public string LocatorNormalizedValue { get; set; } = string.Empty;

  public int Ordinal { get; set; }

  public ContentKind ContentKind { get; set; }

  public string ExtractedText { get; set; } = string.Empty;

  public int TextLength { get; set; }

  public ConfidenceBand ConfidenceBand { get; set; }

  public string IdentityKey { get; set; } = string.Empty;

  public string IdentityKeyHash { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public FragmentCandidateReviewState ReviewState { get; set; }

  public string? ReviewedBy { get; set; }

  public DateTimeOffset? ReviewedAtUtc { get; set; }

  public string? ReviewNotes { get; set; }
}

internal sealed class ParserDiagnosticRecord
{
  public ParserDiagnosticId Id { get; set; }

  public ParserRunId ParserRunId { get; set; }

  public DiagnosticSeverity Severity { get; set; }

  public string Code { get; set; } = string.Empty;

  public string Message { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public DiagnosticRefType? CandidateRefType { get; set; }

  public string? CandidateRefValue { get; set; }

  public FragmentLocatorType? LocatorType { get; set; }

  public string? LocatorValue { get; set; }
}
