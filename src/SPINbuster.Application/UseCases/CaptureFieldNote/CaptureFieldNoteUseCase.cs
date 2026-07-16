using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CaptureFieldNote;

public sealed class CaptureFieldNoteUseCase
  : ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CaptureFieldNoteUseCase(
    IInspectionSessionRepository inspectionSessionRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _inspectionSessionRepository = inspectionSessionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<CaptureFieldNoteResult> HandleAsync(
    CaptureFieldNoteCommand command,
    CancellationToken cancellationToken = default)
  {
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(
      command.InspectionSessionId,
      cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(
        nameof(InspectionSession),
        command.InspectionSessionId.ToString());

    var auditStart = inspectionSession.AuditTrail.Count;
    var fieldNote = inspectionSession.RecordFieldNote(
      FieldNoteId.New(),
      _currentUser.UserId.Value,
      _clock.UtcNow,
      new FieldNoteRawText(command.RawText));

    await _inspectionSessionRepository.UpdateAsync(inspectionSession, cancellationToken);
    StageAuditEvents(AuditTrailSlice.GetNewEvents(inspectionSession, auditStart));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new CaptureFieldNoteResult(
      fieldNote.Id,
      inspectionSession.Id,
      fieldNote.CapturedAtUtc);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
