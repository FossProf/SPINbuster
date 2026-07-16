using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IAuditEventQueryRepository
{
  Task<IReadOnlyCollection<AuditEvent>> GetBySubjectAsync(
    string subjectType,
    string subjectId,
    CancellationToken cancellationToken = default);
}
