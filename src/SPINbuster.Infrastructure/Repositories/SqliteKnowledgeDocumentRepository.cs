using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteKnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteKnowledgeDocumentRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<KnowledgeDocument?> GetByIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.KnowledgeDocuments
      .AsNoTracking()
      .Include(document => document.Revisions)
      .SingleOrDefaultAsync(document => document.Id == knowledgeDocumentId, cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(nameof(KnowledgeDocument), knowledgeDocumentId.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public async Task<IReadOnlyCollection<KnowledgeDocument>> GetByProjectAsync(
    ProjectId projectId,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.KnowledgeDocuments
      .AsNoTracking()
      .Include(document => document.Revisions)
      .Where(document => document.ProjectId == projectId)
      .OrderBy(document => document.CanonicalTitle)
      .ThenBy(document => document.Id)
      .ToArrayAsync(cancellationToken);

    if (records.Length == 0)
    {
      return [];
    }

    var auditTrailBySubjectId = await LoadAuditTrailMapAsync(
      nameof(KnowledgeDocument),
      records.Select(document => document.Id.ToString()).ToArray(),
      cancellationToken);

    return records
      .Select(record => InfrastructureMapper.ToDomain(
        record,
        auditTrailBySubjectId.TryGetValue(record.Id.ToString(), out var auditTrail)
          ? auditTrail
          : []))
      .ToArray();
  }

  public Task AddAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default)
  {
    _dbContext.KnowledgeDocuments.Add(InfrastructureMapper.ToRecord(knowledgeDocument));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.KnowledgeDocuments
      .SingleAsync(document => document.Id == knowledgeDocument.Id, cancellationToken);

    existing.CurrentAuthoritativeRevisionId = knowledgeDocument.CurrentAuthoritativeRevisionId;
    existing.Lifecycle = knowledgeDocument.Lifecycle;
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
      .ThenBy(auditEvent => auditEvent.Id.Value)
      .ToArray();
  }

  private async Task<Dictionary<string, AuditEvent[]>> LoadAuditTrailMapAsync(
    string subjectType,
    IReadOnlyCollection<string> subjectIds,
    CancellationToken cancellationToken)
  {
    var records = await _dbContext.AuditEvents
      .AsNoTracking()
      .Where(record => record.SubjectType == subjectType && subjectIds.Contains(record.SubjectId))
      .ToArrayAsync(cancellationToken);

    return records
      .Select(InfrastructureMapper.ToDomain)
      .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
      .ThenBy(auditEvent => auditEvent.Id.Value)
      .GroupBy(auditEvent => auditEvent.SubjectId, StringComparer.Ordinal)
      .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
  }
}
