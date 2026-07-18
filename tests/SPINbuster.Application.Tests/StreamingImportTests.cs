using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Domain;
using System.Buffers;
using System.Text;

namespace SPINbuster.Application.Tests;

public sealed class StreamingImportTests
{
  [Fact]
  public async Task NonSeekableStreamImportSucceeds()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var content = new NonSeekableStream(Encoding.UTF8.GetBytes("hello world"));

    var result = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      content));

    Assert.False(result.ReusedExistingProjectSource);
    Assert.Equal(11L, result.ContentLength);
    Assert.Single(fixture.ImportedSourceRepository.AddedSources);
    Assert.Single(fixture.StorageObjectRepository.AddedStorageObjects);
  }

  [Fact]
  public async Task SizeExceedDuringReadThrowsAndNoBytesPersistedAndNoStateCommitted()
  {
    var fixture = CreateFixture();
    fixture.ImportPolicy.MaxContentLengthBytes = 10;
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var largeContent = new MemoryStream(Encoding.UTF8.GetBytes("this content is definitely longer than ten bytes"));

    var exception = await Assert.ThrowsAsync<DocumentContentTooLargeException>(async () =>
      await useCase.HandleAsync(new ImportDocumentSourceCommand(
        importSession,
        fixture.ProjectId,
        "large.bin",
        "application/octet-stream",
        ImportedSourceOrigin.LocalFile,
        null,
        largeContent)));

    Assert.Equal(48L, exception.ContentLength);
    Assert.Equal(10L, exception.MaxAllowedLength);
    Assert.Empty(fixture.ImportedSourceRepository.AddedSources);
    Assert.Empty(fixture.StorageObjectRepository.AddedStorageObjects);
  }

  [Fact]
  public async Task ContentLengthMatchesOriginalBytes()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var expectedBytes = Encoding.UTF8.GetBytes("exact length verification");

    var result = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "verify.txt",
      "text/plain",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(expectedBytes)));

    Assert.Equal(expectedBytes.Length, result.ContentLength);
    Assert.Equal(expectedBytes.Length, fixture.StorageObjectRepository.AddedStorageObjects.Single().ContentLength);
  }

  [Fact]
  public async Task HashMatchesExpectedSha256()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var expectedBytes = Encoding.UTF8.GetBytes("hash verification content");
    var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(expectedBytes));

    var result = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "hash.txt",
      "text/plain",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(expectedBytes)));

    Assert.Equal(expectedHash, result.ContentHash);
    Assert.Equal(expectedHash, fixture.StorageObjectRepository.AddedStorageObjects.Single().ContentHash);
  }

  [Fact]
  public async Task MidReadFailureThrowsAndLeavesNoPartialState()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var failingStream = new FailingAfterReadStream(Encoding.UTF8.GetBytes("partial read content"), readLimit: 0);

    await Assert.ThrowsAsync<IOException>(async () =>
      await useCase.HandleAsync(new ImportDocumentSourceCommand(
        importSession,
        fixture.ProjectId,
        "failing.bin",
        "application/octet-stream",
        ImportedSourceOrigin.LocalFile,
        null,
        failingStream)));

    Assert.Empty(fixture.ImportedSourceRepository.AddedSources);
    Assert.Empty(fixture.StorageObjectRepository.AddedStorageObjects);
  }

  [Fact]
  public async Task CancellationDuringReadThrowsAndCleansUp()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var content = new MemoryStream(Encoding.UTF8.GetBytes("cancellation test"));
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
      await useCase.HandleAsync(new ImportDocumentSourceCommand(
        importSession,
        fixture.ProjectId,
        "cancel.txt",
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        content),
        cts.Token));

    Assert.Empty(fixture.ImportedSourceRepository.AddedSources);
    Assert.Empty(fixture.StorageObjectRepository.AddedStorageObjects);
  }

  [Fact]
  public async Task DuplicateDetectionUsesHashComputedFromBoundedReplayBuffer()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var content = Encoding.UTF8.GetBytes("duplicate detection content");
    var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));

    var first = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "spec-a.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(content)));

    var secondSession = await StartSessionAsync(fixture);
    var second = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      secondSession,
      fixture.ProjectId,
      "spec-b.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(content)));

    Assert.True(second.ReusedExistingProjectSource);
    Assert.Equal(first.ImportedSourceId, second.ImportedSourceId);
    Assert.Equal(expectedHash, first.ContentHash);
    Assert.Equal(expectedHash, second.ContentHash);
    Assert.Single(fixture.ImportedSourceRepository.AddedSources);
    Assert.Single(fixture.StorageObjectRepository.AddedStorageObjects);
  }

  [Fact]
  public async Task CommitFailureAfterStorageLeavesOrphanWithoutDbState()
  {
    var operationLog = new List<string>();
    var fixture = CreateFixture(operationLog);
    var importSession = await StartSessionAsync(fixture);
    fixture.UnitOfWork.ThrowOnCommit = true;
    var useCase = CreateImportUseCase(fixture);
    var content = Encoding.UTF8.GetBytes("orphan candidate content");

    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await useCase.HandleAsync(new ImportDocumentSourceCommand(
        importSession,
        fixture.ProjectId,
        "orphan.txt",
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        new MemoryStream(content))));

    Assert.Single(fixture.StorageObjectRepository.AddedStorageObjects);
    Assert.Single(fixture.ImportedSourceRepository.AddedSources);
    Assert.Contains("commit", operationLog);
  }

  [Fact]
  public async Task EmptyStreamThrowsDomainInvariant()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);

    await Assert.ThrowsAsync<DomainInvariantException>(async () =>
      await useCase.HandleAsync(new ImportDocumentSourceCommand(
        importSession,
        fixture.ProjectId,
        "empty.txt",
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        new MemoryStream(Array.Empty<byte>()))));

    Assert.Empty(fixture.ImportedSourceRepository.AddedSources);
    Assert.Empty(fixture.StorageObjectRepository.AddedStorageObjects);
  }

  [Fact]
  public async Task ExactHashAndLengthPassedToStore()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var contentBytes = Encoding.UTF8.GetBytes("exact hash and length verification");
    var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(contentBytes));

    var result = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "exact.bin",
      "application/octet-stream",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(contentBytes)));

    var storedObject = fixture.StorageObjectRepository.AddedStorageObjects.Single();
    Assert.Equal(expectedHash, storedObject.ContentHash);
    Assert.Equal(contentBytes.Length, storedObject.ContentLength);
    Assert.Equal("SHA-256", storedObject.HashAlgorithm);
    Assert.Equal(1, storedObject.HashAlgorithmVersion);
    Assert.Equal(result.ContentHash, storedObject.ContentHash);
    Assert.Equal(result.ContentLength, storedObject.ContentLength);
  }

  [Fact]
  public async Task MaxSizeBoundaryStreamSucceeds()
  {
    var fixture = CreateFixture();
    fixture.ImportPolicy.MaxContentLengthBytes = 11;
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var boundaryContent = Encoding.UTF8.GetBytes("12345678901");

    var result = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "boundary.txt",
      "text/plain",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(boundaryContent)));

    Assert.False(result.ReusedExistingProjectSource);
    Assert.Equal(11L, result.ContentLength);
    Assert.Single(fixture.ImportedSourceRepository.AddedSources);
  }

  private static ImportDocumentSourceUseCase CreateImportUseCase(DocumentFixture fixture)
  {
    return new ImportDocumentSourceUseCase(
      fixture.ImportSessionRepository,
      fixture.ImportedSourceRepository,
      fixture.StorageObjectRepository,
      fixture.ImmutableContentStore,
      fixture.ContentHashService,
      fixture.ContentInspector,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      NullLogger<ImportDocumentSourceUseCase>.Instance);
  }

  private static async Task<DocumentImportSessionId> StartSessionAsync(DocumentFixture fixture)
  {
    var result = await new BeginDocumentImportSessionUseCase(
      fixture.ImportSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new BeginDocumentImportSessionCommand(fixture.ProjectId));
    return result.ImportSessionId;
  }

  private static DocumentFixture CreateFixture(List<string>? operationLog = null)
  {
    return new DocumentFixture(
      ProjectId.New(),
      new FakeDocumentImportSessionRepository(),
      new FakeImportedDocumentSourceRepository(),
      new FakeStorageObjectRepository(),
      new FakeDocumentProcessingAttemptRepository(),
      new FakeDocumentCandidateRepository(),
      new FakeImmutableContentStore(),
      new FakeContentHashService(),
      new FakeImportedContentInspector(),
      new FakeDocumentImportPolicy(),
      new FakeDocumentProcessor(operationLog),
      new FakeUnitOfWork(operationLog),
      new FakeClock(new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero)),
      new FakeCurrentUser("streaming.reviewer@example.invalid"),
      new FakeAuditRecorder(operationLog));
  }

  private sealed record DocumentFixture(
    ProjectId ProjectId,
    FakeDocumentImportSessionRepository ImportSessionRepository,
    FakeImportedDocumentSourceRepository ImportedSourceRepository,
    FakeStorageObjectRepository StorageObjectRepository,
    FakeDocumentProcessingAttemptRepository ProcessingAttemptRepository,
    FakeDocumentCandidateRepository CandidateRepository,
    FakeImmutableContentStore ImmutableContentStore,
    FakeContentHashService ContentHashService,
    FakeImportedContentInspector ContentInspector,
    FakeDocumentImportPolicy ImportPolicy,
    FakeDocumentProcessor DocumentProcessor,
    FakeUnitOfWork UnitOfWork,
    FakeClock Clock,
    FakeCurrentUser CurrentUser,
    FakeAuditRecorder AuditRecorder);

  /// <summary>
  /// A stream that does not support seeking, simulating real file/network input.
  /// </summary>
  private sealed class NonSeekableStream : Stream
  {
    private readonly MemoryStream _inner;
    private bool _disposed;

    public NonSeekableStream(byte[] data)
    {
      _inner = new MemoryStream(data);
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        _disposed = true;
        _inner.Dispose();
      }
      base.Dispose(disposing);
    }
  }

  /// <summary>
  /// A stream that throws IOException after a configurable number of successful reads.
  /// </summary>
  private sealed class FailingAfterReadStream : Stream
  {
    private readonly MemoryStream _inner;
    private int _readCount;
    private readonly int _readLimit;
    private bool _disposed;

    public FailingAfterReadStream(byte[] data, int readLimit)
    {
      _inner = new MemoryStream(data);
      _readLimit = readLimit;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
      _readCount++;
      if (_readCount > _readLimit)
      {
        throw new IOException("Simulated mid-read failure.");
      }

      return _inner.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
      _readCount++;
      if (_readCount > _readLimit)
      {
        throw new IOException("Simulated mid-read failure.");
      }

      return await _inner.ReadAsync(buffer, cancellationToken);
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        _disposed = true;
        _inner.Dispose();
      }
      base.Dispose(disposing);
    }
  }
}
