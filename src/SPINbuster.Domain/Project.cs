namespace SPINbuster.Domain;

public enum ProjectLifecycle
{
  Draft = 0,
  Active = 1,
  Completed = 2,
  Archived = 3,
}

public sealed class Project : AuditableEntity
{
  private const string AuditSubjectType = "Project";

  public Project(ProjectId id, string name, string createdBy, DateTimeOffset createdAtUtc)
  {
    Id = id;
    Name = DomainGuards.NotNullOrWhiteSpace(name, nameof(name));
    CreatedBy = DomainGuards.NotNullOrWhiteSpace(createdBy, nameof(createdBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    Lifecycle = ProjectLifecycle.Draft;

    AppendAuditEvent(CreateAuditEvent("ProjectCreated", createdBy, createdAtUtc, "Project created."));
  }

  public ProjectId Id { get; }

  public string Name { get; }

  public string CreatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public ProjectLifecycle Lifecycle { get; private set; }

  protected override string SubjectType => AuditSubjectType;

  protected override string SubjectId => Id.ToString();

  internal static Project Rehydrate(
    ProjectId id,
    string name,
    string createdBy,
    DateTimeOffset createdAtUtc,
    ProjectLifecycle lifecycle,
    IEnumerable<AuditEvent> auditTrail)
  {
    var project = new Project(id, name, createdBy, createdAtUtc)
    {
      Lifecycle = lifecycle,
    };

    project.RestoreAuditTrail(auditTrail);
    return project;
  }

  public void Activate(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureTransition(ProjectLifecycle.Draft, nameof(Activate));
    Lifecycle = ProjectLifecycle.Active;
    AppendAuditEvent(CreateAuditEvent("ProjectActivated", actor, occurredAtUtc, "Project activated."));
  }

  public void Complete(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureTransition(ProjectLifecycle.Active, nameof(Complete));
    Lifecycle = ProjectLifecycle.Completed;
    AppendAuditEvent(CreateAuditEvent("ProjectCompleted", actor, occurredAtUtc, "Project completed."));
  }

  public void Archive(string actor, DateTimeOffset occurredAtUtc)
  {
    // Draft projects may be archived when abandoned before activation, while
    // completed projects may be archived as a closed historical record.
    if (Lifecycle is not (ProjectLifecycle.Draft or ProjectLifecycle.Completed))
    {
      throw new LifecycleTransitionException(nameof(Project), Lifecycle.ToString(), nameof(Archive));
    }

    Lifecycle = ProjectLifecycle.Archived;
    AppendAuditEvent(CreateAuditEvent("ProjectArchived", actor, occurredAtUtc, "Project archived."));
  }

  private void EnsureTransition(ProjectLifecycle expectedState, string transitionName)
  {
    if (Lifecycle != expectedState)
    {
      throw new LifecycleTransitionException(nameof(Project), Lifecycle.ToString(), transitionName);
    }
  }
}
