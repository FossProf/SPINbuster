using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RecordDocumentCandidateReview;

public sealed class RecordDocumentCandidateReviewUseCase : ICommandHandler<RecordDocumentCandidateReviewCommand, RecordDocumentCandidateReviewResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IDocumentCandidateRepository _documentCandidateRepository;
  private readonly IUnitOfWork _unitOfWork;

  public RecordDocumentCandidateReviewUseCase(
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

  public async Task<RecordDocumentCandidateReviewResult> HandleAsync(RecordDocumentCandidateReviewCommand command, CancellationToken cancellationToken = default)
  {
    var candidate = await _documentCandidateRepository.GetByIdAsync(command.DocumentCandidateId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(DocumentCandidate), command.DocumentCandidateId.ToString());
    var priorAuditCount = candidate.AuditTrail.Count;
    if (command.Disposition == DocumentCandidateReviewDisposition.HumanAccepted)
    {
      candidate.Accept(_currentUser.UserId.Value, _clock.UtcNow, command.ReviewNotes);
    }
    else
    {
      candidate.Reject(_currentUser.UserId.Value, _clock.UtcNow, command.ReviewNotes);
    }

    await _documentCandidateRepository.UpdateAsync(candidate, cancellationToken);
    Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail.Skip(priorAuditCount));
    await _unitOfWork.CommitAsync(cancellationToken);
    return new RecordDocumentCandidateReviewResult(candidate.Id, candidate.Status, _currentUser.UserId.Value, candidate.ReviewedAtUtc!.Value);
  }
}
