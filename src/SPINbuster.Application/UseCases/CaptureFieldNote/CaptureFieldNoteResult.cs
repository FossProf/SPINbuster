using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CaptureFieldNote;

public sealed record CaptureFieldNoteResult(
  FieldNoteId FieldNoteId,
  InspectionSessionId InspectionSessionId,
  DateTimeOffset CapturedAtUtc);
