namespace SPINbuster.Domain.Tests;

public sealed class AuditEventTests
{
  [Fact]
  public void AuditEventIsImmutableByConstruction()
  {
    var occurredAtUtc = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
    var auditEvent = new AuditEvent(
      AuditEventId.New(),
      nameof(Project),
      ProjectId.New().ToString(),
      "ProjectCreated",
      "owner@example.invalid",
      occurredAtUtc,
      "Created.");

    Assert.Equal(nameof(Project), auditEvent.SubjectType);
    Assert.Equal("ProjectCreated", auditEvent.EventType);
    Assert.Equal(occurredAtUtc, auditEvent.OccurredAtUtc);
  }

  [Fact]
  public void AuditTrailIsAppendOnlyForAuditableEntities()
  {
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));

    project.Activate("owner@example.invalid", new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
    project.Complete("owner@example.invalid", new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero));

    Assert.Equal(3, project.AuditTrail.Count);
    Assert.Equal("ProjectCompleted", project.AuditTrail[^1].EventType);
  }
}
