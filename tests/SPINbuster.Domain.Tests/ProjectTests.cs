namespace SPINbuster.Domain.Tests;

public sealed class ProjectTests
{
  [Fact]
  public void ProjectStartsAsDraft()
  {
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));

    Assert.Equal(ProjectLifecycle.Draft, project.Lifecycle);
    Assert.Single(project.AuditTrail);
  }

  [Fact]
  public void ProjectAllowsValidLifecycleTransitions()
  {
    var project = CreateProject();

    project.Activate("owner@example.invalid", Timestamp(1));
    project.Complete("owner@example.invalid", Timestamp(2));
    project.Archive("owner@example.invalid", Timestamp(3));

    Assert.Equal(ProjectLifecycle.Archived, project.Lifecycle);
    Assert.Equal(4, project.AuditTrail.Count);
  }

  [Fact]
  public void ProjectRejectsInvalidLifecycleTransitions()
  {
    var project = CreateProject();

    Assert.Throws<LifecycleTransitionException>(() => project.Complete("owner@example.invalid", Timestamp(1)));

    project.Archive("owner@example.invalid", Timestamp(1));

    Assert.Throws<LifecycleTransitionException>(() => project.Activate("owner@example.invalid", Timestamp(2)));
  }

  private static Project CreateProject()
  {
    return new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      Timestamp(0));
  }

  private static DateTimeOffset Timestamp(int offsetHours)
  {
    return new DateTimeOffset(2026, 7, 15, 9 + offsetHours, 0, 0, TimeSpan.Zero);
  }
}
