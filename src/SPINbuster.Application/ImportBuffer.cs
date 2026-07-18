using SPINbuster.Domain;

namespace SPINbuster.Application;

/// <summary>
/// Bounded replay buffer produced by <see cref="StreamingImportProcessor"/>.
/// Owns a positioned <see cref="MemoryStream"/> containing the imported content.
/// The stream is rewound to position 0 and ready for hash computation or storage.
/// Disposal releases the underlying stream. No external buffer pool is involved.
/// </summary>
public sealed class ImportBuffer : IAsyncDisposable
{
  internal ImportBuffer(MemoryStream content, long contentLength)
  {
    Content = content ?? throw new ArgumentNullException(nameof(content));
    ContentLength = contentLength;
    Content.Position = 0;
  }

  public MemoryStream Content { get; }

  public long ContentLength { get; }

  public ValueTask DisposeAsync()
  {
    Content.Dispose();
    return ValueTask.CompletedTask;
  }
}
