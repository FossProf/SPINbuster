using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.ImportDocumentSource;

public sealed record ImportDocumentSourceCommand(
  DocumentImportSessionId ImportSessionId,
  ProjectId ProjectId,
  string OriginalFileName,
  string? DeclaredMediaType,
  ImportedSourceOrigin SourceOrigin,
  string? ExternalSourceReference,
  Stream Content) : ICommand<ImportDocumentSourceResult>;
