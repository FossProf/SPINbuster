using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;

public sealed record LoadAiProposalWorkflowSnapshotQuery(
  ModelRunId ModelRunId,
  ProposalId? ProposalId) : IQuery<LoadAiProposalWorkflowSnapshotResult>;
