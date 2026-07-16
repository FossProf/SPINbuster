using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AcceptAiProposal;

public sealed record AcceptAiProposalCommand(
  ProposalId ProposalId,
  string ReviewDispositionNotes) : ICommand<AcceptAiProposalResult>;
