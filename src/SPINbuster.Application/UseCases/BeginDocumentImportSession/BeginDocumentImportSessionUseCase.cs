using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.BeginDocumentImportSession;

public sealed class BeginDocumentImportSessionUseCase : ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IDocumentImportSessionRepository _importSessionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public BeginDocumentImportSessionUseCase(
    IDocumentImportSessionRepository importSessionRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _importSessionRepository = importSessionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<BeginDocumentImportSessionResult> HandleAsync(BeginDocumentImportSessionCommand command, CancellationToken cancellationToken = default)
  {
    var importSession = new DocumentImportSession(DocumentImportSessionId.New(), command.ProjectId, _currentUser.UserId.Value, _clock.UtcNow);
    await _importSessionRepository.AddAsync(importSession, cancellationToken);
    Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail);
    await _unitOfWork.CommitAsync(cancellationToken);
    return new BeginDocumentImportSessionResult(importSession.Id, importSession.ProjectId, importSession.State, importSession.StartedAtUtc);
  }
}
