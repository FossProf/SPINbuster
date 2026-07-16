using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadReportDraftSnapshot;

public sealed class LoadReportDraftSnapshotUseCase
  : IQueryHandler<LoadReportDraftSnapshotQuery, LoadReportDraftSnapshotResult>
{
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IProjectRepository _projectRepository;
  private readonly IReportRepository _reportRepository;

  public LoadReportDraftSnapshotUseCase(
    IReportRepository reportRepository,
    IProjectRepository projectRepository,
    IInspectionSessionRepository inspectionSessionRepository)
  {
    _reportRepository = reportRepository;
    _projectRepository = projectRepository;
    _inspectionSessionRepository = inspectionSessionRepository;
  }

  public async Task<LoadReportDraftSnapshotResult> HandleAsync(
    LoadReportDraftSnapshotQuery query,
    CancellationToken cancellationToken = default)
  {
    var report = await _reportRepository.GetByIdAsync(query.ReportId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Report), query.ReportId.ToString());
    var project = await _projectRepository.GetByIdAsync(report.ProjectId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Project), report.ProjectId.ToString());
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(report.InspectionSessionId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(InspectionSession), report.InspectionSessionId.ToString());

    var selectedFieldNotes = inspectionSession.FieldNotes
      .Where(fieldNote => report.SourceFieldNoteIds.Contains(fieldNote.Id))
      .Select(fieldNote => new ReportDraftFieldNoteSourceSnapshot(
        fieldNote.Id,
        fieldNote.RawText.Value,
        fieldNote.CapturedBy,
        fieldNote.CapturedAtUtc))
      .ToArray();

    var selectedEvidenceAttachments = inspectionSession.EvidenceAttachments
      .Where(evidenceAttachment => report.SourceEvidenceAttachmentIds.Contains(evidenceAttachment.Id))
      .Select(evidenceAttachment => new ReportDraftEvidenceSourceSnapshot(
        evidenceAttachment.Id,
        evidenceAttachment.RawEvidence.FileName,
        evidenceAttachment.RawEvidence.MediaType,
        evidenceAttachment.RawEvidence.StorageKey,
        evidenceAttachment.RawEvidence.Checksum,
        evidenceAttachment.CapturedBy,
        evidenceAttachment.CapturedAtUtc,
        evidenceAttachment.Interpretation?.Summary,
        evidenceAttachment.Interpretation?.InterpretedBy,
        evidenceAttachment.Interpretation?.InterpretedAtUtc))
      .ToArray();

    return new LoadReportDraftSnapshotResult(
      report.Id,
      report.Title.Value,
      report.RevisionNumber,
      report.Lifecycle,
      project.Id,
      project.Name,
      inspectionSession.Id,
      inspectionSession.Name,
      report.Sections
        .Select(section => new ReportDraftSectionSnapshot(section.Heading, section.Content))
        .ToArray(),
      selectedFieldNotes,
      selectedEvidenceAttachments,
      report.AuditTrail
        .Select(auditEvent => new ReportDraftAuditEntrySnapshot(
          auditEvent.Id,
          auditEvent.EventType,
          auditEvent.Actor,
          auditEvent.OccurredAtUtc,
          auditEvent.Description))
        .ToArray());
  }
}
