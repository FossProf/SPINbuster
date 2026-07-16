using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using System.Text.Json;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeDocumentImportSessionRepository : IDocumentImportSessionRepository
{
  private readonly Dictionary<DocumentImportSessionId, DocumentImportSession> _sessions = [];

  public Task<DocumentImportSession?> GetByIdAsync(DocumentImportSessionId importSessionId, CancellationToken cancellationToken = default)
  {
    _sessions.TryGetValue(importSessionId, out var session);
    return Task.FromResult(session);
  }

  public Task AddAsync(DocumentImportSession importSession, CancellationToken cancellationToken = default)
  {
    _sessions[importSession.Id] = importSession;
    return Task.CompletedTask;
  }

  public List<DocumentImportSession> UpdatedSessions { get; } = [];

  public Task UpdateAsync(DocumentImportSession importSession, CancellationToken cancellationToken = default)
  {
    _sessions[importSession.Id] = importSession;
    UpdatedSessions.Add(importSession);
    return Task.CompletedTask;
  }
}

internal sealed class FakeStorageObjectRepository : IStorageObjectRepository
{
  private readonly Dictionary<StorageObjectId, StorageObject> _storageObjects = [];

  public Task<StorageObject?> GetByIdAsync(StorageObjectId storageObjectId, CancellationToken cancellationToken = default)
  {
    _storageObjects.TryGetValue(storageObjectId, out var storageObject);
    return Task.FromResult(storageObject);
  }

  public Task<StorageObject?> GetByContentHashAsync(string contentHash, string hashAlgorithm, int hashAlgorithmVersion, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_storageObjects.Values.SingleOrDefault(item =>
      item.ContentHash == contentHash
      && item.HashAlgorithm == hashAlgorithm
      && item.HashAlgorithmVersion == hashAlgorithmVersion));
  }

  public List<StorageObject> AddedStorageObjects { get; } = [];

  public Task AddAsync(StorageObject storageObject, CancellationToken cancellationToken = default)
  {
    _storageObjects[storageObject.Id] = storageObject;
    AddedStorageObjects.Add(storageObject);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(StorageObject storageObject, CancellationToken cancellationToken = default)
  {
    _storageObjects[storageObject.Id] = storageObject;
    return Task.CompletedTask;
  }
}

internal sealed class FakeImportedDocumentSourceRepository : IImportedDocumentSourceRepository
{
  private readonly Dictionary<ImportedSourceId, ImportedDocumentSource> _sources = [];

  public Task<ImportedDocumentSource?> GetByIdAsync(ImportedSourceId importedSourceId, CancellationToken cancellationToken = default)
  {
    _sources.TryGetValue(importedSourceId, out var source);
    return Task.FromResult(source);
  }

  public Task<ImportedDocumentSource?> GetByProjectAndContentHashAsync(ProjectId projectId, string contentHash, string hashAlgorithm, int hashAlgorithmVersion, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_sources.Values.SingleOrDefault(item =>
      item.ProjectId == projectId
      && item.ContentHash == contentHash
      && item.HashAlgorithm == hashAlgorithm
      && item.HashAlgorithmVersion == hashAlgorithmVersion));
  }

  public Task<bool> ExistsInOtherProjectsAsync(ProjectId projectId, string contentHash, string hashAlgorithm, int hashAlgorithmVersion, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_sources.Values.Any(item =>
      item.ProjectId != projectId
      && item.ContentHash == contentHash
      && item.HashAlgorithm == hashAlgorithm
      && item.HashAlgorithmVersion == hashAlgorithmVersion));
  }

  public Task<IReadOnlyCollection<ImportedDocumentSource>> GetByImportSessionAsync(DocumentImportSessionId importSessionId, int maxResults, CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<ImportedDocumentSource>>(
      _sources.Values.Where(item => item.ImportSessionId == importSessionId).Take(maxResults).ToArray());
  }

  public List<ImportedDocumentSource> AddedSources { get; } = [];

  public Task AddAsync(ImportedDocumentSource importedDocumentSource, CancellationToken cancellationToken = default)
  {
    _sources[importedDocumentSource.Id] = importedDocumentSource;
    AddedSources.Add(importedDocumentSource);
    return Task.CompletedTask;
  }
}

internal sealed class FakeDocumentProcessingAttemptRepository : IDocumentProcessingAttemptRepository
{
  private readonly Dictionary<DocumentProcessingAttemptId, DocumentProcessingAttempt> _attempts = [];

  public Task<DocumentProcessingAttempt?> GetByIdAsync(DocumentProcessingAttemptId processingAttemptId, CancellationToken cancellationToken = default)
  {
    _attempts.TryGetValue(processingAttemptId, out var attempt);
    return Task.FromResult(attempt);
  }

  public Task<int> GetNextAttemptNumberAsync(ImportedSourceId importedSourceId, CancellationToken cancellationToken = default)
  {
    var next = _attempts.Values.Count(item => item.ImportedSourceId == importedSourceId) + 1;
    return Task.FromResult(next);
  }

  public Task<IReadOnlyCollection<DocumentProcessingAttempt>> GetByImportedSourceAsync(ImportedSourceId importedSourceId, int maxResults, CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<DocumentProcessingAttempt>>(
      _attempts.Values.Where(item => item.ImportedSourceId == importedSourceId).OrderBy(item => item.AttemptNumber).Take(maxResults).ToArray());
  }

  public List<DocumentProcessingAttempt> AddedAttempts { get; } = [];

  public List<DocumentProcessingAttempt> UpdatedAttempts { get; } = [];

  public Task AddAsync(DocumentProcessingAttempt processingAttempt, CancellationToken cancellationToken = default)
  {
    _attempts[processingAttempt.Id] = processingAttempt;
    AddedAttempts.Add(processingAttempt);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(DocumentProcessingAttempt processingAttempt, CancellationToken cancellationToken = default)
  {
    _attempts[processingAttempt.Id] = processingAttempt;
    UpdatedAttempts.Add(processingAttempt);
    return Task.CompletedTask;
  }
}

internal sealed class FakeDocumentCandidateRepository : IDocumentCandidateRepository
{
  private readonly Dictionary<DocumentCandidateId, DocumentCandidate> _candidates = [];

  public Task<DocumentCandidate?> GetByIdAsync(DocumentCandidateId documentCandidateId, CancellationToken cancellationToken = default)
  {
    _candidates.TryGetValue(documentCandidateId, out var candidate);
    return Task.FromResult(candidate);
  }

  public Task<IReadOnlyCollection<DocumentCandidate>> GetByImportedSourceAsync(ImportedSourceId importedSourceId, int maxResults, CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<DocumentCandidate>>(
      _candidates.Values.Where(item => item.ImportedSourceId == importedSourceId).Take(maxResults).ToArray());
  }

  public Task<IReadOnlyCollection<DocumentCandidate>> GetByProcessingAttemptAsync(DocumentProcessingAttemptId processingAttemptId, int maxResults, CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<DocumentCandidate>>(
      _candidates.Values.Where(item => item.ProcessingAttemptId == processingAttemptId).Take(maxResults).ToArray());
  }

  public List<DocumentCandidate> AddedCandidates { get; } = [];

  public List<DocumentCandidate> UpdatedCandidates { get; } = [];

  public Task AddAsync(DocumentCandidate documentCandidate, CancellationToken cancellationToken = default)
  {
    _candidates[documentCandidate.Id] = documentCandidate;
    AddedCandidates.Add(documentCandidate);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(DocumentCandidate documentCandidate, CancellationToken cancellationToken = default)
  {
    _candidates[documentCandidate.Id] = documentCandidate;
    UpdatedCandidates.Add(documentCandidate);
    return Task.CompletedTask;
  }
}

internal sealed class FakeContentHashService : IContentHashService
{
  public Func<Stream, CancellationToken, Task<ContentHashResult>> ComputeAsyncCore { get; set; } = async (content, cancellationToken) =>
  {
    await using var memory = new MemoryStream();
    await content.CopyToAsync(memory, cancellationToken);
    return new ContentHashResult(
      Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(memory.ToArray())),
      "SHA-256",
      1,
      memory.Length);
  };

  public Task<ContentHashResult> ComputeAsync(Stream content, CancellationToken cancellationToken = default)
  {
    return ComputeAsyncCore(content, cancellationToken);
  }
}

internal sealed class FakeImportedContentInspector : IImportedContentInspector
{
  public Func<string, string?, long, CancellationToken, Task<ImportedContentInspectionResult>> InspectAsyncCore { get; set; } =
    (fileName, declaredMediaType, _, _) => Task.FromResult(new ImportedContentInspectionResult(
      Path.GetFileName(fileName),
      declaredMediaType,
      "application/pdf",
      true,
      []));

  public Task<ImportedContentInspectionResult> InspectAsync(string originalFileName, string? declaredMediaType, long contentLength, CancellationToken cancellationToken = default)
  {
    return InspectAsyncCore(originalFileName, declaredMediaType, contentLength, cancellationToken);
  }
}

internal sealed class FakeImmutableContentStore : IImmutableContentStore
{
  private readonly Dictionary<StorageObjectId, byte[]> _content = [];
  private readonly Dictionary<string, StoredContentResult> _byHash = new(StringComparer.Ordinal);

  public bool ThrowOnStore { get; set; }

  public bool ReturnUnavailableOnOpen { get; set; }

  public int OpenReadCalls { get; private set; }

  public int StoreCalls { get; private set; }

  public Task<StoredContentResult?> FindByHashAsync(string contentHash, string hashAlgorithm, int hashAlgorithmVersion, CancellationToken cancellationToken = default)
  {
    _byHash.TryGetValue($"{contentHash}|{hashAlgorithm}|{hashAlgorithmVersion}", out var result);
    return Task.FromResult(result);
  }

  public async Task<StoredContentResult> StoreAsync(StoreImmutableContentRequest request, CancellationToken cancellationToken = default)
  {
    if (ThrowOnStore)
    {
      throw new IOException("Store failed.");
    }

    await using var memory = new MemoryStream();
    await request.Content.CopyToAsync(memory, cancellationToken);
    StoreCalls++;
    _content[request.StorageObjectId] = memory.ToArray();
    var result = new StoredContentResult(
      request.StorageObjectId,
      request.StorageProviderKey,
      request.ImmutableObjectKey,
      request.ContentLength,
      request.ContentHash,
      request.HashAlgorithm,
      request.HashAlgorithmVersion,
      request.CreatedAtUtc,
      request.EncryptionMetadataId,
      StorageAvailabilityState.Available);
    _byHash[$"{request.ContentHash}|{request.HashAlgorithm}|{request.HashAlgorithmVersion}"] = result;
    return result;
  }

  public Task<OpenImmutableContentResult> OpenReadAsync(StorageObjectId storageObjectId, CancellationToken cancellationToken = default)
  {
    OpenReadCalls++;
    if (ReturnUnavailableOnOpen)
    {
      return Task.FromResult(new OpenImmutableContentResult(Stream.Null, StorageAvailabilityState.Unavailable, string.Empty, "SHA-256", 1, 0));
    }

    var bytes = _content[storageObjectId];
    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    return Task.FromResult(new OpenImmutableContentResult(new MemoryStream(bytes, writable: false), StorageAvailabilityState.Available, hash, "SHA-256", 1, bytes.Length));
  }
}

internal sealed class FakeDocumentImportPolicy : IDocumentImportPolicy
{
  public long MaxContentLengthBytes { get; set; } = 1024 * 1024;

  public int MaxCandidateQueryResults { get; set; } = 50;

  public int MaxProcessingHistoryResults { get; set; } = 50;
}

internal sealed class FakeDocumentProcessor : IDocumentProcessor
{
  private readonly List<string>? _sharedOperationLog;

  public FakeDocumentProcessor(List<string>? sharedOperationLog = null)
  {
    _sharedOperationLog = sharedOperationLog;
  }

  public List<string> SequenceLog { get; } = [];

  public Func<DocumentProcessorRequest, CancellationToken, Task<DocumentProcessorExecutionResult>> ProcessAsyncCore { get; set; } =
    (request, _) =>
    {
      using var document = JsonDocument.Parse("""{"type":"metadata"}""");
      return Task.FromResult(new DocumentProcessorExecutionResult(
        true,
        request.ContentHash,
        DocumentProcessingFailureClassification.None,
        null,
        [
          new DocumentProcessorCandidateResult(
            DocumentCandidateType.MetadataCandidate,
            "document-metadata-candidate",
            "1.0.0",
            document.RootElement.Clone(),
            null,
            ConfidenceBand.High,
            [],
            DocumentProcessorCandidateOutcome.ReadyForReview),
        ]));
    };

  public DocumentProcessorDescriptor Describe()
  {
    return new DocumentProcessorDescriptor("deterministic-fixture", "fixture", "1.0.0", [DocumentProcessingCapability.MetadataInspection], ["application/pdf"]);
  }

  public Task<DocumentProcessorExecutionResult> ProcessAsync(DocumentProcessorRequest request, CancellationToken cancellationToken = default)
  {
    SequenceLog.Add("processor-run");
    _sharedOperationLog?.Add("processor-run");
    return ProcessAsyncCore(request, cancellationToken);
  }
}
