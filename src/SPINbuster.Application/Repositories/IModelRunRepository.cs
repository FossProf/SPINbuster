using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IModelRunRepository
{
  Task<ModelRun?> GetByIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default);

  Task<ModelRun?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<ModelRunAttempt>> GetAttemptsAsync(
    ModelRunId modelRunId,
    CancellationToken cancellationToken = default);

  Task AddAsync(ModelRun modelRun, CancellationToken cancellationToken = default);

  Task UpdateAsync(ModelRun modelRun, CancellationToken cancellationToken = default);

  Task AddAttemptAsync(ModelRunAttempt attempt, CancellationToken cancellationToken = default);
}
