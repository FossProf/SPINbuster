namespace SPINbuster.Domain;

public sealed record RawEvidenceReference
{
  public RawEvidenceReference(
    string fileName,
    string mediaType,
    string storageKey,
    string checksum)
  {
    FileName = DomainGuards.NotNullOrWhiteSpace(fileName, nameof(fileName));
    MediaType = DomainGuards.NotNullOrWhiteSpace(mediaType, nameof(mediaType));
    StorageKey = DomainGuards.NotNullOrWhiteSpace(storageKey, nameof(storageKey));
    Checksum = DomainGuards.NotNullOrWhiteSpace(checksum, nameof(checksum));
  }

  public string FileName { get; }

  public string MediaType { get; }

  public string StorageKey { get; }

  public string Checksum { get; }
}
