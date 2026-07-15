using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateProject;

public sealed record CreateProjectResult(
  ProjectId ProjectId,
  string Name,
  ProjectLifecycle Lifecycle,
  DateTimeOffset CreatedAtUtc);
