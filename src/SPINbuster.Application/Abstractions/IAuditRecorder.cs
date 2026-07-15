using SPINbuster.Domain;

namespace SPINbuster.Application.Abstractions;

public interface IAuditRecorder
{
  void Stage(AuditEvent auditEvent);
}
