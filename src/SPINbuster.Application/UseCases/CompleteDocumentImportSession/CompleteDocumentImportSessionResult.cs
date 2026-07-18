using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CompleteDocumentImportSession;

public sealed record CompleteDocumentImportSessionResult(
  DocumentImportSessionId ImportSessionId,
  ProjectId ProjectId,
  DocumentImportSessionState State,
  DateTimeOffset? CompletedAtUtc,
  int SourceCount,
  int AcceptedCount,
  int DuplicateCount,
  int RejectedCount);
