using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IInspectionSessionRepository
{
  Task<InspectionSession?> GetByIdAsync(
    InspectionSessionId inspectionSessionId,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default);

  Task UpdateAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default);
}
