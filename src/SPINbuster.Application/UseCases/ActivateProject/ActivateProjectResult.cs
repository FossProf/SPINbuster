using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.ActivateProject;

public sealed record ActivateProjectResult(
  ProjectId ProjectId,
  string Name,
  ProjectLifecycle Lifecycle);
