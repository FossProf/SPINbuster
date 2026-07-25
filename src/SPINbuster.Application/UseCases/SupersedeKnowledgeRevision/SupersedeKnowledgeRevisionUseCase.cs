using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.SupersedeKnowledgeRevision;

public sealed class SupersedeKnowledgeRevisionUseCase
  : ICommandHandler<SupersedeKnowledgeRevisionCommand, SupersedeKnowledgeRevisionResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public SupersedeKnowledgeRevisionUseCase(
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

  public async Task<SupersedeKnowledgeRevisionResult> HandleAsync(
    SupersedeKnowledgeRevisionCommand command,
    CancellationToken cancellationToken = default)
  {
    var knowledgeDocument = await _knowledgeDocumentRepository.GetByIdAsync(command.KnowledgeDocumentId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocument), command.KnowledgeDocumentId.ToString());

    var initialAuditCount = knowledgeDocument.AuditTrail.Count;
    var successorRevision = new KnowledgeDocumentRevision(
      KnowledgeDocumentRevisionId.New(),
      knowledgeDocument.Id,
      command.KnowledgeSourceId,
      command.RevisionLabel,
      command.EffectiveDate,
      command.ReceivedAtUtc,
      command.SourceAuthority,
      command.ContentHash,
      command.MetadataHash,
      command.SupersededRevisionId,
      command.SourceSystemReference,
      command.DescriptiveNotes,
      _clock.UtcNow,
      command.IngestionStatus);

    var outcome = knowledgeDocument.SupersedeCurrentRevision(
      successorRevision,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _knowledgeDocumentRepository.UpdateAsync(knowledgeDocument, cancellationToken);
    await _knowledgeRevisionRepository.UpdateAsync(outcome.SupersededRevision, cancellationToken);
    await _knowledgeRevisionRepository.AddAsync(outcome.SuccessorRevision, cancellationToken);
    StageAuditEvents(AuditTrailSlice.GetNewEvents(knowledgeDocument, initialAuditCount));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new SupersedeKnowledgeRevisionResult(
      knowledgeDocument.Id,
      outcome.SuccessorRevision.Id,
      outcome.SupersededRevision.Id,
      knowledgeDocument.CurrentAuthoritativeRevisionId!.Value);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
