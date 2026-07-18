using System.Buffers;
using SPINbuster.Domain;

namespace SPINbuster.Application;

/// <summary>
/// Bounded replay buffer produced by <see cref="StreamingImportProcessor"/>.
/// Owns a pooled byte rental and a positioned <see cref="MemoryStream"/> over it.
/// The stream is rewound to position 0 and ready for hash computation or storage.
/// Disposal returns the rented buffer to the shared pool.
/// </summary>
public sealed class ImportBuffer : IAsyncDisposable
{
  private byte[]? _rentedBuffer;
  private readonly int _rentedLength;

  internal ImportBuffer(MemoryStream content, long contentLength, byte[] rentedBuffer, int rentedLength)
  {
    Content = content ?? throw new ArgumentNullException(nameof(content));
    ContentLength = contentLength;
    _rentedBuffer = rentedBuffer;
    _rentedLength = rentedLength;
  }

  public MemoryStream Content { get; }

  public long ContentLength { get; }

  public ValueTask DisposeAsync()
  {
    var buffer = Interlocked.Exchange(ref _rentedBuffer, null);
    if (buffer is not null)
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }

    Content.Dispose();
    return ValueTask.CompletedTask;
  }
}
