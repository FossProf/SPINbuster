namespace SPINbuster.Domain;

public enum InspectionSessionLifecycle
{
  Planned = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
}

public sealed class InspectionSession : AuditableEntity
{
  private const string AuditSubjectType = "InspectionSession";

  private readonly List<FieldNote> _fieldNotes = [];
  private readonly List<EvidenceAttachment> _evidenceAttachments = [];

  public InspectionSession(
    InspectionSessionId id,
    ProjectId projectId,
    string name,
    string createdBy,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    Name = DomainGuards.NotNullOrWhiteSpace(name, nameof(name));
    CreatedBy = DomainGuards.NotNullOrWhiteSpace(createdBy, nameof(createdBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    Lifecycle = InspectionSessionLifecycle.Planned;

    AppendAuditEvent(CreateAuditEvent("InspectionSessionCreated", createdBy, createdAtUtc, "Inspection session created."));
  }

  public InspectionSessionId Id { get; }

  public ProjectId ProjectId { get; }

  public string Name { get; }

  public string CreatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public InspectionSessionLifecycle Lifecycle { get; private set; }

  public DateTimeOffset? StartedAtUtc { get; private set; }

  public DateTimeOffset? CompletedAtUtc { get; private set; }

  protected override string SubjectType => AuditSubjectType;

  protected override string SubjectId => Id.ToString();

  // InspectionSession owns the mutable child collections for notes and
  // evidence. Other aggregates should link back to the session by identifier.
  public IReadOnlyList<FieldNote> FieldNotes => _fieldNotes.AsReadOnly();

  public IReadOnlyList<EvidenceAttachment> EvidenceAttachments => _evidenceAttachments.AsReadOnly();

  internal static InspectionSession Rehydrate(
    InspectionSessionId id,
    ProjectId projectId,
    string name,
    string createdBy,
    DateTimeOffset createdAtUtc,
    InspectionSessionLifecycle lifecycle,
    DateTimeOffset? startedAtUtc,
    DateTimeOffset? completedAtUtc,
    IEnumerable<FieldNote> fieldNotes,
    IEnumerable<EvidenceAttachment> evidenceAttachments,
    IEnumerable<AuditEvent> auditTrail)
  {
    var inspectionSession = new InspectionSession(id, projectId, name, createdBy, createdAtUtc)
    {
      Lifecycle = lifecycle,
      StartedAtUtc = startedAtUtc,
      CompletedAtUtc = completedAtUtc,
    };

    inspectionSession._fieldNotes.AddRange(fieldNotes);
    inspectionSession._evidenceAttachments.AddRange(evidenceAttachments);
    inspectionSession.RestoreAuditTrail(auditTrail);
    return inspectionSession;
  }

  public void Start(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureLifecycle(InspectionSessionLifecycle.Planned, nameof(Start));

    Lifecycle = InspectionSessionLifecycle.InProgress;
    StartedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("InspectionSessionStarted", actor, occurredAtUtc, "Inspection session started."));
  }

  public void Complete(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureLifecycle(InspectionSessionLifecycle.InProgress, nameof(Complete));

    Lifecycle = InspectionSessionLifecycle.Completed;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("InspectionSessionCompleted", actor, occurredAtUtc, "Inspection session completed."));
  }

  public void Cancel(string actor, DateTimeOffset occurredAtUtc)
  {
    if (Lifecycle is InspectionSessionLifecycle.Completed or InspectionSessionLifecycle.Cancelled)
    {
      throw new LifecycleTransitionException(nameof(InspectionSession), Lifecycle.ToString(), nameof(Cancel));
    }

    Lifecycle = InspectionSessionLifecycle.Cancelled;
    AppendAuditEvent(CreateAuditEvent("InspectionSessionCancelled", actor, occurredAtUtc, "Inspection session cancelled."));
  }

  public FieldNote RecordFieldNote(
    FieldNoteId id,
    string capturedBy,
    DateTimeOffset capturedAtUtc,
    FieldNoteRawText rawText)
  {
    EnsureLifecycle(InspectionSessionLifecycle.InProgress, nameof(RecordFieldNote));
    EnsureFieldNoteIdIsUnique(id);

    var fieldNote = new FieldNote(id, Id, capturedBy, capturedAtUtc, rawText);
    _fieldNotes.Add(fieldNote);

    AppendAuditEvent(CreateAuditEvent("FieldNoteRecorded", capturedBy, capturedAtUtc, $"Field note {fieldNote.Id} recorded."));
    return fieldNote;
  }

  public EvidenceAttachment AttachEvidence(
    EvidenceAttachmentId id,
    string capturedBy,
    DateTimeOffset capturedAtUtc,
    RawEvidenceReference rawEvidence)
  {
    EnsureLifecycle(InspectionSessionLifecycle.InProgress, nameof(AttachEvidence));
    EnsureEvidenceAttachmentIdIsUnique(id);

    var attachment = new EvidenceAttachment(id, Id, capturedBy, capturedAtUtc, rawEvidence);
    _evidenceAttachments.Add(attachment);

    AppendAuditEvent(CreateAuditEvent("EvidenceAttached", capturedBy, capturedAtUtc, $"Evidence attachment {attachment.Id} recorded."));
    return attachment;
  }

  public void InterpretEvidence(
    EvidenceAttachmentId evidenceAttachmentId,
    EvidenceInterpretation interpretation)
  {
    if (Lifecycle is not (InspectionSessionLifecycle.InProgress or InspectionSessionLifecycle.Completed))
    {
      throw new LifecycleTransitionException(nameof(InspectionSession), Lifecycle.ToString(), nameof(InterpretEvidence));
    }

    var attachment = _evidenceAttachments.SingleOrDefault(item => item.Id == evidenceAttachmentId);
    if (attachment is null)
    {
      throw new DomainInvariantException($"Evidence attachment {evidenceAttachmentId} was not found.");
    }

    attachment.ApplyInterpretation(interpretation);
    AppendAuditEvent(CreateAuditEvent("EvidenceInterpreted", interpretation.InterpretedBy, interpretation.InterpretedAtUtc, $"Evidence attachment {attachment.Id} interpreted."));
  }

  private void EnsureLifecycle(InspectionSessionLifecycle expectedLifecycle, string transitionName)
  {
    if (Lifecycle != expectedLifecycle)
    {
      throw new LifecycleTransitionException(nameof(InspectionSession), Lifecycle.ToString(), transitionName);
    }
  }

  private void EnsureFieldNoteIdIsUnique(FieldNoteId id)
  {
    if (_fieldNotes.Any(item => item.Id == id))
    {
      throw new DomainInvariantException($"Field note {id} is already recorded for inspection session {Id}.");
    }
  }

  private void EnsureEvidenceAttachmentIdIsUnique(EvidenceAttachmentId id)
  {
    if (_evidenceAttachments.Any(item => item.Id == id))
    {
      throw new DomainInvariantException($"Evidence attachment {id} is already recorded for inspection session {Id}.");
    }
  }
}
