namespace SPINbuster.Domain;

public enum ReportLifecycle
{
  Draft = 0,
  UnderReview = 1,
  Approved = 2,
}

public sealed class Report : AuditableEntity
{
  private const string AuditSubjectType = "Report";

  private readonly List<ReportDraftSection> _sections = [];
  private readonly List<FieldNoteId> _sourceFieldNoteIds = [];
  private readonly List<EvidenceAttachmentId> _sourceEvidenceAttachmentIds = [];

  public Report(
    ReportId id,
    ProjectId projectId,
    InspectionSessionId inspectionSessionId,
    ReportTitle title,
    IEnumerable<ReportDraftSection> sections,
    IEnumerable<FieldNoteId> sourceFieldNoteIds,
    IEnumerable<EvidenceAttachmentId> sourceEvidenceAttachmentIds,
    string createdBy,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    InspectionSessionId = inspectionSessionId;
    Title = title ?? throw new DomainInvariantException($"{nameof(title)} must be provided.");
    CreatedBy = DomainGuards.NotNullOrWhiteSpace(createdBy, nameof(createdBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    Lifecycle = ReportLifecycle.Draft;
    RevisionNumber = 1;

    _sections.AddRange(CreateSections(sections));
    _sourceFieldNoteIds.AddRange(CreateUniqueFieldNoteSourceIds(sourceFieldNoteIds));
    _sourceEvidenceAttachmentIds.AddRange(CreateUniqueEvidenceSourceIds(sourceEvidenceAttachmentIds));

    if (_sourceFieldNoteIds.Count == 0 && _sourceEvidenceAttachmentIds.Count == 0)
    {
      throw new DomainInvariantException("A report draft must reference at least one field note or evidence attachment.");
    }

    AppendAuditEvent(CreateAuditEvent(
      "ReportCreated",
      createdBy,
      createdAtUtc,
      $"Report created as draft revision {RevisionNumber}."));
  }

  public ReportId Id { get; }

  public ProjectId ProjectId { get; }

  public InspectionSessionId InspectionSessionId { get; }

  public ReportTitle Title { get; private set; }

  public int RevisionNumber { get; private set; }

  public IReadOnlyList<ReportDraftSection> Sections => _sections.AsReadOnly();

  public IReadOnlyList<FieldNoteId> SourceFieldNoteIds => _sourceFieldNoteIds.AsReadOnly();

  public IReadOnlyList<EvidenceAttachmentId> SourceEvidenceAttachmentIds => _sourceEvidenceAttachmentIds.AsReadOnly();

  public string CreatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public ReportLifecycle Lifecycle { get; private set; }

  public string? ApprovedBy { get; private set; }

  public DateTimeOffset? ApprovedAtUtc { get; private set; }

  protected override string SubjectType => AuditSubjectType;

  protected override string SubjectId => Id.ToString();

  internal static Report Rehydrate(
    ReportId id,
    ProjectId projectId,
    InspectionSessionId inspectionSessionId,
    string title,
    int revisionNumber,
    IEnumerable<ReportDraftSection> sections,
    IEnumerable<FieldNoteId> sourceFieldNoteIds,
    IEnumerable<EvidenceAttachmentId> sourceEvidenceAttachmentIds,
    string createdBy,
    DateTimeOffset createdAtUtc,
    ReportLifecycle lifecycle,
    string? approvedBy,
    DateTimeOffset? approvedAtUtc,
    IEnumerable<AuditEvent> auditTrail)
  {
    if (revisionNumber < 1)
    {
      throw new DomainInvariantException("Report revision number must be at least 1.");
    }

    var report = new Report(
      id,
      projectId,
      inspectionSessionId,
      new ReportTitle(title),
      sections,
      sourceFieldNoteIds,
      sourceEvidenceAttachmentIds,
      createdBy,
      createdAtUtc)
    {
      Lifecycle = lifecycle,
      RevisionNumber = revisionNumber,
      ApprovedBy = approvedBy,
      ApprovedAtUtc = approvedAtUtc,
    };

    report.RestoreAuditTrail(auditTrail);
    return report;
  }

  public void UpdateDraft(
    ReportTitle title,
    IEnumerable<ReportDraftSection> sections,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    EnsureLifecycle(ReportLifecycle.Draft, nameof(UpdateDraft));

    Title = title ?? throw new DomainInvariantException($"{nameof(title)} must be provided.");
    _sections.Clear();
    _sections.AddRange(CreateSections(sections));
    RevisionNumber++;
    AppendAuditEvent(CreateAuditEvent(
      "ReportDraftUpdated",
      actor,
      occurredAtUtc,
      $"Report draft updated to revision {RevisionNumber}."));
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

  private static ReportDraftSection[] CreateSections(IEnumerable<ReportDraftSection> sections)
  {
    var materializedSections = sections?.ToArray()
      ?? throw new DomainInvariantException($"{nameof(sections)} must be provided.");

    if (materializedSections.Length == 0)
    {
      throw new DomainInvariantException("At least one report draft section must be provided.");
    }

    return materializedSections;
  }

  private static FieldNoteId[] CreateUniqueFieldNoteSourceIds(IEnumerable<FieldNoteId> sourceFieldNoteIds)
  {
    var materializedIds = sourceFieldNoteIds?.ToArray()
      ?? throw new DomainInvariantException($"{nameof(sourceFieldNoteIds)} must be provided.");
    var distinctIds = materializedIds.Distinct().ToArray();
    if (distinctIds.Length != materializedIds.Length)
    {
      throw new DomainInvariantException("Duplicate field-note source references are not allowed.");
    }

    return distinctIds;
  }

  private static EvidenceAttachmentId[] CreateUniqueEvidenceSourceIds(IEnumerable<EvidenceAttachmentId> sourceEvidenceAttachmentIds)
  {
    var materializedIds = sourceEvidenceAttachmentIds?.ToArray()
      ?? throw new DomainInvariantException($"{nameof(sourceEvidenceAttachmentIds)} must be provided.");
    var distinctIds = materializedIds.Distinct().ToArray();
    if (distinctIds.Length != materializedIds.Length)
    {
      throw new DomainInvariantException("Duplicate evidence source references are not allowed.");
    }

    return distinctIds;
  }
}
