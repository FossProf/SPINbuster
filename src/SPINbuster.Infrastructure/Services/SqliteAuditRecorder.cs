using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Services;

public sealed class SqliteAuditRecorder : IAuditRecorder
{
  private readonly List<AuditEventRecord> _stagedAuditEvents = [];

  public void Stage(AuditEvent auditEvent)
  {
    _stagedAuditEvents.Add(InfrastructureMapper.ToRecord(auditEvent));
  }

  internal IReadOnlyCollection<AuditEventRecord> ReleaseStagedAuditEvents()
  {
    var stagedAuditEvents = _stagedAuditEvents.ToArray();
    _stagedAuditEvents.Clear();
    return stagedAuditEvents;
  }
}
