using System.Text.Json;
using SPINbuster.Domain;

namespace SPINbuster.Application.Abstractions
{

  public interface IContentHashService
  {
    Task<ContentHashResult> ComputeAsync(Stream content, CancellationToken cancellationToken = default);
  }

  public sealed record ContentHashResult(
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    long ContentLength);

  public interface IImmutableContentStore
  {
    Task<StoredContentResult?> FindByHashAsync(
      string contentHash,
      string hashAlgorithm,
      int hashAlgorithmVersion,
      CancellationToken cancellationToken = default);

    Task<StoredContentResult> StoreAsync(
      StoreImmutableContentRequest request,
      CancellationToken cancellationToken = default);

    Task<OpenImmutableContentResult> OpenReadAsync(
      StorageObjectId storageObjectId,
      CancellationToken cancellationToken = default);
  }

  public sealed record StoreImmutableContentRequest(
    StorageObjectId StorageObjectId,
    string StorageProviderKey,
    string ImmutableObjectKey,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    long ContentLength,
    Stream Content,
    DateTimeOffset CreatedAtUtc,
    string? EncryptionMetadataId);

  public sealed record StoredContentResult(
    StorageObjectId StorageObjectId,
    string StorageProviderKey,
    string ImmutableObjectKey,
    long ContentLength,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    DateTimeOffset CreatedAtUtc,
    string? EncryptionMetadataId,
    StorageAvailabilityState AvailabilityState);

  public sealed record OpenImmutableContentResult(
    Stream Content,
    StorageAvailabilityState AvailabilityState,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    long ContentLength);

  public interface IImportedContentInspector
  {
    Task<ImportedContentInspectionResult> InspectAsync(
      string originalFileName,
      string? declaredMediaType,
      long contentLength,
      CancellationToken cancellationToken = default);
  }

  public sealed record ImportedContentInspectionResult(
    string NormalizedFileName,
    string? NormalizedDeclaredMediaType,
    string? DetectedMediaType,
    bool IsSupported,
    IReadOnlyList<string> Warnings);

  public interface IDocumentImportPolicy
  {
    long MaxContentLengthBytes { get; }

    int MaxCandidateQueryResults { get; }

    int MaxProcessingHistoryResults { get; }
  }

  public interface IDocumentProcessor
  {
    DocumentProcessorDescriptor Describe();

    Task<DocumentProcessorExecutionResult> ProcessAsync(
      DocumentProcessorRequest request,
      CancellationToken cancellationToken = default);
  }

  public sealed record DocumentProcessorDescriptor(
    string ProcessorRole,
    string ProcessorIdentity,
    string ProcessorVersion,
    IReadOnlyList<DocumentProcessingCapability> SupportedCapabilities,
    IReadOnlyList<string> SupportedMediaTypes);

  public enum DocumentProcessingCapability
  {
    MetadataInspection = 0,
    StructureDetection = 1,
    TextExtraction = 2,
    FragmentDetection = 3,
    CitationDetection = 4,
    RelationshipSuggestion = 5,
    AssertionSuggestion = 6,
  }

  public sealed record DocumentProcessorRequest(
    ImportedSourceId ImportedSourceId,
    ProjectId ProjectId,
    string OriginalFileName,
    string? DeclaredMediaType,
    string? DetectedMediaType,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    long ContentLength,
    Stream Content);

  public sealed record DocumentProcessorExecutionResult(
    bool Success,
    string? RawOutputHash,
    DocumentProcessingFailureClassification FailureClassification,
    string? FailureDetails,
    IReadOnlyList<DocumentProcessorCandidateResult> Candidates);

  public sealed record DocumentProcessorCandidateResult(
    DocumentCandidateType CandidateType,
    string SchemaId,
    string SchemaVersion,
    JsonElement Payload,
    string? SourceLocator,
    ConfidenceBand ConfidenceBand,
    IReadOnlyList<string> UncertaintyCodes,
    DocumentProcessorCandidateOutcome Outcome);

  public enum DocumentProcessorCandidateOutcome
  {
    ReadyForReview = 0,
    SchemaRejected = 1,
    PolicyRejected = 2,
    Abstained = 3,
    Failed = 4,
  }

}

namespace SPINbuster.Application.Repositories
{

  public interface IImportedDocumentSourceRepository
  {
    Task<ImportedDocumentSource?> GetByIdAsync(ImportedSourceId importedSourceId, CancellationToken cancellationToken = default);

    Task<ImportedDocumentSource?> GetByProjectAndContentHashAsync(
      ProjectId projectId,
      string contentHash,
      string hashAlgorithm,
      int hashAlgorithmVersion,
      CancellationToken cancellationToken = default);

    Task<bool> ExistsInOtherProjectsAsync(
      ProjectId projectId,
      string contentHash,
      string hashAlgorithm,
      int hashAlgorithmVersion,
      CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ImportedDocumentSource>> GetByImportSessionAsync(
      DocumentImportSessionId importSessionId,
      int maxResults,
      CancellationToken cancellationToken = default);

    Task AddAsync(ImportedDocumentSource importedDocumentSource, CancellationToken cancellationToken = default);
  }

  public interface IDocumentImportSessionRepository
  {
    Task<DocumentImportSession?> GetByIdAsync(DocumentImportSessionId importSessionId, CancellationToken cancellationToken = default);

    Task AddAsync(DocumentImportSession importSession, CancellationToken cancellationToken = default);

    Task UpdateAsync(DocumentImportSession importSession, CancellationToken cancellationToken = default);
  }

  public interface IDocumentProcessingAttemptRepository
  {
    Task<DocumentProcessingAttempt?> GetByIdAsync(DocumentProcessingAttemptId processingAttemptId, CancellationToken cancellationToken = default);

    Task<int> GetNextAttemptNumberAsync(ImportedSourceId importedSourceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DocumentProcessingAttempt>> GetByImportedSourceAsync(
      ImportedSourceId importedSourceId,
      int maxResults,
      CancellationToken cancellationToken = default);

    Task AddAsync(DocumentProcessingAttempt processingAttempt, CancellationToken cancellationToken = default);

    Task UpdateAsync(DocumentProcessingAttempt processingAttempt, CancellationToken cancellationToken = default);
  }

  public interface IDocumentCandidateRepository
  {
    Task<DocumentCandidate?> GetByIdAsync(DocumentCandidateId documentCandidateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DocumentCandidate>> GetByImportedSourceAsync(
      ImportedSourceId importedSourceId,
      int maxResults,
      CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DocumentCandidate>> GetByProcessingAttemptAsync(
      DocumentProcessingAttemptId processingAttemptId,
      int maxResults,
      CancellationToken cancellationToken = default);

    Task AddAsync(DocumentCandidate documentCandidate, CancellationToken cancellationToken = default);

    Task UpdateAsync(DocumentCandidate documentCandidate, CancellationToken cancellationToken = default);
  }

  public interface IStorageObjectRepository
  {
    Task<StorageObject?> GetByIdAsync(StorageObjectId storageObjectId, CancellationToken cancellationToken = default);

    Task<StorageObject?> GetByContentHashAsync(
      string contentHash,
      string hashAlgorithm,
      int hashAlgorithmVersion,
      CancellationToken cancellationToken = default);

    Task AddAsync(StorageObject storageObject, CancellationToken cancellationToken = default);

    Task UpdateAsync(StorageObject storageObject, CancellationToken cancellationToken = default);
  }
}
