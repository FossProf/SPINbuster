using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.VerifyKnowledgeRevision;

public sealed class VerifyKnowledgeRevisionUseCase
  : ICommandHandler<VerifyKnowledgeRevisionCommand, VerifyKnowledgeRevisionResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public VerifyKnowledgeRevisionUseCase(
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IKnowledgeRevisionRepository knowledgeRevisionRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _knowledgeRevisionRepository = knowledgeRevisionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<VerifyKnowledgeRevisionResult> HandleAsync(
    VerifyKnowledgeRevisionCommand command,
    CancellationToken cancellationToken = default)
  {
    var knowledgeDocument = await _knowledgeDocumentRepository.GetByIdAsync(command.KnowledgeDocumentId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocument), command.KnowledgeDocumentId.ToString());

    var initialAuditCount = knowledgeDocument.AuditTrail.Count;
    var revision = knowledgeDocument.VerifyRevision(
      command.KnowledgeDocumentRevisionId,
      command.VerificationStatus,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _knowledgeDocumentRepository.UpdateAsync(knowledgeDocument, cancellationToken);
    await _knowledgeRevisionRepository.UpdateAsync(revision, cancellationToken);
    StageAuditEvents(AuditTrailSlice.GetNewEvents(knowledgeDocument, initialAuditCount));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new VerifyKnowledgeRevisionResult(
      knowledgeDocument.Id,
      revision.Id,
      revision.VerificationStatus);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
