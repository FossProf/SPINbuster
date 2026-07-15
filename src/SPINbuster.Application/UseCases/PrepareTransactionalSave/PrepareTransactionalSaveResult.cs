using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.PrepareTransactionalSave;

public sealed record PrepareTransactionalSaveResult(
  SaveTransactionId SaveTransactionId,
  ReportId ReportId,
  SaveTransactionState State,
  DateTimeOffset PreparedAtUtc);
