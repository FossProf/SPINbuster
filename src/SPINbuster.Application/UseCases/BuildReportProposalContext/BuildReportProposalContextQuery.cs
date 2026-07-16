using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.BuildReportProposalContext;

public sealed record BuildReportProposalContextQuery(ReportId ReportId) : IQuery<BuildReportProposalContextResult>;
