using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteAiProposalRepository : IAiProposalRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteAiProposalRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<AiProposal?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.AiProposals
      .AsNoTracking()
      .SingleOrDefaultAsync(proposal => proposal.Id == proposalId, cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<AiProposal?> GetByModelRunIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.AiProposals
      .AsNoTracking()
      .SingleOrDefaultAsync(proposal => proposal.ModelRunId == modelRunId, cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public Task AddAsync(AiProposal proposal, CancellationToken cancellationToken = default)
  {
    _dbContext.AiProposals.Add(InfrastructureMapper.ToRecord(proposal));
    return Task.CompletedTask;
  }

  public Task UpdateAsync(AiProposal proposal, CancellationToken cancellationToken = default)
  {
    _dbContext.AiProposals.Update(InfrastructureMapper.ToRecord(proposal));
    return Task.CompletedTask;
  }
}
