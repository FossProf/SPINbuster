using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IReportRepository
{
  Task<Report?> GetByIdAsync(ReportId reportId, CancellationToken cancellationToken = default);

  Task AddAsync(Report report, CancellationToken cancellationToken = default);
}
