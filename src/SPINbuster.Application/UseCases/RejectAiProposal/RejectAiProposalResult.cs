using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectAiProposal;

public sealed record RejectAiProposalResult(
  ProposalId ProposalId,
  ProposalStatus ProposalStatus,
  ModelRunId ModelRunId,
  ModelRunState ModelRunState);
