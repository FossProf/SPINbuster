using SPINbuster.Application.Abstractions;
using SPINbuster.Documents;
using SPINbuster.Domain;
using System.Text;

namespace SPINbuster.Documents.Tests;

public sealed class DocumentFoundationAdapterTests
{
  [Fact]
  public async Task Sha256ContentHashServiceProducesDeterministicUppercaseHash()
  {
    var service = new Sha256ContentHashService();
    await using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));

    var result = await service.ComputeAsync(content);

    Assert.Equal("B94D27B9934D3E08A52E52D7DA7DABFAC484EFE37A5380EE9088F7ACE2EFCDE9", result.ContentHash);
    Assert.Equal("SHA-256", result.HashAlgorithm);
  }

  [Fact]
  public async Task InMemoryImmutableContentStorePreservesExactBytes()
  {
    var store = new InMemoryImmutableContentStore();
    var bytes = Encoding.UTF8.GetBytes("preserve me");
    await using var content = new MemoryStream(bytes);

    var stored = await store.StoreAsync(new StoreImmutableContentRequest(
      StorageObjectId.New(),
      "provider",
      "object-key",
      Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)),
      "SHA-256",
      1,
      bytes.Length,
      content,
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero),
      null));
    var reopened = await store.OpenReadAsync(stored.StorageObjectId);
    await using var reopenedStream = reopened.Content;
    using var memory = new MemoryStream();
    await reopenedStream.CopyToAsync(memory);

    Assert.Equal(bytes, memory.ToArray());
  }

  [Fact]
  public async Task InMemoryImmutableContentStoreCanSimulateUnavailableRead()
  {
    var store = new InMemoryImmutableContentStore
    {
      SimulateUnavailableRead = true,
    };
    var bytes = Encoding.UTF8.GetBytes("preserve me");
    await using var content = new MemoryStream(bytes);
    var stored = await store.StoreAsync(new StoreImmutableContentRequest(
      StorageObjectId.New(),
      "provider",
      "object-key",
      Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)),
      "SHA-256",
      1,
      bytes.Length,
      content,
      new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero),
      null));

    var result = await store.OpenReadAsync(stored.StorageObjectId);

    Assert.Equal(StorageAvailabilityState.Unavailable, result.AvailabilityState);
  }

  [Fact]
  public async Task BasicImportedContentInspectorWarnsOnDeclaredMediaMismatch()
  {
    var inspector = new BasicImportedContentInspector();

    var result = await inspector.InspectAsync("detail.pdf", "image/jpeg", 10);

    Assert.Equal("application/pdf", result.DetectedMediaType);
    Assert.Single(result.Warnings);
  }

  [Fact]
  public async Task DeterministicDocumentProcessorProducesDeterministicMetadataAndFragmentCandidates()
  {
    var processor = new DeterministicDocumentProcessor();
    await using var content = new MemoryStream(Encoding.UTF8.GetBytes("Section 03 30 00 requires concrete curing in accordance with the approved project specifications."));

    var result = await processor.ProcessAsync(new DocumentProcessorRequest(
      ImportedSourceId.New(),
      ProjectId.New(),
      "detail.pdf",
      "application/pdf",
      "application/pdf",
      "hash",
      "SHA-256",
      1,
      content.Length,
      content));

    Assert.True(result.Success);
    Assert.Equal(2, result.Candidates.Count);
    Assert.Contains(result.Candidates, candidate => candidate.CandidateType == DocumentCandidateType.MetadataCandidate);
    Assert.Contains(result.Candidates, candidate => candidate.CandidateType == DocumentCandidateType.FragmentCandidate && candidate.SourceLocator == "line:1");
  }

  [Fact]
  public async Task LocalFileSystemImmutableContentStoreStoresAndReopensExactBytes()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("filesystem exact bytes");
      var request = CreateStoreRequest(bytes);

      var stored = await store.StoreAsync(request);
      var reopened = await store.OpenReadAsync(request.StorageObjectId);
      await using var reopenedContent = reopened.Content;
      using var memory = new MemoryStream();
      await reopenedContent.CopyToAsync(memory);

      Assert.Equal(bytes, memory.ToArray());
      Assert.Equal(StorageAvailabilityState.Available, reopened.AvailabilityState);
      Assert.Equal(stored.ImmutableObjectKey, GetSingleObjectKey(rootPath));
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task LocalFileSystemImmutableContentStoreReopensWithNewAdapterInstanceUsingSameRoot()
  {
    var rootPath = CreateRootPath();

    try
    {
      var firstStore = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("reopen across adapter instances");
      var request = CreateStoreRequest(bytes);
      var stored = await firstStore.StoreAsync(request);

      var secondStore = CreateFileSystemStore(rootPath);
      var reopened = await secondStore.OpenReadAsync(request.StorageObjectId);
      await using var reopenedContent = reopened.Content;
      using var memory = new MemoryStream();
      await reopenedContent.CopyToAsync(memory);

      Assert.Equal(bytes, memory.ToArray());
      Assert.Equal(stored.ImmutableObjectKey, GetSingleObjectKey(rootPath));
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task LocalFileSystemImmutableContentStoreReturnsStableProviderRelativeObjectKeyWithoutAbsolutePaths()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("stable key");
      var storageObjectId = StorageObjectId.New();
      var first = await store.StoreAsync(CreateStoreRequest(bytes, storageObjectId: storageObjectId));
      var second = await store.StoreAsync(CreateStoreRequest(bytes, storageObjectId: storageObjectId));

      Assert.Equal(first.ImmutableObjectKey, second.ImmutableObjectKey);
      Assert.Equal("provider", first.StorageProviderKey);
      Assert.DoesNotContain(rootPath, first.ImmutableObjectKey, StringComparison.OrdinalIgnoreCase);
      Assert.False(Path.IsPathRooted(first.ImmutableObjectKey));
      Assert.Equal(first.ImmutableObjectKey.Replace('/', Path.DirectorySeparatorChar), GetSingleObjectKey(rootPath).Replace('/', Path.DirectorySeparatorChar));
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task CallerSuppliedImmutableObjectKeyCannotInfluencePhysicalPathOrEscapeRoot()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("path safety");
      var request = CreateStoreRequest(
        bytes,
        immutableObjectKey: @"..\..\..\dangerous\payload.exe");

      var stored = await store.StoreAsync(request);
      var fullPath = Path.Combine(rootPath, stored.ImmutableObjectKey.Replace('/', Path.DirectorySeparatorChar));

      Assert.DoesNotContain("dangerous", stored.ImmutableObjectKey, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("..", stored.ImmutableObjectKey, StringComparison.Ordinal);
      Assert.StartsWith(Path.GetFullPath(rootPath), Path.GetFullPath(fullPath), StringComparison.OrdinalIgnoreCase);
      Assert.Equal(GetSingleObjectKey(rootPath), stored.ImmutableObjectKey);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task IdenticalRepeatedWriteSucceedsSafely()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("repeat safely");
      var storageObjectId = StorageObjectId.New();
      var first = await store.StoreAsync(CreateStoreRequest(bytes, storageObjectId: storageObjectId));
      var second = await store.StoreAsync(CreateStoreRequest(bytes, storageObjectId: storageObjectId));

      Assert.Equal(first.ImmutableObjectKey, second.ImmutableObjectKey);
      Assert.Single(Directory.EnumerateFiles(Path.Combine(rootPath, "objects"), "*.blob", SearchOption.AllDirectories));
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task ConcurrentIdenticalWritesConvergeSafelyWithoutCorruption()
  {
    var rootPath = CreateRootPath();

    try
    {
      var storageObjectId = StorageObjectId.New();
      var bytes = Encoding.UTF8.GetBytes(new string('A', 262144));
      var tasks = Enumerable.Range(0, 6)
        .Select(_ =>
        {
          var store = CreateFileSystemStore(rootPath);
          var request = CreateStoreRequest(
            bytes,
            storageObjectId: storageObjectId,
            content: new MemoryStream(bytes, writable: false));
          return store.StoreAsync(request);
        })
        .ToArray();

      var results = await Task.WhenAll(tasks);
      var reopened = await CreateFileSystemStore(rootPath).OpenReadAsync(storageObjectId);
      await using var reopenedContent = reopened.Content;
      using var memory = new MemoryStream();
      await reopenedContent.CopyToAsync(memory);

      Assert.All(results, result => Assert.Equal(results[0].ImmutableObjectKey, result.ImmutableObjectKey));
      Assert.Equal(bytes, memory.ToArray());
      Assert.Single(Directory.EnumerateFiles(Path.Combine(rootPath, "objects"), "*.blob", SearchOption.AllDirectories));
      AssertNoTemporaryFiles(rootPath);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task ConcurrentConflictingWritesFailExplicitlyWithoutReplacingWinner()
  {
    var rootPath = CreateRootPath();

    try
    {
      var storageObjectId = StorageObjectId.New();
      var winnerBytes = Encoding.UTF8.GetBytes(new string('B', 262144));
      var conflictingBytes = Encoding.UTF8.GetBytes(new string('C', 262144));
      var tasks = Enumerable.Range(0, 8)
        .Select(index =>
        {
          var bytes = index % 2 == 0 ? winnerBytes : conflictingBytes;
          var store = CreateFileSystemStore(rootPath);
          return CaptureExceptionAsync(() => store.StoreAsync(CreateStoreRequest(
            bytes,
            storageObjectId: storageObjectId,
            content: new MemoryStream(bytes, writable: false))));
        })
        .ToArray();

      var exceptions = await Task.WhenAll(tasks);
      var reopened = await CreateFileSystemStore(rootPath).OpenReadAsync(storageObjectId);
      await using var reopenedContent = reopened.Content;
      using var memory = new MemoryStream();
      await reopenedContent.CopyToAsync(memory);
      var finalBytes = memory.ToArray();

      Assert.Contains(exceptions, exception => exception is null);
      var failures = exceptions.Where(exception => exception is not null).ToArray();
      Assert.NotEmpty(failures);
      Assert.All(failures, failure =>
      {
        var classifiedFailure = Assert.IsType<ImmutableContentStoreException>(failure);
        Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, classifiedFailure.FailureClassification);
      });
      Assert.True(finalBytes.SequenceEqual(winnerBytes) || finalBytes.SequenceEqual(conflictingBytes));
      Assert.Single(Directory.EnumerateFiles(Path.Combine(rootPath, "objects"), "*.blob", SearchOption.AllDirectories));
      AssertNoTemporaryFiles(rootPath);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task ConflictingExistingBytesFailWithoutOverwrite()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var originalBytes = Encoding.UTF8.GetBytes("original bytes");
      var conflictingBytes = Encoding.UTF8.GetBytes("conflicting bytes");
      var storageObjectId = StorageObjectId.New();
      await store.StoreAsync(CreateStoreRequest(originalBytes, storageObjectId: storageObjectId));

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(conflictingBytes, storageObjectId: storageObjectId)));

      Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, exception.FailureClassification);
      var reopened = await store.OpenReadAsync(storageObjectId);
      await using var reopenedContent = reopened.Content;
      using var memory = new MemoryStream();
      await reopenedContent.CopyToAsync(memory);
      Assert.Equal(originalBytes, memory.ToArray());
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task ExistingCorruptDirectoryAtObjectPathIsDetected()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("corrupt directory");
      var storageObjectId = StorageObjectId.New();
      Directory.CreateDirectory(GetExpectedObjectPath(rootPath, storageObjectId));

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(bytes, storageObjectId: storageObjectId)));

      Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, exception.FailureClassification);
      Assert.DoesNotContain(rootPath, exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task ExistingWrongLengthFileIsDetected()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var expectedBytes = Encoding.UTF8.GetBytes("expected immutable bytes");
      var storageObjectId = StorageObjectId.New();
      WriteObjectFileDirectly(rootPath, storageObjectId, Encoding.UTF8.GetBytes("short"));

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(expectedBytes, storageObjectId: storageObjectId)));

      Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, exception.FailureClassification);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task ExistingWrongHashFileIsDetected()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var expectedBytes = Encoding.UTF8.GetBytes("same length A");
      var wrongBytes = Encoding.UTF8.GetBytes("same length B");
      var storageObjectId = StorageObjectId.New();
      WriteObjectFileDirectly(rootPath, storageObjectId, wrongBytes);

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(expectedBytes, storageObjectId: storageObjectId)));

      Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, exception.FailureClassification);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task LengthMismatchFailsAndCleansTemporaryFiles()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("length mismatch");

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(bytes, contentLength: bytes.Length + 1)));

      Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, exception.FailureClassification);
      AssertNoObjectFiles(rootPath);
      AssertNoTemporaryFiles(rootPath);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task HashMismatchFailsAndCleansTemporaryFiles()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var bytes = Encoding.UTF8.GetBytes("hash mismatch");

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(bytes, contentHash: new string('A', 64))));

      Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, exception.FailureClassification);
      AssertNoObjectFiles(rootPath);
      AssertNoTemporaryFiles(rootPath);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task MissingObjectReturnsReleasedMissingOutcome()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      Directory.CreateDirectory(rootPath);

      var result = await store.OpenReadAsync(StorageObjectId.New());

      Assert.Equal(StorageAvailabilityState.Missing, result.AvailabilityState);
      Assert.Equal(0, result.ContentLength);
      Assert.Equal(string.Empty, result.ContentHash);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task CancellationBeforeFinalizationLeavesNoFinalObjectAndCleansTemporaryFiles()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      using var cancellation = new CancellationTokenSource();
      var bytes = Encoding.UTF8.GetBytes(new string('x', 32768));
      await using var content = new CancellingReadStream(bytes, cancellation);
      var request = CreateStoreRequest(bytes, content: content);

      var exception = await Record.ExceptionAsync(async () =>
        await store.StoreAsync(request, cancellation.Token));
      Assert.IsAssignableFrom<OperationCanceledException>(exception);

      AssertNoObjectFiles(rootPath);
      AssertNoTemporaryFiles(rootPath);
      Directory.CreateDirectory(rootPath);
      var reopened = await store.OpenReadAsync(request.StorageObjectId);
      Assert.Equal(StorageAvailabilityState.Missing, reopened.AvailabilityState);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task RootCreationWhenEnabledCreatesTheConfiguredRoot()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath, createRootIfMissing: true);
      var bytes = Encoding.UTF8.GetBytes("create root");

      await store.StoreAsync(CreateStoreRequest(bytes));

      Assert.True(Directory.Exists(rootPath));
      Assert.True(Directory.Exists(Path.Combine(rootPath, "objects")));
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public void InvalidRootProducesClearFailure()
  {
    var rootPath = Path.Combine(Path.GetTempPath(), "spinbuster-tests", $"{Guid.NewGuid():N}.txt");
    Directory.CreateDirectory(Path.GetDirectoryName(rootPath)!);
    File.WriteAllText(rootPath, "not a directory");

    try
    {
      var exception = Assert.Throws<ImmutableContentStoreException>(() => CreateFileSystemStore(rootPath));
      Assert.Equal(ImmutableContentStoreFailureClassification.RootUnavailable, exception.FailureClassification);
      Assert.Contains("file instead of a directory", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      if (File.Exists(rootPath))
      {
        File.Delete(rootPath);
      }
    }
  }

  [Fact]
  public async Task RootBecomesUnavailableAfterConfigurationProducesClassifiedFailure()
  {
    var rootPath = CreateRootPath();

    try
    {
      Directory.CreateDirectory(rootPath);
      var store = CreateFileSystemStore(rootPath);
      Directory.Delete(rootPath, recursive: true);
      File.WriteAllText(rootPath, "root replaced by file");

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(Encoding.UTF8.GetBytes("payload"))));

      Assert.Equal(ImmutableContentStoreFailureClassification.RootUnavailable, exception.FailureClassification);
      Assert.DoesNotContain(rootPath, exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      if (Directory.Exists(rootPath))
      {
        DeleteDirectoryIfPresent(rootPath);
      }
      else if (File.Exists(rootPath))
      {
        File.Delete(rootPath);
      }
    }
  }

  [Fact]
  public async Task InventoryIsBoundedAndDoesNotRevealAbsolutePaths()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath, configure: options => options.MaxInventoryResults = 2);
      await store.StoreAsync(CreateStoreRequest(Encoding.UTF8.GetBytes("one"), storageObjectId: StorageObjectId.New()));
      await store.StoreAsync(CreateStoreRequest(Encoding.UTF8.GetBytes("two"), storageObjectId: StorageObjectId.New()));

      var items = await store.ListStoredObjectsAsync(2);

      Assert.Equal(2, items.Count);
      Assert.All(items, item =>
      {
        Assert.DoesNotContain(rootPath, item.ProviderRelativeObjectKey, StringComparison.OrdinalIgnoreCase);
        Assert.False(Path.IsPathRooted(item.ProviderRelativeObjectKey));
      });
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task InventoryRejectsMalformedObjectLayout()
  {
    var rootPath = CreateRootPath();

    try
    {
      Directory.CreateDirectory(Path.Combine(rootPath, "objects", "aa", "bb"));
      File.WriteAllText(Path.Combine(rootPath, "objects", "aa", "bb", "not-a-guid.blob"), "bad");
      var store = CreateFileSystemStore(rootPath);

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.ListStoredObjectsAsync(10));

      Assert.Equal(ImmutableContentStoreFailureClassification.IntegrityMismatch, exception.FailureClassification);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task InventoryKeepsOrphanObjectVisibleWithoutMutation()
  {
    var rootPath = CreateRootPath();

    try
    {
      var storageObjectId = StorageObjectId.New();
      var bytes = Encoding.UTF8.GetBytes("orphan bytes");
      WriteObjectFileDirectly(rootPath, storageObjectId, bytes);
      var before = Directory.EnumerateFiles(Path.Combine(rootPath, "objects"), "*", SearchOption.AllDirectories).ToArray();
      var store = CreateFileSystemStore(rootPath);

      var items = await store.ListStoredObjectsAsync(10);
      var after = Directory.EnumerateFiles(Path.Combine(rootPath, "objects"), "*", SearchOption.AllDirectories).ToArray();

      var item = Assert.Single(items);
      Assert.Equal(storageObjectId, item.StorageObjectId);
      Assert.Equal(bytes.Length, item.ContentLength);
      Assert.Equal(before, after);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  [Fact]
  public async Task ReparsePointEscapeIsRejectedWhenSupported()
  {
    var rootPath = CreateRootPath();
    var outsidePath = CreateRootPath();

    try
    {
      Directory.CreateDirectory(rootPath);
      Directory.CreateDirectory(outsidePath);
      var objectsLinkPath = Path.Combine(rootPath, "objects");

      try
      {
        Directory.CreateSymbolicLink(objectsLinkPath, outsidePath);
      }
      catch (Exception linkCreationException) when (
        linkCreationException is UnauthorizedAccessException
        || linkCreationException is IOException
        || linkCreationException is PlatformNotSupportedException)
      {
        return;
      }

      var store = CreateFileSystemStore(rootPath);
      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(Encoding.UTF8.GetBytes("payload"))));

      Assert.Equal(ImmutableContentStoreFailureClassification.AccessDenied, exception.FailureClassification);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
      DeleteDirectoryIfPresent(outsidePath);
    }
  }

  [Fact]
  public async Task PublicExceptionsDoNotExposeAbsolutePaths()
  {
    var rootPath = CreateRootPath();

    try
    {
      var store = CreateFileSystemStore(rootPath);
      var expectedBytes = Encoding.UTF8.GetBytes("same length A");
      var wrongBytes = Encoding.UTF8.GetBytes("same length B");
      var storageObjectId = StorageObjectId.New();
      WriteObjectFileDirectly(rootPath, storageObjectId, wrongBytes);

      var exception = await Assert.ThrowsAsync<ImmutableContentStoreException>(async () =>
        await store.StoreAsync(CreateStoreRequest(expectedBytes, storageObjectId: storageObjectId)));

      Assert.DoesNotContain(rootPath, exception.Message, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain(rootPath, exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      DeleteDirectoryIfPresent(rootPath);
    }
  }

  private static LocalFileSystemImmutableContentStore CreateFileSystemStore(
    string rootPath,
    bool createRootIfMissing = true,
    Action<LocalFileSystemImmutableContentStoreOptions>? configure = null)
  {
    var options = new LocalFileSystemImmutableContentStoreOptions
    {
      RootPath = rootPath,
      CreateRootIfMissing = createRootIfMissing,
    };
    configure?.Invoke(options);
    return new LocalFileSystemImmutableContentStore(options);
  }

  private static StoreImmutableContentRequest CreateStoreRequest(
    byte[] bytes,
    StorageObjectId? storageObjectId = null,
    string? immutableObjectKey = null,
    string? contentHash = null,
    long? contentLength = null,
    Stream? content = null)
  {
    var hash = contentHash ?? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    return new StoreImmutableContentRequest(
      storageObjectId ?? StorageObjectId.New(),
      "provider",
      immutableObjectKey ?? "ignored-by-filesystem-adapter",
      hash,
      "SHA-256",
      1,
      contentLength ?? bytes.Length,
      content ?? new MemoryStream(bytes, writable: false),
      new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero),
      null);
  }

  private static string GetSingleObjectKey(string rootPath)
  {
    var objectPath = Directory.EnumerateFiles(Path.Combine(rootPath, "objects"), "*.blob", SearchOption.AllDirectories).Single();
    return Path.GetRelativePath(rootPath, objectPath).Replace(Path.DirectorySeparatorChar, '/');
  }

  private static string GetExpectedObjectPath(string rootPath, StorageObjectId storageObjectId)
  {
    var normalizedId = storageObjectId.Value.ToString("N");
    return Path.Combine(rootPath, "objects", normalizedId[..2], normalizedId[2..4], $"{normalizedId}.blob");
  }

  private static void WriteObjectFileDirectly(string rootPath, StorageObjectId storageObjectId, byte[] bytes)
  {
    var objectPath = GetExpectedObjectPath(rootPath, storageObjectId);
    Directory.CreateDirectory(Path.GetDirectoryName(objectPath)!);
    File.WriteAllBytes(objectPath, bytes);
  }

  private static void AssertNoObjectFiles(string rootPath)
  {
    var objectRoot = Path.Combine(rootPath, "objects");
    if (Directory.Exists(objectRoot))
    {
      Assert.Empty(Directory.EnumerateFiles(objectRoot, "*", SearchOption.AllDirectories));
    }
  }

  private static void AssertNoTemporaryFiles(string rootPath)
  {
    var temporaryRoot = Path.Combine(rootPath, "_tmp");
    if (Directory.Exists(temporaryRoot))
    {
      Assert.Empty(Directory.EnumerateFiles(temporaryRoot, "*", SearchOption.AllDirectories));
    }
  }

  private static string CreateRootPath()
  {
    return Path.Combine(Path.GetTempPath(), "spinbuster-tests", Guid.NewGuid().ToString("N"));
  }

  private static void DeleteDirectoryIfPresent(string rootPath)
  {
    if (Directory.Exists(rootPath))
    {
      Directory.Delete(rootPath, recursive: true);
    }
  }

  private static async Task<Exception?> CaptureExceptionAsync(Func<Task> action)
  {
    try
    {
      await action();
      return null;
    }
    catch (Exception exception)
    {
      return exception;
    }
  }

  private sealed class CancellingReadStream : Stream
  {
    private readonly MemoryStream _inner;
    private readonly CancellationTokenSource _cancellation;
    private bool _cancelled;

    public CancellingReadStream(byte[] bytes, CancellationTokenSource cancellation)
    {
      _inner = new MemoryStream(bytes, writable: false);
      _cancellation = cancellation;
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanWrite => false;

    public override long Length => _inner.Length;

    public override long Position
    {
      get => _inner.Position;
      set => _inner.Position = value;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (_cancelled)
      {
        cancellationToken.ThrowIfCancellationRequested();
      }

      var bytesRead = await _inner.ReadAsync(buffer, cancellationToken);
      if (!_cancelled && bytesRead > 0)
      {
        _cancelled = true;
        _cancellation.Cancel();
      }

      return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return _inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _inner.Dispose();
        _cancellation.Dispose();
      }

      base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
      await _inner.DisposeAsync();
      _cancellation.Dispose();
      await base.DisposeAsync();
    }
  }

}
