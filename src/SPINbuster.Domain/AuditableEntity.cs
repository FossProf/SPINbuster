namespace SPINbuster.Domain;

public abstract class AuditableEntity
{
  private readonly List<AuditEvent> _auditTrail = [];

  // SubjectType and SubjectId are persisted audit contracts. Derived
  // aggregates declare stable string constants so renaming a C# type
  // never changes historical audit semantics.
  protected abstract string SubjectType { get; }

  protected abstract string SubjectId { get; }

  // Aggregate audit trails are append-only. Consumers can inspect history but
  // cannot replace or reorder the recorded domain facts.
  public IReadOnlyList<AuditEvent> AuditTrail => _auditTrail.AsReadOnly();

  // Centralizes mechanical audit-event construction: ID generation,
  // subject identity stamping, and parameter forwarding. Aggregates
  // remain responsible for deciding when an event is warranted, what
  // event type to record, and what description to include.
  protected AuditEvent CreateAuditEvent(
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      SubjectType,
      SubjectId,
      eventType,
      actor,
      occurredAtUtc,
      description);
  }

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
