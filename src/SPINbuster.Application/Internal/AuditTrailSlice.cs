using SPINbuster.Domain;

namespace SPINbuster.Application.Internal;

internal static class AuditTrailSlice
{
  public static IReadOnlyCollection<AuditEvent> GetNewEvents(
    AuditableEntity aggregate,
    int initialCount)
  {
    // Application handlers record only the audit facts created by the current
    // use-case transition so downstream audit storage does not duplicate history.
    return aggregate.AuditTrail.Skip(initialCount).ToArray();
  }
}
