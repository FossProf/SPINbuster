using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.GenerateReportDraftRequest;

public sealed record GenerateReportDraftRequestQuery(
  ProjectId ProjectId,
  InspectionSessionId InspectionSessionId,
  string DraftTitle) : IQuery<GenerateReportDraftRequestResult>;
