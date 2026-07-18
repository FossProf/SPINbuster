using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentImportSession;

public sealed record LoadDocumentImportSessionResult(
  DocumentImportSessionId ImportSessionId,
  ProjectId ProjectId,
  string InitiatedBy,
  DateTimeOffset StartedAtUtc,
  DateTimeOffset? CompletedAtUtc,
  DocumentImportSessionState State,
  int SourceCount,
  int AcceptedCount,
  int DuplicateCount,
  int RejectedCount,
  string? FailureSummary);
