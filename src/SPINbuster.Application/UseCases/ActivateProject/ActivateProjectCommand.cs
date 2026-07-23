using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.ActivateProject;

public sealed record ActivateProjectCommand(ProjectId ProjectId) : ICommand<ActivateProjectResult>;
