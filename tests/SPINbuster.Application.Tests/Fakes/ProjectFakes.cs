using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeProjectRepository : IProjectRepository
{
  private readonly Dictionary<ProjectId, Project> _projects = [];

  public Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken = default)
  {
    _projects.TryGetValue(projectId, out var project);
    return Task.FromResult(project);
  }

  public Task AddAsync(Project project, CancellationToken cancellationToken = default)
  {
    _projects[project.Id] = project;
    return Task.CompletedTask;
  }

  public List<Project> UpdatedProjects { get; } = [];

  public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
  {
    _projects[project.Id] = project;
    UpdatedProjects.Add(project);
    return Task.CompletedTask;
  }
}
