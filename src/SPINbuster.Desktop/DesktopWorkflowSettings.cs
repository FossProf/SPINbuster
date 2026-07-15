namespace SPINbuster.Desktop;

public sealed record DesktopWorkflowSettings(
  string CurrentUserId,
  string ProjectName,
  string SessionName,
  string FieldNoteText,
  DateTimeOffset InitialTimestampUtc);
