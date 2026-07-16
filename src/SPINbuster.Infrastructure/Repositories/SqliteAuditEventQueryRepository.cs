using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteAuditEventQueryRepository : IAuditEventQueryRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteAuditEventQueryRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<IReadOnlyCollection<AuditEvent>> GetBySubjectAsync(
    string subjectType,
    string subjectId,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.AuditEvents
      .AsNoTracking()
      .Where(record => record.SubjectType == subjectType && record.SubjectId == subjectId)
      .ToArrayAsync(cancellationToken);

    return records
      .OrderBy(record => record.OccurredAtUtc.UtcDateTime)
      .ThenBy(record => record.Id.ToString(), StringComparer.Ordinal)
      .Select(InfrastructureMapper.ToDomain)
      .ToArray();
  }
}
