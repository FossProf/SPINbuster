using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.GenerateReportDraftRequest;

public sealed class GenerateReportDraftRequestUseCase
  : IQueryHandler<GenerateReportDraftRequestQuery, GenerateReportDraftRequestResult>
{
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IProjectRepository _projectRepository;

  public GenerateReportDraftRequestUseCase(
    IProjectRepository projectRepository,
    IInspectionSessionRepository inspectionSessionRepository)
  {
    _projectRepository = projectRepository;
    _inspectionSessionRepository = inspectionSessionRepository;
  }

  public async Task<GenerateReportDraftRequestResult> HandleAsync(
    GenerateReportDraftRequestQuery query,
    CancellationToken cancellationToken = default)
  {
    var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Project), query.ProjectId.ToString());

    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(
      query.InspectionSessionId,
      cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(
        nameof(InspectionSession),
        query.InspectionSessionId.ToString());

    if (inspectionSession.ProjectId != project.Id)
    {
      throw new InvalidOperationException(
        $"Inspection session {inspectionSession.Id} does not belong to project {project.Id}.");
    }

    var fieldNotes = inspectionSession.FieldNotes
      .Select(fieldNote => new ReportDraftFieldNote(
        fieldNote.Id,
        fieldNote.RawText.Value,
        fieldNote.CapturedBy,
        fieldNote.CapturedAtUtc))
      .ToArray();

    // This query intentionally returns raw inspection material plus the current
    // interpretation snapshot. Approval, persistence, and AI execution stay out
    // of the Application contract at this stage.
    var evidenceAttachments = inspectionSession.EvidenceAttachments
      .Select(evidenceAttachment => new ReportDraftEvidenceAttachment(
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

    return new GenerateReportDraftRequestResult(
      project.Id,
      project.Name,
      inspectionSession.Id,
      inspectionSession.Name,
      query.DraftTitle,
      fieldNotes,
      evidenceAttachments);
  }
}
