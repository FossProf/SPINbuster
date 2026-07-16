using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateKnowledgeRelationship;

public sealed class CreateKnowledgeRelationshipUseCase
  : ICommandHandler<CreateKnowledgeRelationshipCommand, CreateKnowledgeRelationshipResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRelationshipRepository _knowledgeRelationshipRepository;
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CreateKnowledgeRelationshipUseCase(
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IKnowledgeRevisionRepository knowledgeRevisionRepository,
    IKnowledgeRelationshipRepository knowledgeRelationshipRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _knowledgeRevisionRepository = knowledgeRevisionRepository;
    _knowledgeRelationshipRepository = knowledgeRelationshipRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<CreateKnowledgeRelationshipResult> HandleAsync(
    CreateKnowledgeRelationshipCommand command,
    CancellationToken cancellationToken = default)
  {
    await EnsureSubjectExistsAsync(command.ProjectId, command.Source, cancellationToken);
    await EnsureSubjectExistsAsync(command.ProjectId, command.Target, cancellationToken);

    var existingRelationship = await _knowledgeRelationshipRepository.FindByEndpointsAsync(
      command.ProjectId,
      command.Source,
      command.Target,
      command.RelationshipType,
      cancellationToken);
    if (existingRelationship is not null)
    {
      throw new DomainInvariantException(
        $"Duplicate knowledge relationship {command.RelationshipType} between {command.Source.ToStableKey()} and {command.Target.ToStableKey()} is not allowed.");
    }

    var knowledgeRelationship = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      command.ProjectId,
      command.Source,
      command.Target,
      command.RelationshipType,
      command.EvidenceOrRationale,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _knowledgeRelationshipRepository.AddAsync(knowledgeRelationship, cancellationToken);
    StageAuditEvents(knowledgeRelationship.AuditTrail);
    await _unitOfWork.CommitAsync(cancellationToken);

    return new CreateKnowledgeRelationshipResult(
      knowledgeRelationship.Id,
      knowledgeRelationship.RelationshipType == KnowledgeRelationshipType.Contradicts);
  }

  private async Task EnsureSubjectExistsAsync(
    ProjectId projectId,
    KnowledgeSubjectReference subject,
    CancellationToken cancellationToken)
  {
    if (subject.ProjectId != projectId)
    {
      throw new DomainInvariantException("Knowledge subject references must remain project-scoped.");
    }

    switch (subject.SubjectKind)
    {
      case KnowledgeSubjectKind.Document:
        {
          var documentId = subject.DocumentId
            ?? throw new DomainInvariantException("Document subject references must include a document ID.");
          var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocument), documentId.ToString());
          if (document.ProjectId != projectId)
          {
            throw new DomainInvariantException("Knowledge relationships cannot cross project boundaries.");
          }

          break;
        }

      case KnowledgeSubjectKind.Revision:
        {
          var revisionId = subject.RevisionId
            ?? throw new DomainInvariantException("Revision subject references must include a revision ID.");
          var revision = await _knowledgeRevisionRepository.GetByIdAsync(revisionId, cancellationToken)
            ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocumentRevision), revisionId.ToString());
          var document = await _knowledgeDocumentRepository.GetByIdAsync(revision.DocumentId, cancellationToken)
            ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocument), revision.DocumentId.ToString());
          if (document.ProjectId != projectId)
          {
            throw new DomainInvariantException("Knowledge relationships cannot cross project boundaries.");
          }

          break;
        }

      default:
        throw new DomainInvariantException($"Unsupported knowledge subject kind {subject.SubjectKind}.");
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
