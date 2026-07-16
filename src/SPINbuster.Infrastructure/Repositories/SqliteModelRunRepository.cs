using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteModelRunRepository : IModelRunRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteModelRunRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<ModelRun?> GetByIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.ModelRuns
      .AsNoTracking()
      .SingleOrDefaultAsync(modelRun => modelRun.Id == modelRunId, cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<ModelRun?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.ModelRuns
      .AsNoTracking()
      .SingleOrDefaultAsync(modelRun => modelRun.CorrelationId == correlationId, cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<IReadOnlyCollection<ModelRunAttempt>> GetAttemptsAsync(
    ModelRunId modelRunId,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.ModelRunAttempts
      .AsNoTracking()
      .Where(attempt => attempt.ModelRunId == modelRunId)
      .OrderBy(attempt => attempt.AttemptNumber)
      .ToArrayAsync(cancellationToken);

    return records.Select(InfrastructureMapper.ToDomain).ToArray();
  }

  public Task AddAsync(ModelRun modelRun, CancellationToken cancellationToken = default)
  {
    _dbContext.ModelRuns.Add(InfrastructureMapper.ToRecord(modelRun));
    return Task.CompletedTask;
  }

  public Task UpdateAsync(ModelRun modelRun, CancellationToken cancellationToken = default)
  {
    var record = InfrastructureMapper.ToRecord(modelRun);
    var trackedRecord = _dbContext.ModelRuns.Local.SingleOrDefault(localRecord => localRecord.Id == modelRun.Id);
    if (trackedRecord is null)
    {
      _dbContext.ModelRuns.Update(record);
      return Task.CompletedTask;
    }

    _dbContext.Entry(trackedRecord).CurrentValues.SetValues(record);
    return Task.CompletedTask;
  }

  public Task AddAttemptAsync(ModelRunAttempt attempt, CancellationToken cancellationToken = default)
  {
    _dbContext.ModelRunAttempts.Add(InfrastructureMapper.ToRecord(attempt));
    return Task.CompletedTask;
  }
}
