using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectDocumentCandidate;

public sealed class RejectDocumentCandidateUseCase : ICommandHandler<RejectDocumentCandidateCommand, RejectDocumentCandidateResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IDocumentCandidateRepository _documentCandidateRepository;
  private readonly IUnitOfWork _unitOfWork;

  public RejectDocumentCandidateUseCase(
    IDocumentCandidateRepository documentCandidateRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _documentCandidateRepository = documentCandidateRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<RejectDocumentCandidateResult> HandleAsync(RejectDocumentCandidateCommand command, CancellationToken cancellationToken = default)
  {
    var candidate = await _documentCandidateRepository.GetByIdAsync(command.DocumentCandidateId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(DocumentCandidate), command.DocumentCandidateId.ToString());
    var priorAuditCount = candidate.AuditTrail.Count;
    candidate.Reject(_currentUser.UserId.Value, _clock.UtcNow, command.ReviewNotes);
    await _documentCandidateRepository.UpdateAsync(candidate, cancellationToken);
    Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail.Skip(priorAuditCount));
    await _unitOfWork.CommitAsync(cancellationToken);
    return new RejectDocumentCandidateResult(candidate.Id, candidate.Status, _currentUser.UserId.Value, candidate.ReviewedAtUtc!.Value);
  }
}
