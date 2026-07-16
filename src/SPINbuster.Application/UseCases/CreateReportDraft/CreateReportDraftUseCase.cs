using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateReportDraft;

public sealed class CreateReportDraftUseCase
  : ICommandHandler<CreateReportDraftCommand, CreateReportDraftResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IProjectRepository _projectRepository;
  private readonly IReportRepository _reportRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CreateReportDraftUseCase(
    IProjectRepository projectRepository,
    IInspectionSessionRepository inspectionSessionRepository,
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _projectRepository = projectRepository;
    _inspectionSessionRepository = inspectionSessionRepository;
    _reportRepository = reportRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<CreateReportDraftResult> HandleAsync(
    CreateReportDraftCommand command,
    CancellationToken cancellationToken = default)
  {
    // The report-draft command is the first authoritative idempotent mutation:
    // a retry with the same operation ID must resolve to the original draft.
    var existingReport = await _reportRepository.GetByOperationIdAsync(command.OperationId, cancellationToken);
    if (existingReport is not null)
    {
      EnsureExistingReportMatches(existingReport, command);
      return new CreateReportDraftResult(existingReport.Id, existingReport.RevisionNumber, true);
    }

    var project = await _projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Project), command.ProjectId.ToString());
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(command.InspectionSessionId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(InspectionSession), command.InspectionSessionId.ToString());

    if (inspectionSession.ProjectId != project.Id)
    {
      throw new InvalidOperationException(
        $"Inspection session {inspectionSession.Id} does not belong to project {project.Id}.");
    }

    ValidateRequestedSources(command, inspectionSession);

    var report = new Report(
      ReportId.New(),
      project.Id,
      inspectionSession.Id,
      new ReportTitle(command.Title),
      command.Sections.Select(section => new ReportDraftSection(section.Heading, section.Content)),
      command.SourceFieldNoteIds,
      command.SourceEvidenceAttachmentIds,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _reportRepository.AddAsync(report, command.OperationId, cancellationToken);
    StageAuditEvents(report.AuditTrail);
    await _unitOfWork.CommitAsync(cancellationToken);

    return new CreateReportDraftResult(report.Id, report.RevisionNumber, false);
  }

  private static void ValidateRequestedSources(
    CreateReportDraftCommand command,
    InspectionSession inspectionSession)
  {
    // Report provenance must reference sources that actually belong to the
    // inspection session being drafted; cross-session source mixing is invalid.
    var fieldNotesById = inspectionSession.FieldNotes.ToDictionary(fieldNote => fieldNote.Id);
    foreach (var fieldNoteId in command.SourceFieldNoteIds.Distinct())
    {
      if (!fieldNotesById.ContainsKey(fieldNoteId))
      {
        throw new InvalidOperationException(
          $"Field note {fieldNoteId} is not part of inspection session {inspectionSession.Id}.");
      }
    }

    var evidenceById = inspectionSession.EvidenceAttachments.ToDictionary(evidenceAttachment => evidenceAttachment.Id);
    foreach (var evidenceAttachmentId in command.SourceEvidenceAttachmentIds.Distinct())
    {
      if (!evidenceById.ContainsKey(evidenceAttachmentId))
      {
        throw new InvalidOperationException(
          $"Evidence attachment {evidenceAttachmentId} is not part of inspection session {inspectionSession.Id}.");
      }
    }
  }

  private static void EnsureExistingReportMatches(Report existingReport, CreateReportDraftCommand command)
  {
    var sameProject = existingReport.ProjectId == command.ProjectId;
    var sameSession = existingReport.InspectionSessionId == command.InspectionSessionId;
    var sameTitle = existingReport.Title.Value == command.Title;
    var sameFieldNotes = existingReport.SourceFieldNoteIds.SequenceEqual(command.SourceFieldNoteIds);
    var sameEvidence = existingReport.SourceEvidenceAttachmentIds.SequenceEqual(command.SourceEvidenceAttachmentIds);
    var sameSections = existingReport.Sections.Select(section => (section.Heading, section.Content)).SequenceEqual(
      command.Sections.Select(section => (section.Heading, section.Content)));

    if (!sameProject || !sameSession || !sameTitle || !sameFieldNotes || !sameEvidence || !sameSections)
    {
      throw new InvalidOperationException(
        $"Operation ID {command.OperationId} was already used for a different report-draft request.");
    }
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
