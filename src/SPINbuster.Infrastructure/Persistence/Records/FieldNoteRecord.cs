using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class FieldNoteRecord
{
  public FieldNoteId Id { get; set; }

  public InspectionSessionId InspectionSessionId { get; set; }

  public string CapturedBy { get; set; } = string.Empty;

  public DateTimeOffset CapturedAtUtc { get; set; }

  public string RawText { get; set; } = string.Empty;
}
