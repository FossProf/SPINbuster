using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AcceptAiProposal;

public sealed class AcceptAiProposalUseCase : ICommandHandler<AcceptAiProposalCommand, AcceptAiProposalResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IModelRunRepository _modelRunRepository;
  private readonly IAiProposalRepository _proposalRepository;
  private readonly IUnitOfWork _unitOfWork;

  public AcceptAiProposalUseCase(
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

  public async Task<AcceptAiProposalResult> HandleAsync(
    AcceptAiProposalCommand command,
    CancellationToken cancellationToken = default)
  {
    var proposal = await _proposalRepository.GetByIdAsync(command.ProposalId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException("AiProposal", command.ProposalId.ToString());
    var modelRun = await _modelRunRepository.GetByIdAsync(proposal.ModelRunId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(ModelRun), proposal.ModelRunId.ToString());

    proposal.Accept(command.ReviewDispositionNotes);
    modelRun.MarkReviewCompleted();
    modelRun.Close();

    await _proposalRepository.UpdateAsync(proposal, cancellationToken);
    await _modelRunRepository.UpdateAsync(modelRun, cancellationToken);
    _auditRecorder.Stage(AiAuditEventFactory.ProposalAccepted(proposal, _currentUser.UserId.Value, _clock.UtcNow));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new AcceptAiProposalResult(proposal.Id, proposal.Status, modelRun.Id, modelRun.State);
  }
}
