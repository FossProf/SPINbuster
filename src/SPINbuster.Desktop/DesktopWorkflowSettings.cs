namespace SPINbuster.Desktop;

public sealed record DesktopWorkflowSettings(
  string CurrentUserId,
  string ProjectName,
  string SessionName,
  string FieldNoteText,
  string EvidenceFileName,
  string EvidenceMediaType,
  string EvidenceStorageKey,
  string EvidenceChecksum,
  string InterpretationSummary,
  string DraftTitle,
  string DraftSummaryHeading,
  string DraftSummaryContent,
  string DraftObservationHeading,
  string DraftObservationContent,
  Guid ReportOperationId,
  DateTimeOffset InitialTimestampUtc);
