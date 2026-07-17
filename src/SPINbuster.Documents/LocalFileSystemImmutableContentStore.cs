using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;
using System.Security.Cryptography;

namespace SPINbuster.Documents;

public sealed class LocalFileSystemImmutableContentStoreOptions
{
  public string RootPath { get; set; } = string.Empty;

  public bool CreateRootIfMissing { get; set; } = true;

  public string ObjectDirectoryName { get; set; } = "objects";

  public string TemporaryDirectoryName { get; set; } = "_tmp";

  public bool FlushWritesThroughToDisk { get; set; } = true;

  public bool VerifyFinalObjectAfterWrite { get; set; } = true;

  public bool VerifyInventoryObjectIntegrity { get; set; } = true;

  public int MaxInventoryResults { get; set; } = 256;
}

public sealed record LocalImmutableStoredObjectInventoryItem(
  StorageObjectId StorageObjectId,
  string ProviderRelativeObjectKey,
  long ContentLength,
  string ContentHash,
  string HashAlgorithm,
  int HashAlgorithmVersion,
  DateTimeOffset CreatedAtUtc);

public static class LocalFileSystemImmutableContentStoreServiceCollectionExtensions
{
  public static IServiceCollection AddSpinbusterLocalFileSystemImmutableContentStore(
    this IServiceCollection services,
    Action<LocalFileSystemImmutableContentStoreOptions> configure)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configure);

    var options = new LocalFileSystemImmutableContentStoreOptions();
    configure(options);

    services.AddSingleton(options);
    services.AddSingleton<LocalFileSystemImmutableContentStore>();
    services.AddSingleton<IImmutableContentStore>(provider => provider.GetRequiredService<LocalFileSystemImmutableContentStore>());
    return services;
  }
}

/// <summary>
/// Stores immutable content beneath a canonical root using an ID-addressed layout:
/// objects/{first-two-hex}/{next-two-hex}/{storage-object-id-n}.blob
///
/// The path is derived only from <see cref="StorageObjectId"/> so imported file names,
/// caller-provided object-key text, and other untrusted metadata never influence the
/// physical layout. The adapter also validates that preexisting objects still match
/// their immutable identity before treating them as successful writes.
/// </summary>
public sealed class LocalFileSystemImmutableContentStore : IImmutableContentStore
{
  private const string BlobExtension = ".blob";
  private readonly LocalFileSystemImmutableContentStoreOptions _options;
  private readonly string _canonicalRootPath;
  private readonly string _objectsRootPath;
  private readonly string _temporaryRootPath;

  public LocalFileSystemImmutableContentStore(LocalFileSystemImmutableContentStoreOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    _options = options;
    var configuredRoot = string.IsNullOrWhiteSpace(options.RootPath)
      ? throw new InvalidOperationException("Local filesystem immutable content store root path must be configured.")
      : options.RootPath.Trim();
    var objectDirectoryName = ValidateDirectorySegment(options.ObjectDirectoryName, nameof(options.ObjectDirectoryName));
    var temporaryDirectoryName = ValidateDirectorySegment(options.TemporaryDirectoryName, nameof(options.TemporaryDirectoryName));
    if (options.MaxInventoryResults <= 0)
    {
      throw new InvalidOperationException($"{nameof(options.MaxInventoryResults)} must be greater than zero.");
    }

    _canonicalRootPath = Path.GetFullPath(configuredRoot);
    _objectsRootPath = GetPathUnderRoot(CreateRelativePath(objectDirectoryName));
    _temporaryRootPath = GetPathUnderRoot(CreateRelativePath(temporaryDirectoryName));

    EnsureRootAvailable(options.CreateRootIfMissing);
  }

  public Task<StoredContentResult?> FindByHashAsync(
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    CancellationToken cancellationToken = default)
  {
    return ExecuteWithClassificationAsync(() =>
    {
      ValidateHashRequest(contentHash, hashAlgorithm, hashAlgorithmVersion);
      cancellationToken.ThrowIfCancellationRequested();
      EnsureRootAvailable(createRootIfMissing: false);

      if (!Directory.Exists(_objectsRootPath))
      {
        return Task.FromResult<StoredContentResult?>(null);
      }

      foreach (var objectPath in EnumerateObjectPaths())
      {
        cancellationToken.ThrowIfCancellationRequested();

        var verification = VerifyExistingObject(
          objectPath,
          contentHash,
          hashAlgorithm,
          hashAlgorithmVersion,
          expectedLength: null);
        if (!verification.Exists || !verification.MatchesIdentity)
        {
          continue;
        }

        var storageObjectId = ParseStorageObjectPath(objectPath);
        return Task.FromResult<StoredContentResult?>(
          CreateStoredContentResult(
            storageObjectId,
            "local-filesystem",
            GetProviderRelativeObjectKey(storageObjectId),
            verification.ActualLength,
            verification.ActualHash,
            hashAlgorithm,
            hashAlgorithmVersion,
            new DateTimeOffset(File.GetCreationTimeUtc(objectPath), TimeSpan.Zero),
            null,
            StorageAvailabilityState.Available));
      }

      return Task.FromResult<StoredContentResult?>(null);
    });
  }

  public Task<IReadOnlyCollection<LocalImmutableStoredObjectInventoryItem>> ListStoredObjectsAsync(
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    return ExecuteWithClassificationAsync(() =>
    {
      cancellationToken.ThrowIfCancellationRequested();
      EnsureRootAvailable(createRootIfMissing: false);

      if (maxResults <= 0)
      {
        throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store inventory limit must be greater than zero.");
      }

      if (maxResults > _options.MaxInventoryResults)
      {
        throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store inventory limit exceeds the configured maximum.");
      }

      if (!Directory.Exists(_objectsRootPath))
      {
        return Task.FromResult<IReadOnlyCollection<LocalImmutableStoredObjectInventoryItem>>([]);
      }

      var objectPaths = EnumerateObjectPaths()
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .Take(maxResults)
        .ToArray();

      var items = new List<LocalImmutableStoredObjectInventoryItem>(objectPaths.Length);
      foreach (var objectPath in objectPaths)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var storageObjectId = ParseStorageObjectPath(objectPath);
        string contentHash;
        long contentLength;
        if (_options.VerifyInventoryObjectIntegrity)
        {
          var verification = VerifyExistingObject(objectPath, expectedHash: null, "SHA-256", 1, expectedLength: null);
          contentHash = verification.ActualHash;
          contentLength = verification.ActualLength;
        }
        else
        {
          EnsureExistingAncestorsDoNotUseReparsePoints(objectPath, includeLeafWhenExisting: true);
          EnsureRegularFile(objectPath);
          var fileInfo = new FileInfo(objectPath);
          contentLength = fileInfo.Length;
          contentHash = string.Empty;
        }

        items.Add(new LocalImmutableStoredObjectInventoryItem(
          storageObjectId,
          GetProviderRelativeObjectKey(storageObjectId),
          contentLength,
          contentHash,
          "SHA-256",
          1,
          new DateTimeOffset(File.GetCreationTimeUtc(objectPath), TimeSpan.Zero)));
      }

      return Task.FromResult<IReadOnlyCollection<LocalImmutableStoredObjectInventoryItem>>(items);
    });
  }

  public Task<StoredContentResult> StoreAsync(
    StoreImmutableContentRequest request,
    CancellationToken cancellationToken = default)
  {
    return ExecuteWithClassificationAsync(async () =>
    {
      ArgumentNullException.ThrowIfNull(request);
      ValidateHashRequest(request.ContentHash, request.HashAlgorithm, request.HashAlgorithmVersion);
      cancellationToken.ThrowIfCancellationRequested();

      EnsureRootAvailable(createRootIfMissing: true);

      var objectKey = GetProviderRelativeObjectKey(request.StorageObjectId);
      var finalPath = GetPathUnderRoot(objectKey);
      EnsureExistingAncestorsDoNotUseReparsePoints(finalPath, includeLeafWhenExisting: true);

      var existingVerification = VerifyExistingObject(
        finalPath,
        request.ContentHash,
        request.HashAlgorithm,
        request.HashAlgorithmVersion,
        request.ContentLength);
      if (existingVerification.Exists)
      {
        if (!existingVerification.MatchesIdentity)
        {
          throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store found an existing object whose bytes do not match the requested immutable identity.");
        }

        return CreateStoredContentResult(
          request.StorageObjectId,
          request.StorageProviderKey,
          objectKey,
          existingVerification.ActualLength,
          existingVerification.ActualHash,
          request.HashAlgorithm,
          request.HashAlgorithmVersion,
          new DateTimeOffset(File.GetCreationTimeUtc(finalPath), TimeSpan.Zero),
          request.EncryptionMetadataId,
          StorageAvailabilityState.Available);
      }

      var temporaryRelativeKey = CreateRelativePath(
        Path.GetFileName(_temporaryRootPath),
        $"{Guid.NewGuid():N}.tmp");
      var temporaryPath = GetPathUnderRoot(temporaryRelativeKey);
      EnsureExistingAncestorsDoNotUseReparsePoints(temporaryPath, includeLeafWhenExisting: false);

      Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
      Directory.CreateDirectory(Path.GetDirectoryName(temporaryPath)!);

      var temporaryFileCreated = false;
      try
      {
        var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        if (_options.FlushWritesThroughToDisk)
        {
          fileOptions |= FileOptions.WriteThrough;
        }

        await using (var temporaryStream = new FileStream(
          temporaryPath,
          FileMode.CreateNew,
          FileAccess.Write,
          FileShare.None,
          81920,
          fileOptions))
        {
          temporaryFileCreated = true;
          await request.Content.CopyToAsync(temporaryStream, cancellationToken);
          temporaryStream.Flush(_options.FlushWritesThroughToDisk);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var temporaryVerification = VerifyExistingObject(
          temporaryPath,
          request.ContentHash,
          request.HashAlgorithm,
          request.HashAlgorithmVersion,
          request.ContentLength);
        if (!temporaryVerification.Exists)
        {
          throw CreateFailure(ImmutableContentStoreFailureClassification.IoFailure, "Immutable content store could not verify the staged temporary object.");
        }

        if (!temporaryVerification.MatchesIdentity)
        {
          if (temporaryVerification.ActualLength != request.ContentLength)
          {
            throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store staged bytes whose length does not match the requested content length.");
          }

          throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store staged bytes whose hash does not match the requested content hash.");
        }

        try
        {
          File.Move(temporaryPath, finalPath);
          temporaryFileCreated = false;
        }
        catch (IOException) when (File.Exists(finalPath))
        {
          var racedVerification = VerifyExistingObject(
            finalPath,
            request.ContentHash,
            request.HashAlgorithm,
            request.HashAlgorithmVersion,
            request.ContentLength);
          if (!racedVerification.MatchesIdentity)
          {
            throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store found a conflicting object while finalizing the immutable write.");
          }

          DeleteIfPresent(temporaryPath);
          temporaryFileCreated = false;
        }

        if (_options.VerifyFinalObjectAfterWrite)
        {
          var finalVerification = VerifyExistingObject(
            finalPath,
            request.ContentHash,
            request.HashAlgorithm,
            request.HashAlgorithmVersion,
            request.ContentLength);
          if (!finalVerification.Exists || !finalVerification.MatchesIdentity)
          {
            throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store could not re-verify the finalized object identity.");
          }
        }

        return CreateStoredContentResult(
          request.StorageObjectId,
          request.StorageProviderKey,
          objectKey,
          request.ContentLength,
          request.ContentHash,
          request.HashAlgorithm,
          request.HashAlgorithmVersion,
          request.CreatedAtUtc,
          request.EncryptionMetadataId,
          StorageAvailabilityState.Available);
      }
      catch
      {
        if (temporaryFileCreated)
        {
          DeleteIfPresent(temporaryPath);
        }

        throw;
      }
    });
  }

  public Task<OpenImmutableContentResult> OpenReadAsync(
    StorageObjectId storageObjectId,
    CancellationToken cancellationToken = default)
  {
    return ExecuteWithClassificationAsync(() =>
    {
      cancellationToken.ThrowIfCancellationRequested();
      EnsureRootAvailable(createRootIfMissing: false);

      var objectKey = GetProviderRelativeObjectKey(storageObjectId);
      var finalPath = GetPathUnderRoot(objectKey);
      EnsureExistingAncestorsDoNotUseReparsePoints(finalPath, includeLeafWhenExisting: true);

      if (!File.Exists(finalPath) && !Directory.Exists(finalPath))
      {
        return Task.FromResult(new OpenImmutableContentResult(
          Stream.Null,
          StorageAvailabilityState.Missing,
          string.Empty,
          "SHA-256",
          1,
          0));
      }

      var verification = VerifyExistingObject(finalPath, expectedHash: null, "SHA-256", 1, expectedLength: null);
      var stream = new FileStream(
        finalPath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        81920,
        FileOptions.Asynchronous | FileOptions.SequentialScan);

      return Task.FromResult(new OpenImmutableContentResult(
        stream,
        StorageAvailabilityState.Available,
        verification.ActualHash,
        "SHA-256",
        1,
        verification.ActualLength));
    });
  }

  private void EnsureRootAvailable(bool createRootIfMissing)
  {
    if (File.Exists(_canonicalRootPath))
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.RootUnavailable, "Configured immutable content store root path points to a file instead of a directory.");
    }

    if (!Directory.Exists(_canonicalRootPath))
    {
      if (!createRootIfMissing)
      {
        throw CreateFailure(ImmutableContentStoreFailureClassification.RootUnavailable, "Configured immutable content store root directory does not exist.");
      }

      Directory.CreateDirectory(_canonicalRootPath);
    }

    EnsureExistingAncestorsDoNotUseReparsePoints(_canonicalRootPath, includeLeafWhenExisting: true);
  }

  private IEnumerable<string> EnumerateObjectPaths()
  {
    if (!Directory.Exists(_objectsRootPath))
    {
      yield break;
    }

    EnsureExistingAncestorsDoNotUseReparsePoints(_objectsRootPath, includeLeafWhenExisting: true);
    foreach (var objectPath in Directory.EnumerateFiles(_objectsRootPath, $"*{BlobExtension}", SearchOption.AllDirectories))
    {
      EnsureExistingAncestorsDoNotUseReparsePoints(objectPath, includeLeafWhenExisting: true);
      EnsureRegularFile(objectPath);
      ValidateObjectLayout(objectPath);
      yield return objectPath;
    }
  }

  private string GetProviderRelativeObjectKey(StorageObjectId storageObjectId)
  {
    var normalizedId = storageObjectId.Value.ToString("N");
    return CreateRelativePath(
      Path.GetFileName(_objectsRootPath),
      normalizedId[..2],
      normalizedId[2..4],
      $"{normalizedId}{BlobExtension}");
  }

  private string GetPathUnderRoot(string providerRelativePath)
  {
    var rootWithSeparator = _canonicalRootPath.EndsWith(Path.DirectorySeparatorChar)
      ? _canonicalRootPath
      : _canonicalRootPath + Path.DirectorySeparatorChar;
    var fullPath = Path.GetFullPath(Path.Combine(_canonicalRootPath, providerRelativePath.Replace('/', Path.DirectorySeparatorChar)));
    if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
      && !string.Equals(fullPath, _canonicalRootPath, StringComparison.OrdinalIgnoreCase))
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.AccessDenied, "Generated immutable content path escaped the configured storage root.");
    }

    return fullPath;
  }

  private static string CreateRelativePath(params string[] segments)
  {
    return string.Join("/", segments.Where(segment => !string.IsNullOrWhiteSpace(segment)));
  }

  private static string ValidateDirectorySegment(string value, string parameterName)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new InvalidOperationException($"{parameterName} must be configured.");
    }

    var trimmed = value.Trim();
    if (trimmed.Contains(Path.DirectorySeparatorChar)
      || trimmed.Contains(Path.AltDirectorySeparatorChar)
      || trimmed == "."
      || trimmed == "..")
    {
      throw new InvalidOperationException($"{parameterName} must be a single safe directory segment.");
    }

    return trimmed;
  }

  private static void ValidateHashRequest(string contentHash, string hashAlgorithm, int hashAlgorithmVersion)
  {
    if (string.IsNullOrWhiteSpace(contentHash))
    {
      throw new InvalidOperationException("Immutable content store content hash must be provided.");
    }

    if (!string.Equals(hashAlgorithm, "SHA-256", StringComparison.Ordinal))
    {
      throw new NotSupportedException($"Immutable content store does not support hash algorithm '{hashAlgorithm}'.");
    }

    if (hashAlgorithmVersion != 1)
    {
      throw new NotSupportedException($"Immutable content store does not support hash algorithm version '{hashAlgorithmVersion}'.");
    }
  }

  private ObjectVerification VerifyExistingObject(
    string path,
    string? expectedHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    long? expectedLength)
  {
    if (!File.Exists(path) && !Directory.Exists(path))
    {
      return ObjectVerification.Missing;
    }

    EnsureExistingAncestorsDoNotUseReparsePoints(path, includeLeafWhenExisting: true);
    EnsureRegularFile(path);

    var fileInfo = new FileInfo(path);
    var actualLength = fileInfo.Length;
    var actualHash = ComputeHash(path, hashAlgorithm, hashAlgorithmVersion);
    var matchesLength = !expectedLength.HasValue || actualLength == expectedLength.Value;
    var matchesHash = expectedHash is null || string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
    return new ObjectVerification(true, matchesLength && matchesHash, actualLength, actualHash);
  }

  private static string ComputeHash(string path, string hashAlgorithm, int hashAlgorithmVersion)
  {
    ValidateHashRequest("placeholder", hashAlgorithm, hashAlgorithmVersion);
    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
    using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    var buffer = new byte[81920];
    while (true)
    {
      var bytesRead = stream.Read(buffer, 0, buffer.Length);
      if (bytesRead == 0)
      {
        break;
      }

      hasher.AppendData(buffer, 0, bytesRead);
    }

    return Convert.ToHexString(hasher.GetHashAndReset());
  }

  private static StoredContentResult CreateStoredContentResult(
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
    return new StoredContentResult(
      storageObjectId,
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

  private StorageObjectId ParseStorageObjectPath(string objectPath)
  {
    ValidateObjectLayout(objectPath);

    var fileName = Path.GetFileNameWithoutExtension(objectPath);
    if (!Guid.TryParseExact(fileName, "N", out var value))
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store encountered an object whose file name does not match the expected storage-object identifier layout.");
    }

    return new StorageObjectId(value);
  }

  private void ValidateObjectLayout(string objectPath)
  {
    var relativePath = Path.GetRelativePath(_objectsRootPath, objectPath);
    if (string.Equals(relativePath, ".", StringComparison.Ordinal))
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store encountered a malformed object path.");
    }

    var segments = relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length != 3)
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store encountered a malformed object path.");
    }

    var fileName = Path.GetFileNameWithoutExtension(segments[2]);
    if (!Guid.TryParseExact(fileName, "N", out var value))
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store encountered an object whose file name does not match the expected storage-object identifier layout.");
    }

    var normalizedId = value.ToString("N");
    if (!string.Equals(segments[0], normalizedId[..2], StringComparison.OrdinalIgnoreCase)
      || !string.Equals(segments[1], normalizedId[2..4], StringComparison.OrdinalIgnoreCase)
      || !string.Equals(Path.GetExtension(segments[2]), BlobExtension, StringComparison.OrdinalIgnoreCase))
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store encountered an object whose path does not match the configured ID-addressed layout.");
    }
  }

  private void EnsureExistingAncestorsDoNotUseReparsePoints(string path, bool includeLeafWhenExisting)
  {
    var current = _canonicalRootPath;
    VerifyNoReparsePoint(current);

    var relative = Path.GetRelativePath(_canonicalRootPath, path);
    if (string.Equals(relative, ".", StringComparison.Ordinal))
    {
      return;
    }

    var segments = relative.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
    for (var index = 0; index < segments.Length; index++)
    {
      current = Path.Combine(current, segments[index]);
      var isLeaf = index == segments.Length - 1;
      if (!Directory.Exists(current) && !File.Exists(current))
      {
        break;
      }

      if (!includeLeafWhenExisting && isLeaf)
      {
        break;
      }

      // Windows reparse-point checks reduce accidental traversal through symlinks or
      // junctions, but they remain a bounded defense rather than a perfect guarantee.
      // A privileged actor can still swap a path after validation and before final use.
      VerifyNoReparsePoint(current);
    }
  }

  private static void VerifyNoReparsePoint(string path)
  {
    var attributes = File.GetAttributes(path);
    if ((attributes & FileAttributes.ReparsePoint) != 0)
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.AccessDenied, "Immutable content store encountered a reparse point in the configured storage path.");
    }
  }

  private static void EnsureRegularFile(string path)
  {
    var attributes = File.GetAttributes(path);
    if ((attributes & FileAttributes.Directory) != 0)
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.IntegrityMismatch, "Immutable content store expected a file but found a directory.");
    }

    if ((attributes & FileAttributes.ReparsePoint) != 0)
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.AccessDenied, "Immutable content store encountered a reparse point in the configured storage path.");
    }
  }

  private static void DeleteIfPresent(string path)
  {
    if (File.Exists(path))
    {
      File.Delete(path);
    }
  }

  private static ImmutableContentStoreException CreateFailure(
    ImmutableContentStoreFailureClassification failureClassification,
    string message)
  {
    return new ImmutableContentStoreException(failureClassification, message);
  }

  private static async Task<T> ExecuteWithClassificationAsync<T>(Func<Task<T>> action)
  {
    try
    {
      return await action();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (ImmutableContentStoreException)
    {
      throw;
    }
    catch (UnauthorizedAccessException)
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.AccessDenied, "Immutable content store access was denied.");
    }
    catch (DirectoryNotFoundException)
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.RootUnavailable, "Immutable content store root path is unavailable.");
    }
    catch (IOException)
    {
      throw CreateFailure(ImmutableContentStoreFailureClassification.IoFailure, "Immutable content store I/O failed.");
    }
  }

  private readonly record struct ObjectVerification(
    bool Exists,
    bool MatchesIdentity,
    long ActualLength,
    string ActualHash)
  {
    public static ObjectVerification Missing => new(false, false, 0, string.Empty);
  }
}
