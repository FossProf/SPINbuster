using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ReportFieldNoteSourceRecord
{
  public ReportId ReportId { get; set; }

  public FieldNoteId FieldNoteId { get; set; }
}
