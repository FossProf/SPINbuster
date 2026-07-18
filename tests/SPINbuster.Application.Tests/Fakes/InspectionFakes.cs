using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeInspectionSessionRepository : IInspectionSessionRepository
{
  private readonly Dictionary<InspectionSessionId, InspectionSession> _inspectionSessions = [];

  public Task<InspectionSession?> GetByIdAsync(
    InspectionSessionId inspectionSessionId,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions.TryGetValue(inspectionSessionId, out var inspectionSession);
    return Task.FromResult(inspectionSession);
  }

  public Task AddAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions[inspectionSession.Id] = inspectionSession;
    return Task.CompletedTask;
  }

  public List<InspectionSession> UpdatedInspectionSessions { get; } = [];

  public Task UpdateAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions[inspectionSession.Id] = inspectionSession;
    UpdatedInspectionSessions.Add(inspectionSession);
    return Task.CompletedTask;
  }
}
