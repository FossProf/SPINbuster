using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadAiProposal;

public sealed record LoadAiProposalQuery(ProposalId ProposalId) : IQuery<LoadAiProposalResult>;
