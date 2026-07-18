using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.BeginDocumentImportSession;

public sealed record BeginDocumentImportSessionResult(
  DocumentImportSessionId ImportSessionId,
  ProjectId ProjectId,
  DocumentImportSessionState State,
  DateTimeOffset StartedAtUtc);
