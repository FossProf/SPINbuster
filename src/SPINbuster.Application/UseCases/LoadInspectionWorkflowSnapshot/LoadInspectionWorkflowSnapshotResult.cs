using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;

public sealed record LoadInspectionWorkflowSnapshotResult(
  PersistedProjectSnapshot Project,
  PersistedInspectionSessionSnapshot InspectionSession);

public sealed record PersistedProjectSnapshot(
  ProjectId ProjectId,
  string Name,
  ProjectLifecycle Lifecycle,
  IReadOnlyList<PersistedAuditEntry> AuditHistory);

public sealed record PersistedInspectionSessionSnapshot(
  InspectionSessionId InspectionSessionId,
  ProjectId ProjectId,
  string Name,
  InspectionSessionLifecycle Lifecycle,
  IReadOnlyList<PersistedFieldNote> FieldNotes,
  IReadOnlyList<PersistedAuditEntry> AuditHistory);

public sealed record PersistedFieldNote(
  FieldNoteId FieldNoteId,
  string RawText,
  DateTimeOffset CapturedAtUtc);

public sealed record PersistedAuditEntry(
  AuditEventId AuditEventId,
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
