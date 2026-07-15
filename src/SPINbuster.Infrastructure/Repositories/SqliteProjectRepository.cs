using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteProjectRepository : IProjectRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteProjectRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.Projects
      .AsNoTracking()
      .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(nameof(Project), projectId.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public Task AddAsync(Project project, CancellationToken cancellationToken = default)
  {
    _dbContext.Projects.Add(InfrastructureMapper.ToRecord(project));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.Projects
      .SingleAsync(record => record.Id == project.Id, cancellationToken);

    existing.Lifecycle = project.Lifecycle;
  }

  private async Task<IReadOnlyCollection<AuditEvent>> LoadAuditTrailAsync(
    string subjectType,
    string subjectId,
    CancellationToken cancellationToken)
  {
    var records = await _dbContext.AuditEvents
      .AsNoTracking()
      .Where(record => record.SubjectType == subjectType && record.SubjectId == subjectId)
      .ToArrayAsync(cancellationToken);

    return records
      .Select(InfrastructureMapper.ToDomain)
      .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
      .ThenBy(auditEvent => auditEvent.Id)
      .ToArray();
  }
}
