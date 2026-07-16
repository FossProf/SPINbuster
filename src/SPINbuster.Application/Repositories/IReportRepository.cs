using SPINbuster.Application;
using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IReportRepository
{
  Task<Report?> GetByIdAsync(ReportId reportId, CancellationToken cancellationToken = default);

  Task<Report?> GetByOperationIdAsync(OperationId operationId, CancellationToken cancellationToken = default);

  Task AddAsync(Report report, OperationId operationId, CancellationToken cancellationToken = default);
}
