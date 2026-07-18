namespace SPINbuster.Domain;

public sealed class DocumentContentTooLargeException : DomainInvariantException
{
  public DocumentContentTooLargeException(long contentLength, long maxAllowedLength)
    : base($"Imported content length {contentLength} bytes exceeds the maximum allowed size of {maxAllowedLength} bytes.")
  {
    ContentLength = contentLength;
    MaxAllowedLength = maxAllowedLength;
  }

  public long ContentLength { get; }

  public long MaxAllowedLength { get; }
}
