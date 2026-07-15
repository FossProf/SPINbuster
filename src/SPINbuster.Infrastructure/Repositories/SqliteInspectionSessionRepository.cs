using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteInspectionSessionRepository : IInspectionSessionRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteInspectionSessionRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<InspectionSession?> GetByIdAsync(
    InspectionSessionId inspectionSessionId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.InspectionSessions
      .AsNoTracking()
      .Include(session => session.FieldNotes)
      .Include(session => session.EvidenceAttachments)
      .AsSplitQuery()
      .SingleOrDefaultAsync(session => session.Id == inspectionSessionId, cancellationToken);

    if (record is null)
    {
      return null;
    }

    var auditTrail = await LoadAuditTrailAsync(nameof(InspectionSession), inspectionSessionId.ToString(), cancellationToken);
    return InfrastructureMapper.ToDomain(record, auditTrail);
  }

  public Task AddAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    _dbContext.InspectionSessions.Add(InfrastructureMapper.ToRecord(inspectionSession));
    return Task.CompletedTask;
  }

  public async Task UpdateAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    var existing = await _dbContext.InspectionSessions
      .Include(session => session.FieldNotes)
      .Include(session => session.EvidenceAttachments)
      .AsSplitQuery()
      .SingleAsync(session => session.Id == inspectionSession.Id, cancellationToken);

    existing.Lifecycle = inspectionSession.Lifecycle;
    existing.StartedAtUtc = inspectionSession.StartedAtUtc;
    existing.CompletedAtUtc = inspectionSession.CompletedAtUtc;

    var existingFieldNotesById = existing.FieldNotes.ToDictionary(record => record.Id);
    foreach (var fieldNote in inspectionSession.FieldNotes)
    {
      if (!existingFieldNotesById.ContainsKey(fieldNote.Id))
      {
        existing.FieldNotes.Add(InfrastructureMapper.ToRecord(fieldNote));
      }
    }

    var existingEvidenceById = existing.EvidenceAttachments.ToDictionary(record => record.Id);
    foreach (var evidenceAttachment in inspectionSession.EvidenceAttachments)
    {
      if (!existingEvidenceById.TryGetValue(evidenceAttachment.Id, out var existingEvidence))
      {
        existing.EvidenceAttachments.Add(InfrastructureMapper.ToRecord(evidenceAttachment));
        continue;
      }

      ApplyEvidenceInterpretation(existingEvidence, evidenceAttachment);
    }
  }

  private static void ApplyEvidenceInterpretation(
    EvidenceAttachmentRecord existingRecord,
    EvidenceAttachment domainEvidence)
  {
    existingRecord.InterpretationSummary = domainEvidence.Interpretation?.Summary;
    existingRecord.InterpretedBy = domainEvidence.Interpretation?.InterpretedBy;
    existingRecord.InterpretedAtUtc = domainEvidence.Interpretation?.InterpretedAtUtc;
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
      .ThenBy(auditEvent => auditEvent.Id)
      .ToArray();
  }
}
