using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed record ParsingExecutableWorkflowResult(
  CreateProjectResult CreatedProject,
  BeginDocumentImportSessionResult ImportSession,
  ImportDocumentSourceResult ImportedSource,
  CompleteDocumentImportSessionResult CompletedImportSession,
  RequestDocumentParsingResult FirstParseResult,
  LoadParsingSnapshotResult FirstSnapshot,
  RequestDocumentParsingResult ReplayParseResult,
  LoadParsingSnapshotResult ReplaySnapshot,
  RequestDocumentParsingResult UnsupportedMediaResult,
  RequestDocumentParsingResult CancelledParseResult,
  RequestDocumentParsingResult MalformedOutputResult,
  LoadParsingSnapshotResult FinalSnapshot);

public sealed record ParsingWorkflowFailurePresentation(
  string Scenario,
  string ExceptionType,
  string Message);
