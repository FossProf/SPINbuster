using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.StartInspectionSession;

public sealed record StartInspectionSessionCommand(
  ProjectId ProjectId,
  string SessionName) : ICommand<StartInspectionSessionResult>;
