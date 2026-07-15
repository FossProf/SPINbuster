namespace SPINbuster.Domain;

public abstract class AuditableEntity
{
  private readonly List<AuditEvent> _auditTrail = [];

  // Aggregate audit trails are append-only. Consumers can inspect history but
  // cannot replace or reorder the recorded domain facts.
  public IReadOnlyList<AuditEvent> AuditTrail => _auditTrail.AsReadOnly();

  protected void AppendAuditEvent(AuditEvent auditEvent)
  {
    _auditTrail.Add(auditEvent);
  }

  internal void RestoreAuditTrail(IEnumerable<AuditEvent> auditTrail)
  {
    _auditTrail.Clear();
    _auditTrail.AddRange(auditTrail);
  }
}
