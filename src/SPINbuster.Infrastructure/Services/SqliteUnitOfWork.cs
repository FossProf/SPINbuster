using Microsoft.EntityFrameworkCore.Storage;
using SPINbuster.Application.Abstractions;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Services;

public sealed class SqliteUnitOfWork : IUnitOfWork
{
  private readonly SqliteAuditRecorder _auditRecorder;
  private readonly SpinbusterDbContext _dbContext;

  public SqliteUnitOfWork(SpinbusterDbContext dbContext, SqliteAuditRecorder auditRecorder)
  {
    _dbContext = dbContext;
    _auditRecorder = auditRecorder;
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      var stagedAuditEvents = _auditRecorder.ReleaseStagedAuditEvents();
      _dbContext.AuditEvents.AddRange(stagedAuditEvents);

      await _dbContext.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
      await transaction.RollbackAsync(cancellationToken);
      throw;
    }
  }
}
