using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteParserRunRepository : IParserRunRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteParserRunRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<ParserRun?> GetByIdAsync(ParserRunId parserRunId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.ParserRuns.AsNoTracking().SingleOrDefaultAsync(item => item.Id == parserRunId, cancellationToken);
    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(nameof(ParserRun), parserRunId.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public async Task<ParserRun?> GetBySourceAndParserAsync(
    ImportedSourceId importedSourceId,
    string parserKey,
    string parserVersion,
    string contractVersion,
    string contractHash,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.ParserRuns.AsNoTracking().SingleOrDefaultAsync(item =>
      item.ImportedSourceId == importedSourceId
      && item.ParserKey == parserKey
      && item.ParserVersion == parserVersion
      && item.ParserContractVersion == contractVersion
      && item.ParserContractHash == contractHash,
      cancellationToken);
    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(nameof(ParserRun), record.Id.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public async Task<IReadOnlyCollection<ParserRun>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.ParserRuns.AsNoTracking()
      .Where(item => item.ProjectId == projectId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.CreatedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var auditMap = await LoadAuditTrailMapAsync(nameof(ParserRun), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);
    return records
      .Select(record => InfrastructureMapper.ToDomain(record, auditMap.TryGetValue(record.Id.ToString(), out var trail) ? trail : []))
      .ToArray();
  }

  public async Task<IReadOnlyCollection<ParserRun>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.ParserRuns.AsNoTracking()
      .Where(item => item.ImportedSourceId == importedSourceId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.CreatedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var auditMap = await LoadAuditTrailMapAsync(nameof(ParserRun), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);
    return records
      .Select(record => InfrastructureMapper.ToDomain(record, auditMap.TryGetValue(record.Id.ToString(), out var trail) ? trail : []))
      .ToArray();
  }

  public Task AddAsync(ParserRun parserRun, CancellationToken cancellationToken = default)
  {
    _dbContext.ParserRuns.Add(InfrastructureMapper.ToRecord(parserRun));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(ParserRun parserRun, CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.ParserRuns.SingleAsync(item => item.Id == parserRun.Id, cancellationToken);
    existing.State = parserRun.State;
    existing.ExecutionStatus = parserRun.ExecutionStatus;
    existing.StartedAtUtc = parserRun.StartedAtUtc;
    existing.CompletedAtUtc = parserRun.CompletedAtUtc;
    existing.FailureReason = parserRun.FailureReason;
  }

  private async Task<IReadOnlyCollection<AuditEvent>> LoadAuditTrailAsync(string subjectType, string subjectId, CancellationToken cancellationToken)
  {
    var records = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == subjectType && item.SubjectId == subjectId)
      .ToArrayAsync(cancellationToken);
    return records.Select(InfrastructureMapper.ToDomain).OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id.Value).ToArray();
  }

  private async Task<Dictionary<string, AuditEvent[]>> LoadAuditTrailMapAsync(string subjectType, string[] subjectIds, CancellationToken cancellationToken)
  {
    var records = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == subjectType && subjectIds.Contains(item.SubjectId))
      .ToArrayAsync(cancellationToken);
    return records
      .Select(InfrastructureMapper.ToDomain)
      .GroupBy(item => item.SubjectId, StringComparer.Ordinal)
      .ToDictionary(group => group.Key, group => group.OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id.Value).ToArray(), StringComparer.Ordinal);
  }
}

public sealed class SqliteFragmentCandidateRepository : IFragmentCandidateRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteFragmentCandidateRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<FragmentCandidate?> GetByIdAsync(FragmentCandidateId fragmentCandidateId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.FragmentCandidates.AsNoTracking()
      .FirstOrDefaultAsync(item => item.Id == fragmentCandidateId, cancellationToken);
    if (record is null)
    {
      return null;
    }

    var auditMap = await LoadAuditTrailMapAsync(nameof(FragmentCandidate), [record.Id.ToString()], cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditMap.TryGetValue(record.Id.ToString(), out var trail) ? trail : []);
  }

  public async Task<IReadOnlyCollection<FragmentCandidate>> GetByParserRunAsync(
    ParserRunId parserRunId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.FragmentCandidates.AsNoTracking()
      .Where(item => item.ParserRunId == parserRunId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.Ordinal)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var auditMap = await LoadAuditTrailMapAsync(nameof(FragmentCandidate), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);
    return records
      .Select(record => InfrastructureMapper.ToDomain(record, auditMap.TryGetValue(record.Id.ToString(), out var trail) ? trail : []))
      .ToArray();
  }

  public async Task<IReadOnlyCollection<FragmentCandidate>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.FragmentCandidates.AsNoTracking()
      .Where(item => item.ImportedSourceId == importedSourceId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.CreatedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var auditMap = await LoadAuditTrailMapAsync(nameof(FragmentCandidate), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);
    return records
      .Select(record => InfrastructureMapper.ToDomain(record, auditMap.TryGetValue(record.Id.ToString(), out var trail) ? trail : []))
      .ToArray();
  }

  public Task AddAsync(FragmentCandidate fragmentCandidate, CancellationToken cancellationToken = default)
  {
    _dbContext.FragmentCandidates.Add(InfrastructureMapper.ToRecord(fragmentCandidate));
    return Task.CompletedTask;
  }

  public Task UpdateAsync(FragmentCandidate fragmentCandidate, CancellationToken cancellationToken = default)
  {
    var record = InfrastructureMapper.ToRecord(fragmentCandidate);
    var existingEntry = _dbContext.ChangeTracker
      .Entries<FragmentCandidateRecord>()
      .FirstOrDefault(e => e.Entity.Id == record.Id);
    if (existingEntry is not null)
    {
      existingEntry.State = EntityState.Detached;
    }

    _dbContext.FragmentCandidates.Update(record);
    return Task.CompletedTask;
  }

  public async Task<IReadOnlyCollection<FragmentCandidate>> GetByProjectFilteredAsync(
    ProjectId projectId,
    int maxResults,
    FragmentCandidateReviewState? reviewStateFilter,
    CancellationToken cancellationToken = default)
  {
    var query = _dbContext.FragmentCandidates.AsNoTracking()
      .Where(item => item.ProjectId == projectId);

    if (reviewStateFilter.HasValue)
    {
      query = query.Where(item => item.ReviewState == reviewStateFilter.Value);
    }

    var records = await query.ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.CreatedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var auditMap = await LoadAuditTrailMapAsync(nameof(FragmentCandidate), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);
    return records
      .Select(record => InfrastructureMapper.ToDomain(record, auditMap.TryGetValue(record.Id.ToString(), out var trail) ? trail : []))
      .ToArray();
  }

  public async Task<IReadOnlyCollection<FragmentCandidate>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.FragmentCandidates.AsNoTracking()
      .Where(item => item.ProjectId == projectId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.CreatedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var auditMap = await LoadAuditTrailMapAsync(nameof(FragmentCandidate), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);
    return records
      .Select(record => InfrastructureMapper.ToDomain(record, auditMap.TryGetValue(record.Id.ToString(), out var trail) ? trail : []))
      .ToArray();
  }

  private async Task<Dictionary<string, AuditEvent[]>> LoadAuditTrailMapAsync(string subjectType, string[] subjectIds, CancellationToken cancellationToken)
  {
    var records = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == subjectType && subjectIds.Contains(item.SubjectId))
      .ToArrayAsync(cancellationToken);
    return records
      .Select(InfrastructureMapper.ToDomain)
      .GroupBy(item => item.SubjectId, StringComparer.Ordinal)
      .ToDictionary(group => group.Key, group => group.OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id.Value).ToArray(), StringComparer.Ordinal);
  }
}
