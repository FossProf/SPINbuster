using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateReportDraft;

public sealed record CreateReportDraftResult(
  ReportId ReportId,
  int RevisionNumber,
  bool WasDuplicateOperation);
