using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AttachEvidence;

public sealed class AttachEvidenceUseCase
  : ICommandHandler<AttachEvidenceCommand, AttachEvidenceResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public AttachEvidenceUseCase(
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

  public async Task<AttachEvidenceResult> HandleAsync(
    AttachEvidenceCommand command,
    CancellationToken cancellationToken = default)
  {
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(
      command.InspectionSessionId,
      cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(
        nameof(InspectionSession),
        command.InspectionSessionId.ToString());

    var auditStart = inspectionSession.AuditTrail.Count;
    var evidenceAttachment = inspectionSession.AttachEvidence(
      EvidenceAttachmentId.New(),
      _currentUser.UserId.Value,
      _clock.UtcNow,
      new RawEvidenceReference(
        command.FileName,
        command.MediaType,
        command.StorageKey,
        command.Checksum));

    await _inspectionSessionRepository.UpdateAsync(inspectionSession, cancellationToken);
    StageAuditEvents(AuditTrailSlice.GetNewEvents(inspectionSession, auditStart));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new AttachEvidenceResult(
      evidenceAttachment.Id,
      inspectionSession.Id,
      evidenceAttachment.CapturedAtUtc);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
