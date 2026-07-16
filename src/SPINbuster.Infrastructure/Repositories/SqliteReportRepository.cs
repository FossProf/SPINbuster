using SPINbuster.Application;
using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteReportRepository : IReportRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteReportRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<Report?> GetByIdAsync(ReportId reportId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.Reports
      .AsNoTracking()
      .Include(report => report.Sections)
      .Include(report => report.FieldNoteSources)
      .Include(report => report.EvidenceSources)
      .AsSplitQuery()
      .SingleOrDefaultAsync(report => report.Id == reportId, cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditRecords = await _dbContext.AuditEvents
      .AsNoTracking()
      .Where(auditEvent => auditEvent.SubjectType == nameof(Report) && auditEvent.SubjectId == reportId.ToString())
      .ToArrayAsync(cancellationToken);

    return InfrastructureMapper.ToDomain(
      record,
      auditRecords
        .Select(InfrastructureMapper.ToDomain)
        .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
        .ThenBy(auditEvent => auditEvent.Id)
        .ToArray());
  }

  public async Task<Report?> GetByOperationIdAsync(OperationId operationId, CancellationToken cancellationToken = default)
  {
    var reportId = await _dbContext.ReportDraftOperations
      .AsNoTracking()
      .Where(operation => operation.OperationId == operationId)
      .Select(operation => (ReportId?)operation.ReportId)
      .SingleOrDefaultAsync(cancellationToken);

    return reportId is null
      ? null
      : await GetByIdAsync(reportId.Value, cancellationToken);
  }

  public Task AddAsync(Report report, OperationId operationId, CancellationToken cancellationToken = default)
  {
    var reportRecord = InfrastructureMapper.ToRecord(report);
    // Persist report content and provenance explicitly so detached rehydration
    // does not rely on implicit EF tracking behavior.
    reportRecord.Sections.AddRange(report.Sections.Select((section, index) => new ReportSectionRecord
    {
      ReportId = report.Id,
      Position = index,
      Heading = section.Heading,
      Content = section.Content,
    }));
    reportRecord.FieldNoteSources.AddRange(report.SourceFieldNoteIds.Select(fieldNoteId => new ReportFieldNoteSourceRecord
    {
      ReportId = report.Id,
      FieldNoteId = fieldNoteId,
    }));
    reportRecord.EvidenceSources.AddRange(report.SourceEvidenceAttachmentIds.Select(evidenceAttachmentId => new ReportEvidenceSourceRecord
    {
      ReportId = report.Id,
      EvidenceAttachmentId = evidenceAttachmentId,
    }));
    reportRecord.Operations.Add(new ReportDraftOperationRecord
    {
      OperationId = operationId,
      ReportId = report.Id,
    });

    _dbContext.Reports.Add(reportRecord);
    return Task.CompletedTask;
  }
}
