using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadReportDraftSnapshot;

public sealed record LoadReportDraftSnapshotQuery(ReportId ReportId) : IQuery<LoadReportDraftSnapshotResult>;
