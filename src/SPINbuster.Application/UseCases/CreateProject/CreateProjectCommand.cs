using SPINbuster.Application.Contracts;

namespace SPINbuster.Application.UseCases.CreateProject;

public sealed record CreateProjectCommand(string Name) : ICommand<CreateProjectResult>;
