using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IAiProposalRepository
{
  Task<AiProposal?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken = default);

  Task<AiProposal?> GetByModelRunIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default);

  Task AddAsync(AiProposal proposal, CancellationToken cancellationToken = default);

  Task UpdateAsync(AiProposal proposal, CancellationToken cancellationToken = default);
}
