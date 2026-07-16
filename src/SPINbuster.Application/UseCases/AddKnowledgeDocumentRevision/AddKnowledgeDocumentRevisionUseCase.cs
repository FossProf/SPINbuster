using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddKnowledgeDocumentRevision;

public sealed class AddKnowledgeDocumentRevisionUseCase
  : ICommandHandler<AddKnowledgeDocumentRevisionCommand, AddKnowledgeDocumentRevisionResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public AddKnowledgeDocumentRevisionUseCase(
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

  public async Task<AddKnowledgeDocumentRevisionResult> HandleAsync(
    AddKnowledgeDocumentRevisionCommand command,
    CancellationToken cancellationToken = default)
  {
    var knowledgeDocument = await _knowledgeDocumentRepository.GetByIdAsync(command.KnowledgeDocumentId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocument), command.KnowledgeDocumentId.ToString());

    var initialAuditCount = knowledgeDocument.AuditTrail.Count;
    var knowledgeRevision = new KnowledgeDocumentRevision(
      KnowledgeDocumentRevisionId.New(),
      knowledgeDocument.Id,
      command.KnowledgeSourceId,
      command.RevisionLabel,
      command.EffectiveDate,
      command.ReceivedAtUtc,
      command.SourceAuthority,
      command.ContentHash,
      command.MetadataHash,
      null,
      command.SourceSystemReference,
      command.DescriptiveNotes,
      _clock.UtcNow,
      command.IngestionStatus);

    knowledgeDocument.AddInitialRevision(
      knowledgeRevision,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _knowledgeDocumentRepository.UpdateAsync(knowledgeDocument, cancellationToken);
    await _knowledgeRevisionRepository.AddAsync(knowledgeRevision, cancellationToken);
    StageAuditEvents(AuditTrailSlice.GetNewEvents(knowledgeDocument, initialAuditCount));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new AddKnowledgeDocumentRevisionResult(
      knowledgeDocument.Id,
      knowledgeRevision.Id,
      knowledgeDocument.CurrentAuthoritativeRevisionId,
      knowledgeRevision.Lifecycle);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
