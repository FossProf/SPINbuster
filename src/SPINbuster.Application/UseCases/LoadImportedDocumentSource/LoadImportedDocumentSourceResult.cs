using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadImportedDocumentSource;

public sealed record LoadImportedDocumentSourceResult(
  ImportedSourceId ImportedSourceId,
  DocumentImportSessionId ImportSessionId,
  ProjectId ProjectId,
  string OriginalFileName,
  string? DeclaredMediaType,
  string? DetectedMediaType,
  long ContentLength,
  string ContentHash,
  string HashAlgorithm,
  int HashAlgorithmVersion,
  StorageObjectId StorageObjectId,
  string StorageProviderKey,
  string ImmutableObjectKey,
  ImportedSourceOrigin SourceOrigin,
  ImportedDocumentSourceStatus Status);
