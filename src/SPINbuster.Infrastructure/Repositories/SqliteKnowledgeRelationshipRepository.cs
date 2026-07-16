using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteKnowledgeRelationshipRepository : IKnowledgeRelationshipRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteKnowledgeRelationshipRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<KnowledgeRelationship?> GetByIdAsync(
    KnowledgeRelationshipId knowledgeRelationshipId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.KnowledgeRelationships
      .AsNoTracking()
      .SingleOrDefaultAsync(relationship => relationship.Id == knowledgeRelationshipId, cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(nameof(KnowledgeRelationship), knowledgeRelationshipId.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public async Task<KnowledgeRelationship?> FindByEndpointsAsync(
    ProjectId projectId,
    KnowledgeSubjectReference source,
    KnowledgeSubjectReference target,
    KnowledgeRelationshipType relationshipType,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.KnowledgeRelationships
      .AsNoTracking()
      .SingleOrDefaultAsync(
        relationship => relationship.ProjectId == projectId
          && relationship.SourceKey == source.ToStableKey()
          && relationship.TargetKey == target.ToStableKey()
          && relationship.RelationshipType == relationshipType,
        cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(nameof(KnowledgeRelationship), record.Id.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public async Task<IReadOnlyCollection<KnowledgeRelationship>> GetBySubjectAsync(
    ProjectId projectId,
    KnowledgeSubjectReference subject,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.KnowledgeRelationships
      .AsNoTracking()
      .Where(relationship => relationship.ProjectId == projectId
        && (relationship.SourceKey == subject.ToStableKey()
            || relationship.TargetKey == subject.ToStableKey()))
      .ToArrayAsync(cancellationToken);

    var orderedRecords = records
      .OrderBy(relationship => relationship.CreatedAtUtc)
      .ThenBy(relationship => relationship.Id)
      .Take(maxResults)
      .ToArray();

    if (orderedRecords.Length == 0)
    {
      return [];
    }

    var auditTrailBySubjectId = await LoadAuditTrailMapAsync(
      nameof(KnowledgeRelationship),
      orderedRecords.Select(record => record.Id.ToString()).ToArray(),
      cancellationToken);

    return orderedRecords
      .Select(record => InfrastructureMapper.ToDomain(
        record,
        auditTrailBySubjectId.TryGetValue(record.Id.ToString(), out var auditTrail)
          ? auditTrail
          : []))
      .ToArray();
  }

  public Task AddAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default)
  {
    _dbContext.KnowledgeRelationships.Add(InfrastructureMapper.ToRecord(knowledgeRelationship));
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default)
  {
    var record = InfrastructureMapper.ToRecord(knowledgeRelationship);
    var trackedRecord = _dbContext.KnowledgeRelationships.Local.SingleOrDefault(localRecord => localRecord.Id == knowledgeRelationship.Id);
    if (trackedRecord is null)
    {
      _dbContext.KnowledgeRelationships.Update(record);
      return Task.CompletedTask;
    }

    _dbContext.Entry(trackedRecord).CurrentValues.SetValues(record);
    return Task.CompletedTask;
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
