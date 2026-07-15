namespace SPINbuster.Domain;

public sealed class FieldNote
{
  public FieldNote(
    FieldNoteId id,
    InspectionSessionId inspectionSessionId,
    string capturedBy,
    DateTimeOffset capturedAtUtc,
    FieldNoteRawText rawText)
  {
    Id = id;
    InspectionSessionId = inspectionSessionId;
    CapturedBy = DomainGuards.NotNullOrWhiteSpace(capturedBy, nameof(capturedBy));
    CapturedAtUtc = DomainGuards.NotDefault(capturedAtUtc, nameof(capturedAtUtc));
    RawText = rawText ?? throw new DomainInvariantException($"{nameof(rawText)} must be provided.");
  }

  public FieldNoteId Id { get; }

  public InspectionSessionId InspectionSessionId { get; }

  public string CapturedBy { get; }

  public DateTimeOffset CapturedAtUtc { get; }

  public FieldNoteRawText RawText { get; }
}
