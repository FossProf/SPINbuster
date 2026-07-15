using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.GenerateReportDraftRequest;

public sealed record GenerateReportDraftRequestResult(
  ProjectId ProjectId,
  string ProjectName,
  InspectionSessionId InspectionSessionId,
  string InspectionSessionName,
  string DraftTitle,
  IReadOnlyCollection<ReportDraftFieldNote> FieldNotes,
  IReadOnlyCollection<ReportDraftEvidenceAttachment> EvidenceAttachments);

public sealed record ReportDraftFieldNote(
  FieldNoteId FieldNoteId,
  string RawText,
  string CapturedBy,
  DateTimeOffset CapturedAtUtc);

public sealed record ReportDraftEvidenceAttachment(
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
