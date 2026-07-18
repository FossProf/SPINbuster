using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Services;

public sealed class SqliteUnitOfWork : IUnitOfWork
{
  private readonly SqliteAuditRecorder _auditRecorder;
  private readonly SpinbusterDbContext _dbContext;
  private readonly ILogger<SqliteUnitOfWork> _logger;
  private readonly IReadOnlyList<IDeferredReferenceHandler> _deferredReferenceHandlers;

  public SqliteUnitOfWork(
    SpinbusterDbContext dbContext,
    SqliteAuditRecorder auditRecorder,
    ILogger<SqliteUnitOfWork> logger,
    IEnumerable<IDeferredReferenceHandler> deferredReferenceHandlers)
  {
    _dbContext = dbContext;
    _auditRecorder = auditRecorder;
    _logger = logger;
    _deferredReferenceHandlers = deferredReferenceHandlers.ToArray();
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      var stagedAuditEvents = _auditRecorder.ReleaseStagedAuditEvents();
      _dbContext.AuditEvents.AddRange(stagedAuditEvents);

      var deferredReferences = CollectDeferredReferences();

      if (deferredReferences.Count > 0)
      {
        ClearDeferredReferences(deferredReferences);
      }

      await _dbContext.SaveChangesAsync(cancellationToken);

      if (deferredReferences.Count > 0)
      {
        RestoreDeferredReferences(deferredReferences);
        await _dbContext.SaveChangesAsync(cancellationToken);
      }

      await transaction.CommitAsync(cancellationToken);
      _logger.LogDebug("Transaction committed with {AuditEventCount} audit events", stagedAuditEvents.Count);
    }
    catch
    {
      await transaction.RollbackAsync(cancellationToken);
      _logger.LogWarning("Transaction rolled back");
      throw;
    }
  }

  private List<(EntityEntry Entry, IDeferredReferenceHandler Handler, DeferredReferenceInfo Reference)> CollectDeferredReferences()
  {
    var results = new List<(EntityEntry, IDeferredReferenceHandler, DeferredReferenceInfo)>();

    foreach (var entry in _dbContext.ChangeTracker.Entries())
    {
      foreach (var handler in _deferredReferenceHandlers)
      {
        if (handler.CanHandle(entry))
        {
          foreach (var reference in handler.Extract(entry))
          {
            results.Add((entry, handler, reference));
          }
        }
      }
    }

    return results;
  }

  private static void ClearDeferredReferences(
    List<(EntityEntry Entry, IDeferredReferenceHandler Handler, DeferredReferenceInfo Reference)> deferredReferences)
  {
    foreach (var (entry, handler, reference) in deferredReferences)
    {
      handler.Clear(entry, reference);
    }
  }

  private static void RestoreDeferredReferences(
    List<(EntityEntry Entry, IDeferredReferenceHandler Handler, DeferredReferenceInfo Reference)> deferredReferences)
  {
    foreach (var (entry, handler, reference) in deferredReferences)
    {
      handler.Restore(entry, reference);
    }
  }
}
