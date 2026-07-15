using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddInterpretation;

public sealed class AddInterpretationUseCase
  : ICommandHandler<AddInterpretationCommand, AddInterpretationResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public AddInterpretationUseCase(
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

  public async Task<AddInterpretationResult> HandleAsync(
    AddInterpretationCommand command,
    CancellationToken cancellationToken = default)
  {
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(
      command.InspectionSessionId,
      cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(
        nameof(InspectionSession),
        command.InspectionSessionId.ToString());

    var interpretedAtUtc = _clock.UtcNow;
    var interpretation = new EvidenceInterpretation(
      command.Summary,
      _currentUser.UserId,
      interpretedAtUtc);

    var auditStart = inspectionSession.AuditTrail.Count;
    inspectionSession.InterpretEvidence(command.EvidenceAttachmentId, interpretation);

    await _inspectionSessionRepository.UpdateAsync(inspectionSession, cancellationToken);
    StageAuditEvents(AuditTrailSlice.GetNewEvents(inspectionSession, auditStart));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new AddInterpretationResult(
      inspectionSession.Id,
      command.EvidenceAttachmentId,
      interpretation.Summary,
      interpretation.InterpretedAtUtc,
      interpretation.InterpretedBy);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
