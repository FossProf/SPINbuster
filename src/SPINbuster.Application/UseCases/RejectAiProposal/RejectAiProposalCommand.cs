using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectAiProposal;

public sealed record RejectAiProposalCommand(
  ProposalId ProposalId,
  string ReviewDispositionNotes) : ICommand<RejectAiProposalResult>;
