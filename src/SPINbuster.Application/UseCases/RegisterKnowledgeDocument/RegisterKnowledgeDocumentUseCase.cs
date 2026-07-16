using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RegisterKnowledgeDocument;

public sealed class RegisterKnowledgeDocumentUseCase
  : ICommandHandler<RegisterKnowledgeDocumentCommand, RegisterKnowledgeDocumentResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IUnitOfWork _unitOfWork;

  public RegisterKnowledgeDocumentUseCase(
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<RegisterKnowledgeDocumentResult> HandleAsync(
    RegisterKnowledgeDocumentCommand command,
    CancellationToken cancellationToken = default)
  {
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      command.ProjectId,
      command.DocumentType,
      command.CanonicalTitle,
      command.ExternalReferenceNumber,
      command.DisciplineOrCategory,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _knowledgeDocumentRepository.AddAsync(knowledgeDocument, cancellationToken);
    StageAuditEvents(knowledgeDocument.AuditTrail);
    await _unitOfWork.CommitAsync(cancellationToken);

    return new RegisterKnowledgeDocumentResult(
      knowledgeDocument.Id,
      knowledgeDocument.Lifecycle,
      knowledgeDocument.DocumentType,
      knowledgeDocument.CanonicalTitle);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
