using SPINbuster.Application.UseCases.AcceptFragmentCandidate;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadFragmentReviewSnapshot;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.RejectFragmentCandidate;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed record ParsingExecutableWorkflowResult(
  CreateProjectResult CreatedProject,
  BeginDocumentImportSessionResult ImportSession,
  ImportDocumentSourceResult ImportedSourceA,
  ImportDocumentSourceResult ImportedSourceB,
  CompleteDocumentImportSessionResult CompletedImportSession,
  RequestDocumentParsingResult FirstParseResult,
  LoadParsingSnapshotResult FirstSnapshot,
  RequestDocumentParsingResult ReplayParseResult,
  LoadParsingSnapshotResult ReplaySnapshot,
  RequestDocumentParsingResult SourceBParseResult,
  RequestDocumentParsingResult UnsupportedMediaResult,
  RequestDocumentParsingResult CancelledParseResult,
  RequestDocumentParsingResult MalformedOutputResult,
  AcceptFragmentCandidateResult AcceptedCandidate,
  RejectFragmentCandidateResult RejectedCandidate,
  LoadFragmentReviewSnapshotResult ReviewSnapshotAfterAccept,
  LoadFragmentReviewSnapshotResult ReviewSnapshotAfterReject,
  LoadParsingSnapshotResult FinalSnapshot,
  IReadOnlyList<DesktopWorkflowFailurePresentation> FailurePresentations);
