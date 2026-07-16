using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadReportDraftSnapshot;

public sealed record LoadReportDraftSnapshotResult(
  ReportId ReportId,
  string Title,
  int RevisionNumber,
  ReportLifecycle Lifecycle,
  ProjectId ProjectId,
  string ProjectName,
  InspectionSessionId InspectionSessionId,
  string InspectionSessionName,
  IReadOnlyCollection<ReportDraftSectionSnapshot> Sections,
  IReadOnlyCollection<ReportDraftFieldNoteSourceSnapshot> FieldNotes,
  IReadOnlyCollection<ReportDraftEvidenceSourceSnapshot> EvidenceAttachments,
  IReadOnlyCollection<ReportDraftAuditEntrySnapshot> AuditHistory);

public sealed record ReportDraftSectionSnapshot(string Heading, string Content);

public sealed record ReportDraftFieldNoteSourceSnapshot(
  FieldNoteId FieldNoteId,
  string RawText,
  string CapturedBy,
  DateTimeOffset CapturedAtUtc);

public sealed record ReportDraftEvidenceSourceSnapshot(
  EvidenceAttachmentId EvidenceAttachmentId,
  string FileName,
  string MediaType,
  string StorageKey,
  string Checksum,
  string CapturedBy,
  DateTimeOffset CapturedAtUtc,
  string? InterpretationSummary,
  string? InterpretedBy,
  DateTimeOffset? InterpretedAtUtc);

public sealed record ReportDraftAuditEntrySnapshot(
  AuditEventId AuditEventId,
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
