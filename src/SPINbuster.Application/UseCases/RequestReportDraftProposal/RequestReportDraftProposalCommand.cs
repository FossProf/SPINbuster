using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestReportDraftProposal;

public sealed record RequestReportDraftProposalCommand(
  OperationId OperationId,
  ReportId ReportId,
  string PromptPackageId,
  string PromptPackageVersion,
  decimal? Temperature) : ICommand<RequestReportDraftProposalResult>;
