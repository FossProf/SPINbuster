using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteSaveTransactionRepository : ISaveTransactionRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteSaveTransactionRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<SaveTransaction?> GetByIdAsync(
    SaveTransactionId saveTransactionId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.SaveTransactions
      .AsNoTracking()
      .SingleOrDefaultAsync(saveTransaction => saveTransaction.Id == saveTransactionId, cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditRecords = await _dbContext.AuditEvents
      .AsNoTracking()
      .Where(auditEvent => auditEvent.SubjectType == nameof(SaveTransaction) && auditEvent.SubjectId == saveTransactionId.ToString())
      .ToArrayAsync(cancellationToken);

    return InfrastructureMapper.ToDomain(
      record,
      auditRecords
        .Select(InfrastructureMapper.ToDomain)
        .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
        .ThenBy(auditEvent => auditEvent.Id)
        .ToArray());
  }

  public Task AddAsync(
    SaveTransaction saveTransaction,
    CancellationToken cancellationToken = default)
  {
    _dbContext.SaveTransactions.Add(InfrastructureMapper.ToRecord(saveTransaction));
    return Task.CompletedTask;
  }
}
