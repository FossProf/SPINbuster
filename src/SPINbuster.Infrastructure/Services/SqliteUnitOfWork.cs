using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

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

      // A brand-new knowledge document and its initial current revision form a
      // temporary insert cycle. We break that cycle inside the transaction,
      // then restore the authoritative pointer before commit.
      var deferredCurrentRevisionLinks = _dbContext.ChangeTracker
        .Entries<KnowledgeDocumentRecord>()
        .Where(entry => entry.State == EntityState.Added && entry.Entity.CurrentAuthoritativeRevisionId is not null)
        .Select(entry => new DeferredCurrentRevisionLink(
          entry,
          entry.Entity.CurrentAuthoritativeRevisionId!.Value))
        .ToArray();

      foreach (var deferredLink in deferredCurrentRevisionLinks)
      {
        deferredLink.DocumentEntry.Property(document => document.CurrentAuthoritativeRevisionId).CurrentValue = null;
      }

      await _dbContext.SaveChangesAsync(cancellationToken);

      if (deferredCurrentRevisionLinks.Length > 0)
      {
        foreach (var deferredLink in deferredCurrentRevisionLinks)
        {
          deferredLink.DocumentEntry.Property(document => document.CurrentAuthoritativeRevisionId).CurrentValue = deferredLink.CurrentAuthoritativeRevisionId;
          deferredLink.DocumentEntry.Property(document => document.CurrentAuthoritativeRevisionId).IsModified = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
      }

      await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
      await transaction.RollbackAsync(cancellationToken);
      throw;
    }
  }

  private sealed record DeferredCurrentRevisionLink(
    EntityEntry<KnowledgeDocumentRecord> DocumentEntry,
    KnowledgeDocumentRevisionId CurrentAuthoritativeRevisionId);
}
