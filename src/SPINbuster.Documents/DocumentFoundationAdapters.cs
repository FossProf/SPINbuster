using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;
using System.Security.Cryptography;
using System.Text.Json;

namespace SPINbuster.Documents;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSpinbusterDocumentFoundationAdapters(
    this IServiceCollection services,
    Action<DeterministicDocumentFoundationOptions>? configure = null)
  {
    var options = new DeterministicDocumentFoundationOptions();
    configure?.Invoke(options);

    services.AddSingleton(options);
    services.AddSingleton<IDocumentImportPolicy, DeterministicDocumentImportPolicy>();
    services.AddSingleton<IContentHashService, Sha256ContentHashService>();
    services.AddSingleton<IImportedContentInspector, BasicImportedContentInspector>();
    services.AddSingleton<InMemoryImmutableContentStore>();
    services.AddSingleton<IImmutableContentStore>(provider => provider.GetRequiredService<InMemoryImmutableContentStore>());
    services.AddSingleton<DeterministicDocumentProcessor>();
    services.AddSingleton<IDocumentProcessor>(provider => provider.GetRequiredService<DeterministicDocumentProcessor>());
    return services;
  }
}

public sealed class DeterministicDocumentFoundationOptions
{
  public long MaxContentLengthBytes { get; set; } = 10 * 1024 * 1024;

  public int MaxCandidateQueryResults { get; set; } = 100;

  public int MaxProcessingHistoryResults { get; set; } = 100;
}

internal sealed class DeterministicDocumentImportPolicy : IDocumentImportPolicy
{
  private readonly DeterministicDocumentFoundationOptions _options;

  public DeterministicDocumentImportPolicy(DeterministicDocumentFoundationOptions options)
  {
    _options = options;
  }

  public long MaxContentLengthBytes => _options.MaxContentLengthBytes;

  public int MaxCandidateQueryResults => _options.MaxCandidateQueryResults;

  public int MaxProcessingHistoryResults => _options.MaxProcessingHistoryResults;
}

public sealed class Sha256ContentHashService : IContentHashService
{
  public async Task<ContentHashResult> ComputeAsync(Stream content, CancellationToken cancellationToken = default)
  {
    using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    var buffer = new byte[81920];
    long length = 0;

    while (true)
    {
      var bytesRead = await content.ReadAsync(buffer, cancellationToken);
      if (bytesRead == 0)
      {
        break;
      }

      hasher.AppendData(buffer, 0, bytesRead);
      length += bytesRead;
    }

    return new ContentHashResult(
      Convert.ToHexString(hasher.GetHashAndReset()),
      "SHA-256",
      1,
      length);
  }
}

public sealed class BasicImportedContentInspector : IImportedContentInspector
{
  private static readonly Dictionary<string, string> KnownMediaTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    [".pdf"] = "application/pdf",
    [".txt"] = "text/plain",
    [".jpg"] = "image/jpeg",
    [".jpeg"] = "image/jpeg",
    [".png"] = "image/png",
    [".tif"] = "image/tiff",
    [".tiff"] = "image/tiff",
    [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  };

  public Task<ImportedContentInspectionResult> InspectAsync(
    string originalFileName,
    string? declaredMediaType,
    long contentLength,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(originalFileName);
    var normalizedFileName = Path.GetFileName(originalFileName).Trim();
    if (string.IsNullOrWhiteSpace(normalizedFileName))
    {
      throw new DomainInvariantException("Original file name must be provided.");
    }

    var normalizedDeclaredMediaType = string.IsNullOrWhiteSpace(declaredMediaType)
      ? null
      : declaredMediaType.Trim().ToLowerInvariant();
    var extension = Path.GetExtension(normalizedFileName);
    var detectedMediaType = KnownMediaTypes.TryGetValue(extension, out var knownMediaType)
      ? knownMediaType
      : null;

    var warnings = new List<string>();
    if (normalizedDeclaredMediaType is not null
      && detectedMediaType is not null
      && !string.Equals(normalizedDeclaredMediaType, detectedMediaType, StringComparison.OrdinalIgnoreCase))
    {
      warnings.Add("Declared media type does not match the extension-derived media type.");
    }

    return Task.FromResult(new ImportedContentInspectionResult(
      normalizedFileName,
      normalizedDeclaredMediaType,
      detectedMediaType,
      detectedMediaType is not null,
      warnings));
  }
}

public sealed class InMemoryImmutableContentStore : IImmutableContentStore
{
  private readonly Dictionary<StorageObjectId, StoredObject> _objects = [];

  public bool SimulateUnavailableRead { get; set; }

  public bool SimulateWriteFailure { get; set; }

  public bool SimulateReadFailure { get; set; }

  public Task<StoredContentResult?> FindByHashAsync(
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    CancellationToken cancellationToken = default)
  {
    var match = _objects.Values.FirstOrDefault(candidate =>
      string.Equals(candidate.ContentHash, contentHash, StringComparison.Ordinal)
      && string.Equals(candidate.HashAlgorithm, hashAlgorithm, StringComparison.Ordinal)
      && candidate.HashAlgorithmVersion == hashAlgorithmVersion);

    return Task.FromResult(match is null ? null : ToResult(match));
  }

  public async Task<StoredContentResult> StoreAsync(
    StoreImmutableContentRequest request,
    CancellationToken cancellationToken = default)
  {
    if (SimulateWriteFailure)
    {
      throw new IOException("Simulated immutable content store write failure.");
    }

    await using var memory = new MemoryStream();
    await request.Content.CopyToAsync(memory, cancellationToken);

    var storedObject = new StoredObject(
      request.StorageObjectId,
      request.StorageProviderKey,
      request.ImmutableObjectKey,
      memory.ToArray(),
      request.ContentHash,
      request.HashAlgorithm,
      request.HashAlgorithmVersion,
      request.CreatedAtUtc,
      request.EncryptionMetadataId);
    _objects[request.StorageObjectId] = storedObject;
    return ToResult(storedObject);
  }

  public Task<OpenImmutableContentResult> OpenReadAsync(
    StorageObjectId storageObjectId,
    CancellationToken cancellationToken = default)
  {
    if (SimulateReadFailure)
    {
      throw new IOException("Simulated immutable content store read failure.");
    }

    if (!_objects.TryGetValue(storageObjectId, out var storedObject))
    {
      return Task.FromResult(new OpenImmutableContentResult(
        Stream.Null,
        StorageAvailabilityState.Missing,
        string.Empty,
        "SHA-256",
        1,
        0));
    }

    if (SimulateUnavailableRead)
    {
      return Task.FromResult(new OpenImmutableContentResult(
        Stream.Null,
        StorageAvailabilityState.Unavailable,
        storedObject.ContentHash,
        storedObject.HashAlgorithm,
        storedObject.HashAlgorithmVersion,
        storedObject.Bytes.Length));
    }

    return Task.FromResult(new OpenImmutableContentResult(
      new MemoryStream(storedObject.Bytes, writable: false),
      StorageAvailabilityState.Available,
      storedObject.ContentHash,
      storedObject.HashAlgorithm,
      storedObject.HashAlgorithmVersion,
      storedObject.Bytes.Length));
  }

  private static StoredContentResult ToResult(StoredObject storedObject)
  {
    return new StoredContentResult(
      storedObject.StorageObjectId,
      storedObject.StorageProviderKey,
      storedObject.ImmutableObjectKey,
      storedObject.Bytes.Length,
      storedObject.ContentHash,
      storedObject.HashAlgorithm,
      storedObject.HashAlgorithmVersion,
      storedObject.CreatedAtUtc,
      storedObject.EncryptionMetadataId,
      StorageAvailabilityState.Available);
  }

  private sealed record StoredObject(
    StorageObjectId StorageObjectId,
    string StorageProviderKey,
    string ImmutableObjectKey,
    byte[] Bytes,
    string ContentHash,
    string HashAlgorithm,
    int HashAlgorithmVersion,
    DateTimeOffset CreatedAtUtc,
    string? EncryptionMetadataId);
}

public sealed class DeterministicDocumentProcessor : IDocumentProcessor
{
  public Func<DocumentProcessorRequest, CancellationToken, Task<DocumentProcessorExecutionResult>> ProcessAsyncCore { get; set; }

  public DeterministicDocumentProcessor()
  {
    ProcessAsyncCore = DefaultProcessAsync;
  }

  public DocumentProcessorDescriptor Describe()
  {
    return new DocumentProcessorDescriptor(
      "deterministic-fixture",
      "document-fixture",
      "1.0.0",
      [
        DocumentProcessingCapability.MetadataInspection,
        DocumentProcessingCapability.FragmentDetection,
        DocumentProcessingCapability.CitationDetection,
      ],
      [
        "application/pdf",
        "text/plain",
        "image/jpeg",
        "image/png",
      ]);
  }

  public Task<DocumentProcessorExecutionResult> ProcessAsync(
    DocumentProcessorRequest request,
    CancellationToken cancellationToken = default)
  {
    return ProcessAsyncCore(request, cancellationToken);
  }

  private static async Task<DocumentProcessorExecutionResult> DefaultProcessAsync(
    DocumentProcessorRequest request,
    CancellationToken cancellationToken)
  {
    if (request.DetectedMediaType is null)
    {
      return new DocumentProcessorExecutionResult(
        false,
        null,
        DocumentProcessingFailureClassification.UnsupportedMediaType,
        "Detected media type is not supported.",
        []);
    }

    using var document = JsonDocument.Parse($$"""
{
  "title": "{{request.OriginalFileName}}",
  "contentHash": "{{request.ContentHash}}",
  "mediaType": "{{request.DetectedMediaType}}"
}
""");

    await Task.Yield();
    return new DocumentProcessorExecutionResult(
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
      ]);
  }
}
