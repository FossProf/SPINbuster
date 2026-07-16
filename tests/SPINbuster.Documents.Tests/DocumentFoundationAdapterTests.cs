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
  public async Task DeterministicDocumentProcessorProducesMetadataCandidate()
  {
    var processor = new DeterministicDocumentProcessor();
    await using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));

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
    Assert.Single(result.Candidates);
    Assert.Equal(DocumentCandidateType.MetadataCandidate, result.Candidates.Single().CandidateType);
  }
}
