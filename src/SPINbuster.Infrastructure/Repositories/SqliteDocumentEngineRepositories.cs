using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteStorageObjectRepository : IStorageObjectRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteStorageObjectRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<StorageObject?> GetByIdAsync(StorageObjectId storageObjectId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.StorageObjects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == storageObjectId, cancellationToken);
    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<StorageObject?> GetByContentHashAsync(
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.StorageObjects
      .AsNoTracking()
      .SingleOrDefaultAsync(item =>
        item.ContentHash == contentHash
        && item.HashAlgorithm == hashAlgorithm
        && item.HashAlgorithmVersion == hashAlgorithmVersion,
        cancellationToken);
    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public Task AddAsync(StorageObject storageObject, CancellationToken cancellationToken = default)
  {
    _dbContext.StorageObjects.Add(InfrastructureMapper.ToRecord(storageObject));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(StorageObject storageObject, CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.StorageObjects.SingleAsync(item => item.Id == storageObject.Id, cancellationToken);
    existing.AvailabilityState = storageObject.AvailabilityState;
  }
}

public sealed class SqliteImportedDocumentSourceRepository : IImportedDocumentSourceRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteImportedDocumentSourceRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<ImportedDocumentSource?> GetByIdAsync(ImportedSourceId importedSourceId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.ImportedDocumentSources.AsNoTracking().SingleOrDefaultAsync(item => item.Id == importedSourceId, cancellationToken);
    if (record is null)
    {
      return null;
    }

    var storage = await _dbContext.StorageObjects.AsNoTracking().SingleAsync(item => item.Id == record.StorageObjectId, cancellationToken);
    var auditTrail = await LoadAuditTrailAsync(nameof(ImportedDocumentSource), importedSourceId.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, storage, auditTrail);
  }

  public async Task<IReadOnlyCollection<ImportedDocumentSource>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.ImportedDocumentSources
      .AsNoTracking()
      .Where(item => item.ProjectId == projectId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.ImportedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var storageIds = records.Select(record => record.StorageObjectId).Distinct().ToArray();
    var storageById = await _dbContext.StorageObjects
      .AsNoTracking()
      .Where(item => storageIds.Contains(item.Id))
      .ToDictionaryAsync(item => item.Id, cancellationToken);
    var auditMap = await LoadAuditTrailMapAsync(nameof(ImportedDocumentSource), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);

    return records
      .Select(record => InfrastructureMapper.ToDomain(
        record,
        storageById[record.StorageObjectId],
        auditMap.TryGetValue(record.Id.ToString(), out var auditTrail) ? auditTrail : []))
      .ToArray();
  }

  public async Task<ImportedDocumentSource?> GetByProjectAndContentHashAsync(
    ProjectId projectId,
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.ImportedDocumentSources
      .AsNoTracking()
      .SingleOrDefaultAsync(item =>
        item.ProjectId == projectId
        && item.ContentHash == contentHash
        && item.HashAlgorithm == hashAlgorithm
        && item.HashAlgorithmVersion == hashAlgorithmVersion,
        cancellationToken);
    if (record is null)
    {
      return null;
    }

    var storage = await _dbContext.StorageObjects.AsNoTracking().SingleAsync(item => item.Id == record.StorageObjectId, cancellationToken);
    var auditTrail = await LoadAuditTrailAsync(nameof(ImportedDocumentSource), record.Id.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, storage, auditTrail);
  }

  public Task<bool> ExistsInOtherProjectsAsync(
    ProjectId projectId,
    string contentHash,
    string hashAlgorithm,
    int hashAlgorithmVersion,
    CancellationToken cancellationToken = default)
  {
    return _dbContext.ImportedDocumentSources.AsNoTracking().AnyAsync(item =>
      item.ProjectId != projectId
      && item.ContentHash == contentHash
      && item.HashAlgorithm == hashAlgorithm
      && item.HashAlgorithmVersion == hashAlgorithmVersion,
      cancellationToken);
  }

  public async Task<IReadOnlyCollection<ImportedDocumentSource>> GetByImportSessionAsync(
    DocumentImportSessionId importSessionId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.ImportedDocumentSources
      .AsNoTracking()
      .Where(item => item.ImportSessionId == importSessionId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.ImportedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var storageById = await _dbContext.StorageObjects
      .AsNoTracking()
      .Where(item => records.Select(record => record.StorageObjectId).Contains(item.Id))
      .ToDictionaryAsync(item => item.Id, cancellationToken);
    var auditMap = await LoadAuditTrailMapAsync(nameof(ImportedDocumentSource), records.Select(item => item.Id.ToString()).ToArray(), cancellationToken);

    return records
      .Select(record => InfrastructureMapper.ToDomain(
        record,
        storageById[record.StorageObjectId],
        auditMap.TryGetValue(record.Id.ToString(), out var auditTrail) ? auditTrail : []))
      .ToArray();
  }

  public Task AddAsync(ImportedDocumentSource importedDocumentSource, CancellationToken cancellationToken = default)
  {
    _dbContext.ImportedDocumentSources.Add(InfrastructureMapper.ToRecord(importedDocumentSource));
    return Task.CompletedTask;
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

public sealed class SqliteDocumentImportSessionRepository : IDocumentImportSessionRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteDocumentImportSessionRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<DocumentImportSession?> GetByIdAsync(DocumentImportSessionId importSessionId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.DocumentImportSessions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == importSessionId, cancellationToken);
    if (record is null)
    {
      return null;
    }

    var auditTrail = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == nameof(DocumentImportSession) && item.SubjectId == importSessionId.ToString())
      .ToArrayAsync(cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail.Select(InfrastructureMapper.ToDomain).ToArray());
  }

  public async Task<IReadOnlyCollection<DocumentImportSession>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.DocumentImportSessions.AsNoTracking()
      .Where(item => item.ProjectId == projectId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.StartedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();

    if (records.Length == 0)
    {
      return [];
    }

    var auditMap = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == nameof(DocumentImportSession) && records.Select(record => record.Id.ToString()).Contains(item.SubjectId))
      .ToArrayAsync(cancellationToken);

    return records
      .Select(record => InfrastructureMapper.ToDomain(
        record,
        auditMap
          .Where(item => item.SubjectId == record.Id.ToString())
          .Select(InfrastructureMapper.ToDomain)
          .OrderBy(item => item.OccurredAtUtc)
          .ThenBy(item => item.Id.Value)
          .ToArray()))
      .ToArray();
  }

  public Task AddAsync(DocumentImportSession importSession, CancellationToken cancellationToken = default)
  {
    _dbContext.DocumentImportSessions.Add(InfrastructureMapper.ToRecord(importSession));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(DocumentImportSession importSession, CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.DocumentImportSessions.SingleAsync(item => item.Id == importSession.Id, cancellationToken);
    existing.CompletedAtUtc = importSession.CompletedAtUtc;
    existing.State = importSession.State;
    existing.SourceCount = importSession.SourceCount;
    existing.AcceptedCount = importSession.AcceptedCount;
    existing.DuplicateCount = importSession.DuplicateCount;
    existing.RejectedCount = importSession.RejectedCount;
    existing.FailureSummary = importSession.FailureSummary;
  }
}

public sealed class SqliteDocumentProcessingAttemptRepository : IDocumentProcessingAttemptRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteDocumentProcessingAttemptRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<DocumentProcessingAttempt?> GetByIdAsync(DocumentProcessingAttemptId processingAttemptId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.DocumentProcessingAttempts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == processingAttemptId, cancellationToken);
    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(processingAttemptId, cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public async Task<int> GetNextAttemptNumberAsync(ImportedSourceId importedSourceId, CancellationToken cancellationToken = default)
  {
    var current = await _dbContext.DocumentProcessingAttempts.AsNoTracking()
      .Where(item => item.ImportedSourceId == importedSourceId)
      .Select(item => (int?)item.AttemptNumber)
      .MaxAsync(cancellationToken);
    return (current ?? 0) + 1;
  }

  public async Task<IReadOnlyCollection<DocumentProcessingAttempt>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.DocumentProcessingAttempts.AsNoTracking()
      .Where(item => item.ImportedSourceId == importedSourceId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.AttemptNumber)
      .Take(maxResults)
      .ToArray();
    var auditMap = await LoadAuditTrailMapAsync(records.Select(item => item.Id).ToArray(), cancellationToken);
    return records.Select(item => InfrastructureMapper.ToDomain(item, auditMap.TryGetValue(item.Id, out var auditTrail) ? auditTrail : [])).ToArray();
  }

  public Task AddAsync(DocumentProcessingAttempt processingAttempt, CancellationToken cancellationToken = default)
  {
    _dbContext.DocumentProcessingAttempts.Add(InfrastructureMapper.ToRecord(processingAttempt));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(DocumentProcessingAttempt processingAttempt, CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.DocumentProcessingAttempts.SingleAsync(item => item.Id == processingAttempt.Id, cancellationToken);
    existing.StartedAtUtc = processingAttempt.StartedAtUtc;
    existing.CompletedAtUtc = processingAttempt.CompletedAtUtc;
    existing.State = processingAttempt.State;
    existing.FailureClassification = processingAttempt.FailureClassification;
    existing.FailureDetails = processingAttempt.FailureDetails;
    existing.OutputHash = processingAttempt.OutputHash;
  }

  private async Task<IReadOnlyCollection<AuditEvent>> LoadAuditTrailAsync(DocumentProcessingAttemptId id, CancellationToken cancellationToken)
  {
    var records = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == nameof(DocumentProcessingAttempt) && item.SubjectId == id.ToString())
      .ToArrayAsync(cancellationToken);
    return records.Select(InfrastructureMapper.ToDomain).OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id.Value).ToArray();
  }

  private async Task<Dictionary<DocumentProcessingAttemptId, AuditEvent[]>> LoadAuditTrailMapAsync(DocumentProcessingAttemptId[] ids, CancellationToken cancellationToken)
  {
    var subjectIds = ids.Select(item => item.ToString()).ToArray();
    var records = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == nameof(DocumentProcessingAttempt) && subjectIds.Contains(item.SubjectId))
      .ToArrayAsync(cancellationToken);
    return records
      .Select(InfrastructureMapper.ToDomain)
      .GroupBy(item => new DocumentProcessingAttemptId(Guid.Parse(item.SubjectId)))
      .ToDictionary(group => group.Key, group => group.OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id.Value).ToArray());
  }
}

public sealed class SqliteDocumentCandidateRepository : IDocumentCandidateRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteDocumentCandidateRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<DocumentCandidate?> GetByIdAsync(DocumentCandidateId documentCandidateId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.DocumentCandidates.AsNoTracking().SingleOrDefaultAsync(item => item.Id == documentCandidateId, cancellationToken);
    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(documentCandidateId, cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public async Task<IReadOnlyCollection<DocumentCandidate>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.DocumentCandidates.AsNoTracking()
      .Where(item => item.ImportedSourceId == importedSourceId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.CreatedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();
    var auditMap = await LoadAuditTrailMapAsync(records.Select(item => item.Id).ToArray(), cancellationToken);
    return records.Select(item => InfrastructureMapper.ToDomain(item, auditMap.TryGetValue(item.Id, out var auditTrail) ? auditTrail : [])).ToArray();
  }

  public async Task<IReadOnlyCollection<DocumentCandidate>> GetByProcessingAttemptAsync(
    DocumentProcessingAttemptId processingAttemptId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.DocumentCandidates.AsNoTracking()
      .Where(item => item.ProcessingAttemptId == processingAttemptId)
      .ToArrayAsync(cancellationToken);
    records = records
      .OrderBy(item => item.CreatedAtUtc)
      .ThenBy(item => item.Id)
      .Take(maxResults)
      .ToArray();
    var auditMap = await LoadAuditTrailMapAsync(records.Select(item => item.Id).ToArray(), cancellationToken);
    return records.Select(item => InfrastructureMapper.ToDomain(item, auditMap.TryGetValue(item.Id, out var auditTrail) ? auditTrail : [])).ToArray();
  }

  public Task AddAsync(DocumentCandidate documentCandidate, CancellationToken cancellationToken = default)
  {
    _dbContext.DocumentCandidates.Add(InfrastructureMapper.ToRecord(documentCandidate));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(DocumentCandidate documentCandidate, CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.DocumentCandidates.SingleAsync(item => item.Id == documentCandidate.Id, cancellationToken);
    existing.Status = documentCandidate.Status;
    existing.ReviewedBy = documentCandidate.ReviewedBy;
    existing.ReviewedAtUtc = documentCandidate.ReviewedAtUtc;
    existing.ReviewNotes = documentCandidate.ReviewNotes;
  }

  private async Task<IReadOnlyCollection<AuditEvent>> LoadAuditTrailAsync(DocumentCandidateId id, CancellationToken cancellationToken)
  {
    var records = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == nameof(DocumentCandidate) && item.SubjectId == id.ToString())
      .ToArrayAsync(cancellationToken);
    return records.Select(InfrastructureMapper.ToDomain).OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id.Value).ToArray();
  }

  private async Task<Dictionary<DocumentCandidateId, AuditEvent[]>> LoadAuditTrailMapAsync(DocumentCandidateId[] ids, CancellationToken cancellationToken)
  {
    var subjectIds = ids.Select(item => item.ToString()).ToArray();
    var records = await _dbContext.AuditEvents.AsNoTracking()
      .Where(item => item.SubjectType == nameof(DocumentCandidate) && subjectIds.Contains(item.SubjectId))
      .ToArrayAsync(cancellationToken);
    return records
      .Select(InfrastructureMapper.ToDomain)
      .GroupBy(item => new DocumentCandidateId(Guid.Parse(item.SubjectId)))
      .ToDictionary(group => group.Key, group => group.OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id.Value).ToArray());
  }
}
