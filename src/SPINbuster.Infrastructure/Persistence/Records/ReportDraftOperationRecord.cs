using SPINbuster.Application;
using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ReportDraftOperationRecord
{
  public OperationId OperationId { get; set; }

  public ReportId ReportId { get; set; }
}
