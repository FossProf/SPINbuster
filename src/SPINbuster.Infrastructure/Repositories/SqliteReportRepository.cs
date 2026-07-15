using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteReportRepository : IReportRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteReportRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<Report?> GetByIdAsync(ReportId reportId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.Reports
      .AsNoTracking()
      .SingleOrDefaultAsync(report => report.Id == reportId, cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditRecords = await _dbContext.AuditEvents
      .AsNoTracking()
      .Where(auditEvent => auditEvent.SubjectType == nameof(Report) && auditEvent.SubjectId == reportId.ToString())
      .ToArrayAsync(cancellationToken);

    return InfrastructureMapper.ToDomain(
      record,
      auditRecords
        .Select(InfrastructureMapper.ToDomain)
        .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
        .ThenBy(auditEvent => auditEvent.Id)
        .ToArray());
  }

  public Task AddAsync(Report report, CancellationToken cancellationToken = default)
  {
    _dbContext.Reports.Add(InfrastructureMapper.ToRecord(report));
    return Task.CompletedTask;
  }
}
