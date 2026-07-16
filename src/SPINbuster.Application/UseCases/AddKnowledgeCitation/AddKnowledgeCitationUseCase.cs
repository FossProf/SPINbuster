using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddKnowledgeCitation;

public sealed class AddKnowledgeCitationUseCase : ICommandHandler<AddKnowledgeCitationCommand, AddKnowledgeCitationResult>
{
  private readonly IKnowledgeCitationRepository _knowledgeCitationRepository;
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IAuditRecorder _auditRecorder;

  public AddKnowledgeCitationUseCase(
    IKnowledgeCitationRepository knowledgeCitationRepository,
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IKnowledgeRevisionRepository knowledgeRevisionRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _knowledgeCitationRepository = knowledgeCitationRepository;
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _knowledgeRevisionRepository = knowledgeRevisionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<AddKnowledgeCitationResult> HandleAsync(
    AddKnowledgeCitationCommand command,
    CancellationToken cancellationToken = default)
  {
    var revision = await _knowledgeRevisionRepository.GetByIdAsync(command.KnowledgeDocumentRevisionId, cancellationToken)
      ?? throw new DomainInvariantException($"Knowledge revision {command.KnowledgeDocumentRevisionId} was not found.");
    var document = await _knowledgeDocumentRepository.GetByIdAsync(revision.DocumentId, cancellationToken)
      ?? throw new DomainInvariantException($"Knowledge document {revision.DocumentId} was not found.");

    if (document.ProjectId != command.ProjectId)
    {
      throw new DomainInvariantException("Knowledge citations cannot cross project boundaries.");
    }

    var normalizedLocatorValue = command.LocatorValue.Trim();
    var existingCitations = await _knowledgeCitationRepository.GetByRevisionIdAsync(command.KnowledgeDocumentRevisionId, cancellationToken);
    if (existingCitations.Any(citation =>
      citation.LocatorType == command.LocatorType
      && string.Equals(citation.LocatorValue, normalizedLocatorValue, StringComparison.OrdinalIgnoreCase)))
    {
      throw new DomainInvariantException(
        $"Duplicate knowledge citation {command.LocatorType}:{command.LocatorValue} is not allowed on revision {command.KnowledgeDocumentRevisionId}.");
    }

    var citation = new KnowledgeCitation(
      KnowledgeCitationId.New(),
      command.KnowledgeDocumentRevisionId,
      command.LocatorType,
      normalizedLocatorValue,
      revision.ContentHash,
      _clock.UtcNow,
      command.QuotedOrSummarizedText);

    await _knowledgeCitationRepository.AddAsync(citation, cancellationToken);
    _auditRecorder.Stage(new AuditEvent(
      AuditEventId.New(),
      nameof(KnowledgeDocumentRevision),
      revision.Id.ToString(),
      "KnowledgeCitationAdded",
      _currentUser.UserId.Value,
      _clock.UtcNow,
      $"Knowledge citation {citation.LocatorType}:{citation.LocatorValue} added to revision {revision.RevisionLabel}."));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new AddKnowledgeCitationResult(
      citation.Id,
      citation.CitedRevisionId,
      citation.LocatorType,
      citation.LocatorValue);
  }
}
