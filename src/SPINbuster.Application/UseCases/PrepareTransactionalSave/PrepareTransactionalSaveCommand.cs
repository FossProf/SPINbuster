using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.PrepareTransactionalSave;

public sealed record PrepareTransactionalSaveCommand(
  ReportId ReportId) : ICommand<PrepareTransactionalSaveResult>;
