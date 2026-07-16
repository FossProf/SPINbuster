using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AcceptAiProposal;

public sealed record AcceptAiProposalResult(
  ProposalId ProposalId,
  ProposalStatus ProposalStatus,
  ModelRunId ModelRunId,
  ModelRunState ModelRunState);
