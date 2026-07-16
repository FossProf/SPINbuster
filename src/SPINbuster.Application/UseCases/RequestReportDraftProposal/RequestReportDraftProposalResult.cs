using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestReportDraftProposal;

public sealed record RequestReportDraftProposalResult(
  ProposalId? ProposalId,
  ModelRunId ModelRunId,
  ProposalStatus? ProposalStatus,
  ModelRunState ModelRunState,
  ModelRunFailureClassification FailureClassification,
  string? FailureMessage,
  bool IsIdempotentReplay);
