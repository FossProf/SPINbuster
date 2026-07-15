namespace SPINbuster.Domain;

public sealed class AuditEvent
{
  // Audit events intentionally capture only domain-relevant facts. Broader
  // execution metadata such as correlation IDs or IP addresses belongs above
  // the Domain layer.
  public AuditEvent(
    AuditEventId id,
    string subjectType,
    string subjectId,
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    Id = id;
    SubjectType = DomainGuards.NotNullOrWhiteSpace(subjectType, nameof(subjectType));
    SubjectId = DomainGuards.NotNullOrWhiteSpace(subjectId, nameof(subjectId));
    EventType = DomainGuards.NotNullOrWhiteSpace(eventType, nameof(eventType));
    Actor = DomainGuards.NotNullOrWhiteSpace(actor, nameof(actor));
    OccurredAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    Description = DomainGuards.NotNullOrWhiteSpace(description, nameof(description));
  }

  public AuditEventId Id { get; }

  public string SubjectType { get; }

  public string SubjectId { get; }

  public string EventType { get; }

  public string Actor { get; }

  public DateTimeOffset OccurredAtUtc { get; }

  public string Description { get; }
}
