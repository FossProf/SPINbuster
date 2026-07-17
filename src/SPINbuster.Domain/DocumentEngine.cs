using System.Security.Cryptography;
using System.Text;

namespace SPINbuster.Domain;

public enum ImportedSourceOrigin
{
  Unknown = 0,
  LocalFile = 1,
  MobileCapture = 2,
  ExternalStorage = 3,
  EmailAttachment = 4,
  ProjectRepository = 5,
  GeneratedExport = 6,
}

public enum ImportedDocumentSourceStatus
{
  Available = 0,
  Unavailable = 1,
}

public enum StorageAvailabilityState
{
  Available = 0,
  Unavailable = 1,
  Missing = 2,
}

public enum DocumentImportSessionState
{
  Created = 0,
  Validating = 1,
  Importing = 2,
  Completed = 3,
  Failed = 4,
  Cancelled = 5,
}

public enum DocumentProcessingFailureClassification
{
  None = 0,
  UnsupportedMediaType = 1,
  ProviderUnavailable = 2,
  Timeout = 3,
  Cancelled = 4,
  MalformedOutput = 5,
  SchemaRejected = 6,
  PolicyRejected = 7,
  ValidationFailed = 8,
  StorageUnavailable = 9,
  Unknown = 10,
}

public enum DocumentProcessingAttemptState
{
  Requested = 0,
  Running = 1,
  OutputReceived = 2,
  Validating = 3,
  Completed = 4,
  Failed = 5,
  Cancelled = 6,
  Abstained = 7,
}

public enum DocumentCandidateType
{
  MetadataCandidate = 0,
  FragmentCandidate = 1,
  CitationCandidate = 2,
  RelationshipCandidate = 3,
  AssertionCandidate = 4,
  RequirementCandidate = 5,
  ClassificationCandidate = 6,
}

public enum DocumentCandidateStatus
{
  Generated = 0,
  Validated = 1,
  ReadyForReview = 2,
  HumanAccepted = 3,
  Rejected = 4,
  SchemaRejected = 5,
  PolicyRejected = 6,
  Abstained = 7,
  Failed = 8,
}

public sealed record DocumentStorageReference
{
  public DocumentStorageReference(
    StorageObjectId storageObjectId,
    string storageProviderKey,
    string immutableObjectKey,
    long contentLength,
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    DateTimeOffset createdAtUtc,
    string? encryptionMetadataId,
    StorageAvailabilityState availabilityState)
  {
    if (contentLength < 0)
    {
      throw new DomainInvariantException($"{nameof(contentLength)} cannot be negative.");
    }

    if (hashAlgorithmVersion <= 0)
    {
      throw new DomainInvariantException($"{nameof(hashAlgorithmVersion)} must be greater than zero.");
    }

    StorageObjectId = storageObjectId;
    StorageProviderKey = DomainGuards.NotNullOrWhiteSpace(storageProviderKey, nameof(storageProviderKey));
    ImmutableObjectKey = DomainGuards.NotNullOrWhiteSpace(immutableObjectKey, nameof(immutableObjectKey));
    ContentLength = contentLength;
    ContentHash = DomainGuards.NotNullOrWhiteSpace(contentHash, nameof(contentHash));
    HashAlgorithm = DomainGuards.NotNullOrWhiteSpace(hashAlgorithm, nameof(hashAlgorithm));
    HashAlgorithmVersion = hashAlgorithmVersion;
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    EncryptionMetadataId = string.IsNullOrWhiteSpace(encryptionMetadataId) ? null : encryptionMetadataId.Trim();
    AvailabilityState = availabilityState;
  }

  public StorageObjectId StorageObjectId { get; }

  public string StorageProviderKey { get; }

  public string ImmutableObjectKey { get; }

  public long ContentLength { get; }

  public string ContentHash { get; }

  public string HashAlgorithm { get; }

  public int HashAlgorithmVersion { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public string? EncryptionMetadataId { get; }

  public StorageAvailabilityState AvailabilityState { get; }
}

public sealed class StorageObject
{
  public StorageObject(
    StorageObjectId id,
    string storageProviderKey,
    string immutableObjectKey,
    long contentLength,
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    DateTimeOffset createdAtUtc,
    string? encryptionMetadataId,
    StorageAvailabilityState availabilityState)
  {
    if (contentLength < 0)
    {
      throw new DomainInvariantException($"{nameof(contentLength)} cannot be negative.");
    }

    if (hashAlgorithmVersion <= 0)
    {
      throw new DomainInvariantException($"{nameof(hashAlgorithmVersion)} must be greater than zero.");
    }

    Id = id;
    StorageProviderKey = DomainGuards.NotNullOrWhiteSpace(storageProviderKey, nameof(storageProviderKey));
    ImmutableObjectKey = DomainGuards.NotNullOrWhiteSpace(immutableObjectKey, nameof(immutableObjectKey));
    ContentLength = contentLength;
    ContentHash = DomainGuards.NotNullOrWhiteSpace(contentHash, nameof(contentHash));
    HashAlgorithm = DomainGuards.NotNullOrWhiteSpace(hashAlgorithm, nameof(hashAlgorithm));
    HashAlgorithmVersion = hashAlgorithmVersion;
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    EncryptionMetadataId = string.IsNullOrWhiteSpace(encryptionMetadataId) ? null : encryptionMetadataId.Trim();
    AvailabilityState = availabilityState;
  }

  public StorageObjectId Id { get; }

  public string StorageProviderKey { get; }

  public string ImmutableObjectKey { get; }

  public long ContentLength { get; }

  public string ContentHash { get; }

  public string HashAlgorithm { get; }

  public int HashAlgorithmVersion { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public string? EncryptionMetadataId { get; }

  public StorageAvailabilityState AvailabilityState { get; private set; }

  public DocumentStorageReference ToReference()
    => new(
      Id,
      StorageProviderKey,
      ImmutableObjectKey,
      ContentLength,
      ContentHash,
      HashAlgorithm,
      HashAlgorithmVersion,
      CreatedAtUtc,
      EncryptionMetadataId,
      AvailabilityState);

  internal static StorageObject Rehydrate(
    StorageObjectId id,
    string storageProviderKey,
    string immutableObjectKey,
    long contentLength,
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    DateTimeOffset createdAtUtc,
    string? encryptionMetadataId,
    StorageAvailabilityState availabilityState)
  {
    return new StorageObject(
      id,
      storageProviderKey,
      immutableObjectKey,
      contentLength,
      contentHash,
      hashAlgorithm,
      hashAlgorithmVersion,
      createdAtUtc,
      encryptionMetadataId,
      availabilityState);
  }

  public void MarkUnavailable()
  {
    AvailabilityState = StorageAvailabilityState.Unavailable;
  }
}

public sealed class ImportedDocumentSource : AuditableEntity
{
  public ImportedDocumentSource(
    ImportedSourceId id,
    DocumentImportSessionId importSessionId,
    ProjectId projectId,
    string originalFileName,
    string? declaredMediaType,
    string? detectedMediaType,
    long contentLength,
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    DocumentStorageReference storageReference,
    ImportedSourceOrigin sourceOrigin,
    string importedBy,
    DateTimeOffset importedAtUtc,
    ImportedDocumentSourceStatus status,
    string? externalSourceReference)
  {
    if (contentLength < 0)
    {
      throw new DomainInvariantException($"{nameof(contentLength)} cannot be negative.");
    }

    if (hashAlgorithmVersion <= 0)
    {
      throw new DomainInvariantException($"{nameof(hashAlgorithmVersion)} must be greater than zero.");
    }

    if (storageReference.ContentLength != contentLength)
    {
      throw new DomainInvariantException("Storage content length must match imported source content length.");
    }

    if (!string.Equals(storageReference.ContentHash, contentHash, StringComparison.Ordinal))
    {
      throw new DomainInvariantException("Storage content hash must match imported source content hash.");
    }

    if (!string.Equals(storageReference.HashAlgorithm, hashAlgorithm, StringComparison.Ordinal))
    {
      throw new DomainInvariantException("Storage hash algorithm must match imported source hash algorithm.");
    }

    if (storageReference.HashAlgorithmVersion != hashAlgorithmVersion)
    {
      throw new DomainInvariantException("Storage hash algorithm version must match imported source hash algorithm version.");
    }

    Id = id;
    ImportSessionId = importSessionId;
    ProjectId = projectId;
    OriginalFileName = DomainGuards.NotNullOrWhiteSpace(originalFileName, nameof(originalFileName));
    DeclaredMediaType = NormalizeOptional(declaredMediaType);
    DetectedMediaType = NormalizeOptional(detectedMediaType);
    ContentLength = contentLength;
    ContentHash = DomainGuards.NotNullOrWhiteSpace(contentHash, nameof(contentHash));
    HashAlgorithm = DomainGuards.NotNullOrWhiteSpace(hashAlgorithm, nameof(hashAlgorithm));
    HashAlgorithmVersion = hashAlgorithmVersion;
    StorageReference = storageReference;
    SourceOrigin = sourceOrigin;
    ImportedBy = DomainGuards.NotNullOrWhiteSpace(importedBy, nameof(importedBy));
    ImportedAtUtc = DomainGuards.NotDefault(importedAtUtc, nameof(importedAtUtc));
    Status = status;
    ExternalSourceReference = NormalizeOptional(externalSourceReference);

    AppendAuditEvent(CreateAuditEvent(
      "ImportedDocumentSourceRegistered",
      importedBy,
      importedAtUtc,
      $"Imported document source '{OriginalFileName}' registered with content hash {ContentHash}."));
  }

  public ImportedSourceId Id { get; }

  public DocumentImportSessionId ImportSessionId { get; }

  public ProjectId ProjectId { get; }

  public string OriginalFileName { get; }

  public string? DeclaredMediaType { get; }

  public string? DetectedMediaType { get; }

  public long ContentLength { get; }

  public string ContentHash { get; }

  public string HashAlgorithm { get; }

  public int HashAlgorithmVersion { get; }

  public DocumentStorageReference StorageReference { get; }

  public ImportedSourceOrigin SourceOrigin { get; }

  public string ImportedBy { get; }

  public DateTimeOffset ImportedAtUtc { get; }

  public ImportedDocumentSourceStatus Status { get; private set; }

  public string? ExternalSourceReference { get; }

  internal static ImportedDocumentSource Rehydrate(
    ImportedSourceId id,
    DocumentImportSessionId importSessionId,
    ProjectId projectId,
    string originalFileName,
    string? declaredMediaType,
    string? detectedMediaType,
    long contentLength,
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    DocumentStorageReference storageReference,
    ImportedSourceOrigin sourceOrigin,
    string importedBy,
    DateTimeOffset importedAtUtc,
    ImportedDocumentSourceStatus status,
    string? externalSourceReference,
    IEnumerable<AuditEvent> auditTrail)
  {
    var source = new ImportedDocumentSource(
      id,
      importSessionId,
      projectId,
      originalFileName,
      declaredMediaType,
      detectedMediaType,
      contentLength,
      contentHash,
      hashAlgorithm,
      hashAlgorithmVersion,
      storageReference,
      sourceOrigin,
      importedBy,
      importedAtUtc,
      status,
      externalSourceReference)
    {
      Status = status,
    };

    source.RestoreAuditTrail(auditTrail);
    return source;
  }

  public void MarkUnavailable(string actor, DateTimeOffset occurredAtUtc, string reason)
  {
    if (Status == ImportedDocumentSourceStatus.Unavailable)
    {
      return;
    }

    Status = ImportedDocumentSourceStatus.Unavailable;
    AppendAuditEvent(CreateAuditEvent(
      "ImportedDocumentSourceUnavailable",
      actor,
      occurredAtUtc,
      reason));
  }

  private AuditEvent CreateAuditEvent(
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(ImportedDocumentSource),
      Id.ToString(),
      eventType,
      actor,
      occurredAtUtc,
      description);
  }

  private static string? NormalizeOptional(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}

public sealed class DocumentImportSession : AuditableEntity
{
  public DocumentImportSession(
    DocumentImportSessionId id,
    ProjectId projectId,
    string initiatedBy,
    DateTimeOffset startedAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    InitiatedBy = DomainGuards.NotNullOrWhiteSpace(initiatedBy, nameof(initiatedBy));
    StartedAtUtc = DomainGuards.NotDefault(startedAtUtc, nameof(startedAtUtc));
    State = DocumentImportSessionState.Created;

    AppendAuditEvent(CreateAuditEvent(
      "DocumentImportSessionStarted",
      initiatedBy,
      startedAtUtc,
      "Document import session started."));
  }

  public DocumentImportSessionId Id { get; }

  public ProjectId ProjectId { get; }

  public string InitiatedBy { get; }

  public DateTimeOffset StartedAtUtc { get; }

  public DateTimeOffset? CompletedAtUtc { get; private set; }

  public DocumentImportSessionState State { get; private set; }

  public int SourceCount { get; private set; }

  public int AcceptedCount { get; private set; }

  public int DuplicateCount { get; private set; }

  public int RejectedCount { get; private set; }

  public string? FailureSummary { get; private set; }

  internal static DocumentImportSession Rehydrate(
    DocumentImportSessionId id,
    ProjectId projectId,
    string initiatedBy,
    DateTimeOffset startedAtUtc,
    DateTimeOffset? completedAtUtc,
    DocumentImportSessionState state,
    int sourceCount,
    int acceptedCount,
    int duplicateCount,
    int rejectedCount,
    string? failureSummary,
    IEnumerable<AuditEvent> auditTrail)
  {
    var session = new DocumentImportSession(id, projectId, initiatedBy, startedAtUtc)
    {
      CompletedAtUtc = completedAtUtc,
      State = state,
      SourceCount = sourceCount,
      AcceptedCount = acceptedCount,
      DuplicateCount = duplicateCount,
      RejectedCount = rejectedCount,
      FailureSummary = NormalizeOptional(failureSummary),
    };

    session.ValidateCounts();
    session.RestoreAuditTrail(auditTrail);
    return session;
  }

  public void BeginValidation(string actor, DateTimeOffset occurredAtUtc)
  {
    // A batch session may validate multiple sources before explicit completion.
    TransitionTo([DocumentImportSessionState.Created, DocumentImportSessionState.Validating, DocumentImportSessionState.Importing], DocumentImportSessionState.Validating, nameof(BeginValidation));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentImportValidationStarted",
      actor,
      occurredAtUtc,
      "Document import validation started."));
  }

  public void BeginImporting(string actor, DateTimeOffset occurredAtUtc)
  {
    TransitionTo([DocumentImportSessionState.Created, DocumentImportSessionState.Validating, DocumentImportSessionState.Importing], DocumentImportSessionState.Importing, nameof(BeginImporting));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentImportExecutionStarted",
      actor,
      occurredAtUtc,
      "Document import execution started."));
  }

  public void RecordAcceptedSource(ImportedSourceId importedSourceId, string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureActive(nameof(RecordAcceptedSource));
    SourceCount++;
    AcceptedCount++;
    ValidateCounts();
    AppendAuditEvent(CreateAuditEvent(
      "ImportedDocumentSourceAccepted",
      actor,
      occurredAtUtc,
      $"Imported source {importedSourceId} accepted into session {Id}."));
  }

  public void RecordDuplicateSource(ImportedSourceId importedSourceId, string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureActive(nameof(RecordDuplicateSource));
    SourceCount++;
    DuplicateCount++;
    ValidateCounts();
    AppendAuditEvent(CreateAuditEvent(
      "ImportedDocumentSourceDuplicateDetected",
      actor,
      occurredAtUtc,
      $"Duplicate imported source detected and linked to existing source {importedSourceId}."));
  }

  public void RecordRejectedSource(string actor, DateTimeOffset occurredAtUtc, string reason)
  {
    EnsureActive(nameof(RecordRejectedSource));
    SourceCount++;
    RejectedCount++;
    ValidateCounts();
    AppendAuditEvent(CreateAuditEvent(
      "ImportedDocumentSourceRejected",
      actor,
      occurredAtUtc,
      reason));
  }

  public void Complete(string actor, DateTimeOffset completedAtUtc)
  {
    EnsureActive(nameof(Complete));
    State = DocumentImportSessionState.Completed;
    CompletedAtUtc = DomainGuards.NotDefault(completedAtUtc, nameof(completedAtUtc));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentImportSessionCompleted",
      actor,
      completedAtUtc,
      $"Document import session completed with {AcceptedCount} accepted, {DuplicateCount} duplicates, and {RejectedCount} rejected sources."));
  }

  public void Fail(string actor, DateTimeOffset occurredAtUtc, string failureSummary)
  {
    if (State is DocumentImportSessionState.Completed or DocumentImportSessionState.Cancelled or DocumentImportSessionState.Failed)
    {
      throw new LifecycleTransitionException(nameof(DocumentImportSession), State.ToString(), nameof(Fail));
    }

    State = DocumentImportSessionState.Failed;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureSummary = DomainGuards.NotNullOrWhiteSpace(failureSummary, nameof(failureSummary));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentImportSessionFailed",
      actor,
      occurredAtUtc,
      FailureSummary));
  }

  public void Cancel(string actor, DateTimeOffset occurredAtUtc, string reason)
  {
    if (State is DocumentImportSessionState.Completed or DocumentImportSessionState.Cancelled or DocumentImportSessionState.Failed)
    {
      throw new LifecycleTransitionException(nameof(DocumentImportSession), State.ToString(), nameof(Cancel));
    }

    State = DocumentImportSessionState.Cancelled;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureSummary = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentImportSessionCancelled",
      actor,
      occurredAtUtc,
      FailureSummary));
  }

  private void EnsureActive(string transitionName)
  {
    if (State is DocumentImportSessionState.Completed or DocumentImportSessionState.Failed or DocumentImportSessionState.Cancelled)
    {
      throw new LifecycleTransitionException(nameof(DocumentImportSession), State.ToString(), transitionName);
    }
  }

  private void TransitionTo(
    IReadOnlyCollection<DocumentImportSessionState> allowedStates,
    DocumentImportSessionState nextState,
    string transitionName)
  {
    if (!allowedStates.Contains(State))
    {
      throw new LifecycleTransitionException(nameof(DocumentImportSession), State.ToString(), transitionName);
    }

    State = nextState;
  }

  private void ValidateCounts()
  {
    if (SourceCount < 0 || AcceptedCount < 0 || DuplicateCount < 0 || RejectedCount < 0)
    {
      throw new DomainInvariantException("Import session counts cannot be negative.");
    }

    if (SourceCount != AcceptedCount + DuplicateCount + RejectedCount)
    {
      throw new DomainInvariantException("Import session counts must remain internally consistent.");
    }
  }

  private AuditEvent CreateAuditEvent(
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(DocumentImportSession),
      Id.ToString(),
      eventType,
      actor,
      occurredAtUtc,
      description);
  }

  private static string? NormalizeOptional(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}

public sealed class DocumentProcessingAttempt : AuditableEntity
{
  public DocumentProcessingAttempt(
    DocumentProcessingAttemptId id,
    ImportedSourceId importedSourceId,
    ProjectId projectId,
    string processorRole,
    string processorIdentity,
    string processorVersion,
    DateTimeOffset requestedAtUtc,
    int attemptNumber,
    string inputContentHash)
  {
    if (attemptNumber <= 0)
    {
      throw new DomainInvariantException($"{nameof(attemptNumber)} must be greater than zero.");
    }

    Id = id;
    ImportedSourceId = importedSourceId;
    ProjectId = projectId;
    ProcessorRole = DomainGuards.NotNullOrWhiteSpace(processorRole, nameof(processorRole));
    ProcessorIdentity = DomainGuards.NotNullOrWhiteSpace(processorIdentity, nameof(processorIdentity));
    ProcessorVersion = DomainGuards.NotNullOrWhiteSpace(processorVersion, nameof(processorVersion));
    RequestedAtUtc = DomainGuards.NotDefault(requestedAtUtc, nameof(requestedAtUtc));
    AttemptNumber = attemptNumber;
    InputContentHash = DomainGuards.NotNullOrWhiteSpace(inputContentHash, nameof(inputContentHash));
    State = DocumentProcessingAttemptState.Requested;

    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingRequested",
      ProcessorIdentity,
      requestedAtUtc,
      $"Document processing requested for source {ImportedSourceId} using processor {ProcessorIdentity} {ProcessorVersion}."));
  }

  public DocumentProcessingAttemptId Id { get; }

  public ImportedSourceId ImportedSourceId { get; }

  public ProjectId ProjectId { get; }

  public string ProcessorRole { get; }

  public string ProcessorIdentity { get; }

  public string ProcessorVersion { get; }

  public DateTimeOffset RequestedAtUtc { get; }

  public DateTimeOffset? StartedAtUtc { get; private set; }

  public DateTimeOffset? CompletedAtUtc { get; private set; }

  public int AttemptNumber { get; }

  public DocumentProcessingAttemptState State { get; private set; }

  public DocumentProcessingFailureClassification FailureClassification { get; private set; }

  public string? FailureDetails { get; private set; }

  public string InputContentHash { get; }

  public string? OutputHash { get; private set; }

  internal static DocumentProcessingAttempt Rehydrate(
    DocumentProcessingAttemptId id,
    ImportedSourceId importedSourceId,
    ProjectId projectId,
    string processorRole,
    string processorIdentity,
    string processorVersion,
    DateTimeOffset requestedAtUtc,
    DateTimeOffset? startedAtUtc,
    DateTimeOffset? completedAtUtc,
    int attemptNumber,
    DocumentProcessingAttemptState state,
    DocumentProcessingFailureClassification failureClassification,
    string? failureDetails,
    string inputContentHash,
    string? outputHash,
    IEnumerable<AuditEvent> auditTrail)
  {
    var attempt = new DocumentProcessingAttempt(
      id,
      importedSourceId,
      projectId,
      processorRole,
      processorIdentity,
      processorVersion,
      requestedAtUtc,
      attemptNumber,
      inputContentHash)
    {
      StartedAtUtc = startedAtUtc,
      CompletedAtUtc = completedAtUtc,
      State = state,
      FailureClassification = failureClassification,
      FailureDetails = NormalizeOptional(failureDetails),
      OutputHash = NormalizeOptional(outputHash),
    };

    attempt.RestoreAuditTrail(auditTrail);
    return attempt;
  }

  public void Start(DateTimeOffset startedAtUtc)
  {
    if (State != DocumentProcessingAttemptState.Requested)
    {
      throw new LifecycleTransitionException(nameof(DocumentProcessingAttempt), State.ToString(), nameof(Start));
    }

    StartedAtUtc = DomainGuards.NotDefault(startedAtUtc, nameof(startedAtUtc));
    State = DocumentProcessingAttemptState.Running;
    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingStarted",
      ProcessorIdentity,
      startedAtUtc,
      $"Document processing attempt {Id} started."));
  }

  public void MarkOutputReceived(DateTimeOffset occurredAtUtc, string outputHash)
  {
    if (State != DocumentProcessingAttemptState.Running)
    {
      throw new LifecycleTransitionException(nameof(DocumentProcessingAttempt), State.ToString(), nameof(MarkOutputReceived));
    }

    OutputHash = DomainGuards.NotNullOrWhiteSpace(outputHash, nameof(outputHash));
    State = DocumentProcessingAttemptState.OutputReceived;
    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingOutputReceived",
      ProcessorIdentity,
      occurredAtUtc,
      $"Document processing output received with hash {OutputHash}."));
  }

  public void BeginValidation(DateTimeOffset occurredAtUtc)
  {
    if (State != DocumentProcessingAttemptState.OutputReceived)
    {
      throw new LifecycleTransitionException(nameof(DocumentProcessingAttempt), State.ToString(), nameof(BeginValidation));
    }

    State = DocumentProcessingAttemptState.Validating;
    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingValidationStarted",
      ProcessorIdentity,
      occurredAtUtc,
      "Document processing output validation started."));
  }

  public void Complete(DateTimeOffset occurredAtUtc)
  {
    if (State != DocumentProcessingAttemptState.Validating)
    {
      throw new LifecycleTransitionException(nameof(DocumentProcessingAttempt), State.ToString(), nameof(Complete));
    }

    State = DocumentProcessingAttemptState.Completed;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureClassification = DocumentProcessingFailureClassification.None;
    FailureDetails = null;
    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingCompleted",
      ProcessorIdentity,
      occurredAtUtc,
      "Document processing completed successfully."));
  }

  public void Fail(
    DateTimeOffset occurredAtUtc,
    DocumentProcessingFailureClassification failureClassification,
    string failureDetails)
  {
    if (State is DocumentProcessingAttemptState.Completed
      or DocumentProcessingAttemptState.Failed
      or DocumentProcessingAttemptState.Cancelled
      or DocumentProcessingAttemptState.Abstained)
    {
      throw new LifecycleTransitionException(nameof(DocumentProcessingAttempt), State.ToString(), nameof(Fail));
    }

    if (failureClassification == DocumentProcessingFailureClassification.None)
    {
      throw new DomainInvariantException("Failure classification must describe a non-success outcome.");
    }

    State = DocumentProcessingAttemptState.Failed;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureClassification = failureClassification;
    FailureDetails = DomainGuards.NotNullOrWhiteSpace(failureDetails, nameof(failureDetails));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingFailed",
      ProcessorIdentity,
      occurredAtUtc,
      FailureDetails));
  }

  public void Cancel(DateTimeOffset occurredAtUtc, string reason)
  {
    if (State is DocumentProcessingAttemptState.Completed
      or DocumentProcessingAttemptState.Failed
      or DocumentProcessingAttemptState.Cancelled
      or DocumentProcessingAttemptState.Abstained)
    {
      throw new LifecycleTransitionException(nameof(DocumentProcessingAttempt), State.ToString(), nameof(Cancel));
    }

    State = DocumentProcessingAttemptState.Cancelled;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureClassification = DocumentProcessingFailureClassification.Cancelled;
    FailureDetails = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingCancelled",
      ProcessorIdentity,
      occurredAtUtc,
      FailureDetails));
  }

  public void Abstain(DateTimeOffset occurredAtUtc, string reason)
  {
    if (State is DocumentProcessingAttemptState.Completed
      or DocumentProcessingAttemptState.Failed
      or DocumentProcessingAttemptState.Cancelled
      or DocumentProcessingAttemptState.Abstained)
    {
      throw new LifecycleTransitionException(nameof(DocumentProcessingAttempt), State.ToString(), nameof(Abstain));
    }

    State = DocumentProcessingAttemptState.Abstained;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    FailureClassification = DocumentProcessingFailureClassification.None;
    FailureDetails = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentProcessingAbstained",
      ProcessorIdentity,
      occurredAtUtc,
      FailureDetails));
  }

  private AuditEvent CreateAuditEvent(
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(DocumentProcessingAttempt),
      Id.ToString(),
      eventType,
      actor,
      occurredAtUtc,
      description);
  }

  private static string? NormalizeOptional(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}

public sealed class DocumentCandidate : AuditableEntity
{
  public DocumentCandidate(
    DocumentCandidateId id,
    ProjectId projectId,
    ImportedSourceId importedSourceId,
    DocumentProcessingAttemptId processingAttemptId,
    DocumentCandidateType candidateType,
    string schemaId,
    string schemaVersion,
    string canonicalPayload,
    string sourceContentHash,
    string? sourceLocator,
    ConfidenceBand confidenceBand,
    IEnumerable<string> uncertaintyCodes,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    ImportedSourceId = importedSourceId;
    ProcessingAttemptId = processingAttemptId;
    CandidateType = candidateType;
    SchemaId = DomainGuards.NotNullOrWhiteSpace(schemaId, nameof(schemaId));
    SchemaVersion = DomainGuards.NotNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));
    CanonicalPayload = DomainGuards.NotNullOrWhiteSpace(canonicalPayload, nameof(canonicalPayload));
    PayloadHash = ComputeHash(CanonicalPayload);
    SourceContentHash = DomainGuards.NotNullOrWhiteSpace(sourceContentHash, nameof(sourceContentHash));
    SourceLocator = NormalizeOptional(sourceLocator);
    ConfidenceBand = confidenceBand;
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    Status = DocumentCandidateStatus.Generated;

    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateGenerated",
      "system",
      createdAtUtc,
      $"Document candidate {CandidateType} generated with schema {SchemaId}@{SchemaVersion}."));
  }

  public DocumentCandidateId Id { get; }

  public ProjectId ProjectId { get; }

  public ImportedSourceId ImportedSourceId { get; }

  public DocumentProcessingAttemptId ProcessingAttemptId { get; }

  public DocumentCandidateType CandidateType { get; }

  public string SchemaId { get; }

  public string SchemaVersion { get; }

  public string PayloadHash { get; }

  public string CanonicalPayload { get; }

  public string SourceContentHash { get; }

  public string? SourceLocator { get; }

  public ConfidenceBand ConfidenceBand { get; private set; }

  public IReadOnlyList<string> UncertaintyCodes { get; private set; }

  public DocumentCandidateStatus Status { get; private set; }

  public DateTimeOffset CreatedAtUtc { get; }

  public string? ReviewedBy { get; private set; }

  public DateTimeOffset? ReviewedAtUtc { get; private set; }

  public string? ReviewNotes { get; private set; }

  internal static DocumentCandidate Rehydrate(
    DocumentCandidateId id,
    ProjectId projectId,
    ImportedSourceId importedSourceId,
    DocumentProcessingAttemptId processingAttemptId,
    DocumentCandidateType candidateType,
    string schemaId,
    string schemaVersion,
    string payloadHash,
    string canonicalPayload,
    string sourceContentHash,
    string? sourceLocator,
    ConfidenceBand confidenceBand,
    IReadOnlyList<string> uncertaintyCodes,
    DocumentCandidateStatus status,
    DateTimeOffset createdAtUtc,
    string? reviewedBy,
    DateTimeOffset? reviewedAtUtc,
    string? reviewNotes,
    IEnumerable<AuditEvent> auditTrail)
  {
    var candidate = new DocumentCandidate(
      id,
      projectId,
      importedSourceId,
      processingAttemptId,
      candidateType,
      schemaId,
      schemaVersion,
      canonicalPayload,
      sourceContentHash,
      sourceLocator,
      confidenceBand,
      uncertaintyCodes,
      createdAtUtc)
    {
      Status = status,
      ReviewedBy = reviewedBy,
      ReviewedAtUtc = reviewedAtUtc,
      ReviewNotes = NormalizeOptional(reviewNotes),
    };

    if (!string.Equals(candidate.PayloadHash, payloadHash, StringComparison.Ordinal))
    {
      throw new DomainInvariantException("Persisted candidate payload hash must match the canonical payload.");
    }

    candidate.RestoreAuditTrail(auditTrail);
    return candidate;
  }

  public void MarkValidated(DateTimeOffset occurredAtUtc)
  {
    TransitionFrom([DocumentCandidateStatus.Generated], DocumentCandidateStatus.Validated, nameof(MarkValidated));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateValidated",
      "system",
      occurredAtUtc,
      "Document candidate passed deterministic validation."));
  }

  public void MarkReadyForReview(DateTimeOffset occurredAtUtc)
  {
    TransitionFrom([DocumentCandidateStatus.Validated], DocumentCandidateStatus.ReadyForReview, nameof(MarkReadyForReview));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateReadyForReview",
      "system",
      occurredAtUtc,
      "Document candidate is ready for human review."));
  }

  public void MarkSchemaRejected(DateTimeOffset occurredAtUtc, IEnumerable<string> uncertaintyCodes)
  {
    TransitionFrom([DocumentCandidateStatus.Generated, DocumentCandidateStatus.Validated], DocumentCandidateStatus.SchemaRejected, nameof(MarkSchemaRejected));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateSchemaRejected",
      "system",
      occurredAtUtc,
      "Document candidate failed schema validation."));
  }

  public void MarkPolicyRejected(DateTimeOffset occurredAtUtc, IEnumerable<string> uncertaintyCodes)
  {
    TransitionFrom([DocumentCandidateStatus.Generated, DocumentCandidateStatus.Validated], DocumentCandidateStatus.PolicyRejected, nameof(MarkPolicyRejected));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidatePolicyRejected",
      "system",
      occurredAtUtc,
      "Document candidate failed policy validation."));
  }

  public void MarkAbstained(DateTimeOffset occurredAtUtc, IEnumerable<string> uncertaintyCodes)
  {
    TransitionFrom([DocumentCandidateStatus.Generated, DocumentCandidateStatus.Validated], DocumentCandidateStatus.Abstained, nameof(MarkAbstained));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateAbstained",
      "system",
      occurredAtUtc,
      "Document candidate creation abstained."));
  }

  public void MarkFailed(DateTimeOffset occurredAtUtc, IEnumerable<string> uncertaintyCodes)
  {
    TransitionFrom([DocumentCandidateStatus.Generated, DocumentCandidateStatus.Validated], DocumentCandidateStatus.Failed, nameof(MarkFailed));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateFailed",
      "system",
      occurredAtUtc,
      "Document candidate generation failed."));
  }

  public void Accept(string reviewedBy, DateTimeOffset reviewedAtUtc, string? reviewNotes)
  {
    TransitionFrom([DocumentCandidateStatus.ReadyForReview], DocumentCandidateStatus.HumanAccepted, nameof(Accept));
    ReviewedBy = DomainGuards.NotNullOrWhiteSpace(reviewedBy, nameof(reviewedBy));
    ReviewedAtUtc = DomainGuards.NotDefault(reviewedAtUtc, nameof(reviewedAtUtc));
    ReviewNotes = NormalizeOptional(reviewNotes);
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateHumanAccepted",
      ReviewedBy,
      reviewedAtUtc,
      "Document candidate recorded as human-accepted review intent only."));
  }

  public void Reject(string reviewedBy, DateTimeOffset reviewedAtUtc, string? reviewNotes)
  {
    TransitionFrom([DocumentCandidateStatus.ReadyForReview], DocumentCandidateStatus.Rejected, nameof(Reject));
    ReviewedBy = DomainGuards.NotNullOrWhiteSpace(reviewedBy, nameof(reviewedBy));
    ReviewedAtUtc = DomainGuards.NotDefault(reviewedAtUtc, nameof(reviewedAtUtc));
    ReviewNotes = NormalizeOptional(reviewNotes);
    AppendAuditEvent(CreateAuditEvent(
      "DocumentCandidateRejected",
      ReviewedBy,
      reviewedAtUtc,
      "Document candidate rejected during review."));
  }

  private void TransitionFrom(
    IReadOnlyCollection<DocumentCandidateStatus> allowedStatuses,
    DocumentCandidateStatus nextStatus,
    string transitionName)
  {
    if (!allowedStatuses.Contains(Status))
    {
      throw new LifecycleTransitionException(nameof(DocumentCandidate), Status.ToString(), transitionName);
    }

    Status = nextStatus;
  }

  private AuditEvent CreateAuditEvent(
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(DocumentCandidate),
      Id.ToString(),
      eventType,
      actor,
      occurredAtUtc,
      description);
  }

  private static string ComputeHash(string value)
  {
    var bytes = Encoding.UTF8.GetBytes(value);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }

  private static string[] NormalizeStrings(IEnumerable<string> values, string paramName)
  {
    return (values ?? [])
      .Select(value => DomainGuards.NotNullOrWhiteSpace(value, paramName))
      .Distinct(StringComparer.Ordinal)
      .OrderBy(value => value, StringComparer.Ordinal)
      .ToArray();
  }

  private static string? NormalizeOptional(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}
