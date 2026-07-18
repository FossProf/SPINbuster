using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CompleteDocumentImportSession;

public sealed class CompleteDocumentImportSessionUseCase : ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IDocumentImportSessionRepository _importSessionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CompleteDocumentImportSessionUseCase(
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

  public async Task<CompleteDocumentImportSessionResult> HandleAsync(CompleteDocumentImportSessionCommand command, CancellationToken cancellationToken = default)
  {
    var importSession = await _importSessionRepository.GetByIdAsync(command.ImportSessionId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(DocumentImportSession), command.ImportSessionId.ToString());

    var priorAuditCount = importSession.AuditTrail.Count;
    importSession.Complete(_currentUser.UserId.Value, _clock.UtcNow);
    await _importSessionRepository.UpdateAsync(importSession, cancellationToken);
    Internal.DocumentAuditStager.Stage(_auditRecorder, importSession.AuditTrail.Skip(priorAuditCount));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new CompleteDocumentImportSessionResult(
      importSession.Id,
      importSession.ProjectId,
      importSession.State,
      importSession.CompletedAtUtc,
      importSession.SourceCount,
      importSession.AcceptedCount,
      importSession.DuplicateCount,
      importSession.RejectedCount);
  }
}
