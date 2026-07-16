using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ReportEvidenceSourceRecord
{
  public ReportId ReportId { get; set; }

  public EvidenceAttachmentId EvidenceAttachmentId { get; set; }
}
