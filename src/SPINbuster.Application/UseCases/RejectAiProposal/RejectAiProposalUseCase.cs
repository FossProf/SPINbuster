using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectAiProposal;

public sealed class RejectAiProposalUseCase : ICommandHandler<RejectAiProposalCommand, RejectAiProposalResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IModelRunRepository _modelRunRepository;
  private readonly IAiProposalRepository _proposalRepository;
  private readonly IUnitOfWork _unitOfWork;

  public RejectAiProposalUseCase(
    IAiProposalRepository proposalRepository,
    IModelRunRepository modelRunRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _proposalRepository = proposalRepository;
    _modelRunRepository = modelRunRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<RejectAiProposalResult> HandleAsync(
    RejectAiProposalCommand command,
    CancellationToken cancellationToken = default)
  {
    var proposal = await _proposalRepository.GetByIdAsync(command.ProposalId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException("AiProposal", command.ProposalId.ToString());
    var modelRun = await _modelRunRepository.GetByIdAsync(proposal.ModelRunId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(ModelRun), proposal.ModelRunId.ToString());

    proposal.Reject(command.ReviewDispositionNotes);
    modelRun.MarkReviewCompleted();
    modelRun.Close();

    await _proposalRepository.UpdateAsync(proposal, cancellationToken);
    await _modelRunRepository.UpdateAsync(modelRun, cancellationToken);
    _auditRecorder.Stage(AiAuditEventFactory.ProposalRejected(proposal, _currentUser.UserId.Value, _clock.UtcNow));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new RejectAiProposalResult(proposal.Id, proposal.Status, modelRun.Id, modelRun.State);
  }
}
