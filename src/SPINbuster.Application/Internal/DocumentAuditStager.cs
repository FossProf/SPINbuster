using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;

namespace SPINbuster.Application.Internal;

internal static class DocumentAuditStager
{
  public static void Stage(IAuditRecorder auditRecorder, IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      auditRecorder.Stage(auditEvent);
    }
  }
}
