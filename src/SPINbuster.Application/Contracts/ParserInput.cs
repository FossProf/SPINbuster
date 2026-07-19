using SPINbuster.Domain;

namespace SPINbuster.Application.Contracts;

public sealed record ParserInput(
  ImportedSourceId ImportedSourceId,
  ProjectId ProjectId,
  string OriginalFileName,
  string? DeclaredMediaType,
  string? DetectedMediaType,
  string SourceContentHash,
  string HashAlgorithm,
  int HashAlgorithmVersion,
  long ContentLength,
  Stream Content);
