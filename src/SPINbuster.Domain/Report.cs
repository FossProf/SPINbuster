namespace SPINbuster.Domain;

public enum ReportLifecycle
{
  Draft = 0,
  UnderReview = 1,
  Approved = 2,
}

public sealed class Report : AuditableEntity
{
  public Report(
    ReportId id,
    ProjectId projectId,
    InspectionSessionId inspectionSessionId,
    string title,
    string body,
    string createdBy,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    InspectionSessionId = inspectionSessionId;
    Title = DomainGuards.NotNullOrWhiteSpace(title, nameof(title));
    Body = DomainGuards.NotNullOrWhiteSpace(body, nameof(body));
    CreatedBy = DomainGuards.NotNullOrWhiteSpace(createdBy, nameof(createdBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    Lifecycle = ReportLifecycle.Draft;

    AppendAuditEvent(CreateAuditEvent("ReportCreated", createdBy, createdAtUtc, "Report created as draft."));
  }

  public ReportId Id { get; }

  public ProjectId ProjectId { get; }

  public InspectionSessionId InspectionSessionId { get; }

  public string Title { get; private set; }

  public string Body { get; private set; }

  public string CreatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public ReportLifecycle Lifecycle { get; private set; }

  public string? ApprovedBy { get; private set; }

  public DateTimeOffset? ApprovedAtUtc { get; private set; }

  internal static Report Rehydrate(
    ReportId id,
    ProjectId projectId,
    InspectionSessionId inspectionSessionId,
    string title,
    string body,
    string createdBy,
    DateTimeOffset createdAtUtc,
    ReportLifecycle lifecycle,
    string? approvedBy,
    DateTimeOffset? approvedAtUtc,
    IEnumerable<AuditEvent> auditTrail)
  {
    var report = new Report(id, projectId, inspectionSessionId, title, body, createdBy, createdAtUtc)
    {
      Lifecycle = lifecycle,
      ApprovedBy = approvedBy,
      ApprovedAtUtc = approvedAtUtc,
    };

    report.RestoreAuditTrail(auditTrail);
    return report;
  }

  public void UpdateDraft(string title, string body, string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureLifecycle(ReportLifecycle.Draft, nameof(UpdateDraft));

    Title = DomainGuards.NotNullOrWhiteSpace(title, nameof(title));
    Body = DomainGuards.NotNullOrWhiteSpace(body, nameof(body));
    AppendAuditEvent(CreateAuditEvent("ReportDraftUpdated", actor, occurredAtUtc, "Report draft updated."));
  }

  public void SubmitForReview(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureLifecycle(ReportLifecycle.Draft, nameof(SubmitForReview));

    Lifecycle = ReportLifecycle.UnderReview;
    AppendAuditEvent(CreateAuditEvent("ReportSubmittedForReview", actor, occurredAtUtc, "Report submitted for review."));
  }

  public void ReturnToDraft(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureLifecycle(ReportLifecycle.UnderReview, nameof(ReturnToDraft));

    Lifecycle = ReportLifecycle.Draft;
    AppendAuditEvent(CreateAuditEvent("ReportReturnedToDraft", actor, occurredAtUtc, "Report returned to draft."));
  }

  public void Approve(string actor, DateTimeOffset occurredAtUtc)
  {
    // Approval is intentionally explicit and never inferred from draft updates
    // or review submission alone.
    EnsureLifecycle(ReportLifecycle.UnderReview, nameof(Approve));

    Lifecycle = ReportLifecycle.Approved;
    ApprovedBy = DomainGuards.NotNullOrWhiteSpace(actor, nameof(actor));
    ApprovedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("ReportApproved", actor, occurredAtUtc, "Report approved."));
  }

  private void EnsureLifecycle(ReportLifecycle expectedLifecycle, string transitionName)
  {
    if (Lifecycle != expectedLifecycle)
    {
      throw new LifecycleTransitionException(nameof(Report), Lifecycle.ToString(), transitionName);
    }
  }

  private AuditEvent CreateAuditEvent(
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(Report),
      Id.ToString(),
      eventType,
      actor,
      occurredAtUtc,
      description);
  }
}
