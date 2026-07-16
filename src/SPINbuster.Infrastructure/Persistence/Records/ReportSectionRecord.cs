using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ReportSectionRecord
{
  public ReportId ReportId { get; set; }

  public int Position { get; set; }

  public string Heading { get; set; } = string.Empty;

  public string Content { get; set; } = string.Empty;
}
