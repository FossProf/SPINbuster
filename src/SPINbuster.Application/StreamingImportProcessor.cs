using System.Buffers;
using SPINbuster.Domain;

namespace SPINbuster.Application;

/// <summary>
/// Bounded input processor for document import.
/// Consumes the source stream incrementally with early size enforcement,
/// then retains the bytes entirely for replay by the import pipeline.
/// This is bounded buffering, not true streaming: the full content is held
/// in a pooled <see cref="MemoryStream"/> after the incremental pass completes.
/// </summary>
public sealed class StreamingImportProcessor
{
  private const int DefaultChunkSize = 81920;

  /// <summary>
  /// Reads from <paramref name="source"/> in bounded chunks, enforcing
  /// <paramref name="maxContentLengthBytes"/> during each read. Returns
  /// a positioned <see cref="ImportBuffer"/> ready for hash computation and storage.
  /// </summary>
  /// <exception cref="DocumentContentTooLargeException">
  /// Thrown when the cumulative byte count exceeds <paramref name="maxContentLengthBytes"/>.
  /// No bytes are persisted and no database state is committed.
  /// </exception>
  /// <exception cref="DomainInvariantException">
  /// Thrown when the source stream contains no bytes.
  /// </exception>
  public static async Task<ImportBuffer> ProcessAsync(
    Stream source,
    long maxContentLengthBytes,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(source);

    if (maxContentLengthBytes <= 0)
    {
      throw new ArgumentException("Maximum content length must be greater than zero.", nameof(maxContentLengthBytes));
    }

    var chunk = ArrayPool<byte>.Shared.Rent(DefaultChunkSize);
    try
    {
      return await ProcessCoreAsync(source, maxContentLengthBytes, chunk, cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(chunk);
    }
  }

  private static async Task<ImportBuffer> ProcessCoreAsync(
    Stream source,
    long maxContentLengthBytes,
    byte[] chunk,
    CancellationToken cancellationToken)
  {
    // Start with a small initial capacity to avoid over-allocating for tiny files.
    // The MemoryStream will grow naturally as chunks are appended.
    var buffer = new MemoryStream();
    long totalLength = 0;

    try
    {
      int bytesRead;
      while ((bytesRead = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
      {
        totalLength += bytesRead;
        if (totalLength > maxContentLengthBytes)
        {
          throw new DocumentContentTooLargeException(totalLength, maxContentLengthBytes);
        }

        await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
      }
    }
    catch
    {
      await buffer.DisposeAsync().ConfigureAwait(false);
      throw;
    }

    if (totalLength <= 0)
    {
      await buffer.DisposeAsync().ConfigureAwait(false);
      throw new DomainInvariantException("Imported content cannot be empty.");
    }

    // Extract the underlying buffer to avoid a full-size copy.
    // The MemoryStream was created without a byte[] source, so GetBuffer()
    // returns the internal array whose length equals totalLength.
    var rentedBuffer = buffer.GetBuffer();
    var rentedLength = (int)totalLength;

    // Transfer ownership of the rented buffer to ImportBuffer.
    // ImportBuffer assumes responsibility for returning it to the pool.
    var importBuffer = new ImportBuffer(buffer, totalLength, rentedBuffer, rentedLength);

    return importBuffer;
  }
}
