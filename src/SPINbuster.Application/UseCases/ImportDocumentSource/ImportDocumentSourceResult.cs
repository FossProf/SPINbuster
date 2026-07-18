using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.ImportDocumentSource;

public sealed record ImportDocumentSourceResult(
  DocumentImportSessionId ImportSessionId,
  ImportedSourceId ImportedSourceId,
  bool ReusedExistingProjectSource,
  bool SameContentExistsInAnotherProject,
  StorageObjectId StorageObjectId,
  string ContentHash,
  string HashAlgorithm,
  int HashAlgorithmVersion,
  long ContentLength,
  string? DetectedMediaType,
  IReadOnlyList<string> Warnings);
