using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Persistence;

internal static class InfrastructureMapper
{
  public static Project ToDomain(ProjectRecord record, IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return Project.Rehydrate(
      record.Id,
      record.Name,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.Lifecycle,
      auditTrail);
  }

  public static ProjectRecord ToRecord(Project project)
  {
    return new ProjectRecord
    {
      Id = project.Id,
      Name = project.Name,
      CreatedBy = project.CreatedBy,
      CreatedAtUtc = project.CreatedAtUtc,
      Lifecycle = project.Lifecycle,
    };
  }

  public static InspectionSession ToDomain(
    InspectionSessionRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return InspectionSession.Rehydrate(
      record.Id,
      record.ProjectId,
      record.Name,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.Lifecycle,
      record.StartedAtUtc,
      record.CompletedAtUtc,
      record.FieldNotes.Select(ToDomain).ToArray(),
      record.EvidenceAttachments.Select(ToDomain).ToArray(),
      auditTrail);
  }

  public static InspectionSessionRecord ToRecord(InspectionSession inspectionSession)
  {
    var record = new InspectionSessionRecord
    {
      Id = inspectionSession.Id,
      ProjectId = inspectionSession.ProjectId,
      Name = inspectionSession.Name,
      CreatedBy = inspectionSession.CreatedBy,
      CreatedAtUtc = inspectionSession.CreatedAtUtc,
      Lifecycle = inspectionSession.Lifecycle,
      StartedAtUtc = inspectionSession.StartedAtUtc,
      CompletedAtUtc = inspectionSession.CompletedAtUtc,
    };

    record.FieldNotes.AddRange(inspectionSession.FieldNotes.Select(ToRecord));
    record.EvidenceAttachments.AddRange(inspectionSession.EvidenceAttachments.Select(ToRecord));
    return record;
  }

  public static FieldNote ToDomain(FieldNoteRecord record)
  {
    return new FieldNote(
      record.Id,
      record.InspectionSessionId,
      record.CapturedBy,
      record.CapturedAtUtc,
      new FieldNoteRawText(record.RawText));
  }

  public static FieldNoteRecord ToRecord(FieldNote fieldNote)
  {
    return new FieldNoteRecord
    {
      Id = fieldNote.Id,
      InspectionSessionId = fieldNote.InspectionSessionId,
      CapturedBy = fieldNote.CapturedBy,
      CapturedAtUtc = fieldNote.CapturedAtUtc,
      RawText = fieldNote.RawText.Value,
    };
  }

  public static EvidenceAttachment ToDomain(EvidenceAttachmentRecord record)
  {
    var interpretation = string.IsNullOrWhiteSpace(record.InterpretationSummary)
      ? null
      : new EvidenceInterpretation(
        record.InterpretationSummary,
        record.InterpretedBy!,
        record.InterpretedAtUtc!.Value);

    return EvidenceAttachment.Rehydrate(
      record.Id,
      record.InspectionSessionId,
      record.CapturedBy,
      record.CapturedAtUtc,
      new RawEvidenceReference(record.FileName, record.MediaType, record.StorageKey, record.Checksum),
      interpretation);
  }

  public static EvidenceAttachmentRecord ToRecord(EvidenceAttachment evidenceAttachment)
  {
    return new EvidenceAttachmentRecord
    {
      Id = evidenceAttachment.Id,
      InspectionSessionId = evidenceAttachment.InspectionSessionId,
      CapturedBy = evidenceAttachment.CapturedBy,
      CapturedAtUtc = evidenceAttachment.CapturedAtUtc,
      FileName = evidenceAttachment.RawEvidence.FileName,
      MediaType = evidenceAttachment.RawEvidence.MediaType,
      StorageKey = evidenceAttachment.RawEvidence.StorageKey,
      Checksum = evidenceAttachment.RawEvidence.Checksum,
      InterpretationSummary = evidenceAttachment.Interpretation?.Summary,
      InterpretedBy = evidenceAttachment.Interpretation?.InterpretedBy,
      InterpretedAtUtc = evidenceAttachment.Interpretation?.InterpretedAtUtc,
    };
  }

  public static Report ToDomain(ReportRecord record, IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return Report.Rehydrate(
      record.Id,
      record.ProjectId,
      record.InspectionSessionId,
      record.Title,
      record.Body,
      record.CreatedBy,
      record.CreatedAtUtc,
      record.Lifecycle,
      record.ApprovedBy,
      record.ApprovedAtUtc,
      auditTrail);
  }

  public static ReportRecord ToRecord(Report report)
  {
    return new ReportRecord
    {
      Id = report.Id,
      ProjectId = report.ProjectId,
      InspectionSessionId = report.InspectionSessionId,
      Title = report.Title,
      Body = report.Body,
      CreatedBy = report.CreatedBy,
      CreatedAtUtc = report.CreatedAtUtc,
      Lifecycle = report.Lifecycle,
      ApprovedBy = report.ApprovedBy,
      ApprovedAtUtc = report.ApprovedAtUtc,
    };
  }

  public static SaveTransaction ToDomain(
    SaveTransactionRecord record,
    IReadOnlyCollection<AuditEvent> auditTrail)
  {
    return SaveTransaction.Rehydrate(
      record.Id,
      record.ReportId,
      record.InitiatedBy,
      record.CreatedAtUtc,
      record.State,
      record.FailureReason,
      record.PreparedAtUtc,
      record.PersistedAtUtc,
      record.CompletedAtUtc,
      auditTrail);
  }

  public static SaveTransactionRecord ToRecord(SaveTransaction saveTransaction)
  {
    return new SaveTransactionRecord
    {
      Id = saveTransaction.Id,
      ReportId = saveTransaction.ReportId,
      InitiatedBy = saveTransaction.InitiatedBy,
      CreatedAtUtc = saveTransaction.CreatedAtUtc,
      State = saveTransaction.State,
      FailureReason = saveTransaction.FailureReason,
      PreparedAtUtc = saveTransaction.PreparedAtUtc,
      PersistedAtUtc = saveTransaction.PersistedAtUtc,
      CompletedAtUtc = saveTransaction.CompletedAtUtc,
    };
  }

  public static AuditEvent ToDomain(AuditEventRecord record)
  {
    return new AuditEvent(
      record.Id,
      record.SubjectType,
      record.SubjectId,
      record.EventType,
      record.Actor,
      record.OccurredAtUtc,
      record.Description);
  }

  public static AuditEventRecord ToRecord(AuditEvent auditEvent)
  {
    return new AuditEventRecord
    {
      Id = auditEvent.Id,
      SubjectType = auditEvent.SubjectType,
      SubjectId = auditEvent.SubjectId,
      EventType = auditEvent.EventType,
      Actor = auditEvent.Actor,
      OccurredAtUtc = auditEvent.OccurredAtUtc,
      Description = auditEvent.Description,
    };
  }
}
