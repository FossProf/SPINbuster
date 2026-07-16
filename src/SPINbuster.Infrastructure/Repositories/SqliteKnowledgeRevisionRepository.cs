using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteKnowledgeRevisionRepository : IKnowledgeRevisionRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteKnowledgeRevisionRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<KnowledgeDocumentRevision?> GetByIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.KnowledgeDocumentRevisions
      .AsNoTracking()
      .SingleOrDefaultAsync(revision => revision.Id == knowledgeDocumentRevisionId, cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<KnowledgeDocumentRevision?> GetCurrentByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.KnowledgeDocumentRevisions
      .AsNoTracking()
      .SingleOrDefaultAsync(
        revision => revision.KnowledgeDocumentId == knowledgeDocumentId
          && revision.Lifecycle == KnowledgeRevisionLifecycle.CurrentAuthoritative,
        cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<IReadOnlyCollection<KnowledgeDocumentRevision>> GetByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.KnowledgeDocumentRevisions
      .AsNoTracking()
      .Where(revision => revision.KnowledgeDocumentId == knowledgeDocumentId)
      .ToArrayAsync(cancellationToken);

    return records
      .OrderBy(revision => revision.CreatedAtUtc)
      .ThenBy(revision => revision.Id)
      .Select(InfrastructureMapper.ToDomain)
      .ToArray();
  }

  public Task AddAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default)
  {
    _dbContext.KnowledgeDocumentRevisions.Add(InfrastructureMapper.ToRecord(knowledgeDocumentRevision));
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default)
  {
    var record = InfrastructureMapper.ToRecord(knowledgeDocumentRevision);
    var trackedRecord = _dbContext.KnowledgeDocumentRevisions.Local.SingleOrDefault(localRecord => localRecord.Id == knowledgeDocumentRevision.Id);
    if (trackedRecord is null)
    {
      _dbContext.KnowledgeDocumentRevisions.Update(record);
      return Task.CompletedTask;
    }

    _dbContext.Entry(trackedRecord).CurrentValues.SetValues(record);
    return Task.CompletedTask;
  }
}
