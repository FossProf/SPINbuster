using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IProjectRepository
{
  Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken = default);

  Task AddAsync(Project project, CancellationToken cancellationToken = default);

  Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
}
