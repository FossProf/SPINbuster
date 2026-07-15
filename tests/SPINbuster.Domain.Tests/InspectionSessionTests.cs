namespace SPINbuster.Domain.Tests;

public sealed class InspectionSessionTests
{
  [Fact]
  public void InspectionSessionStartsPlanned()
  {
    var session = CreateSession();

    Assert.Equal(InspectionSessionLifecycle.Planned, session.Lifecycle);
    Assert.Single(session.AuditTrail);
  }

  [Fact]
  public void InspectionSessionRequiresInProgressStateForFieldNotesAndEvidence()
  {
    var session = CreateSession();

    Assert.Throws<LifecycleTransitionException>(() => session.RecordFieldNote(
      FieldNoteId.New(),
      "inspector@example.invalid",
      Timestamp(1),
      new FieldNoteRawText("raw note")));

    Assert.Throws<LifecycleTransitionException>(() => session.AttachEvidence(
      EvidenceAttachmentId.New(),
      "inspector@example.invalid",
      Timestamp(1),
      new RawEvidenceReference("photo.jpg", "image/jpeg", "key", "hash")));
  }

  [Fact]
  public void InspectionSessionCapturesFieldNotesAndEvidenceWhileInProgress()
  {
    var session = CreateSession();
    session.Start("inspector@example.invalid", Timestamp(1));

    var note = session.RecordFieldNote(
      FieldNoteId.New(),
      "inspector@example.invalid",
      Timestamp(2),
      new FieldNoteRawText("Observed leak at gasket seam."));

    var attachment = session.AttachEvidence(
      EvidenceAttachmentId.New(),
      "inspector@example.invalid",
      Timestamp(3),
      new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));

    session.InterpretEvidence(
      attachment.Id,
      new EvidenceInterpretation("Leak visible in lower-right quadrant.", "reviewer@example.invalid", Timestamp(4)));

    Assert.Single(session.FieldNotes);
    Assert.Same(note, session.FieldNotes[0]);
    Assert.Single(session.EvidenceAttachments);
    Assert.NotNull(session.EvidenceAttachments[0].Interpretation);
    Assert.Equal(5, session.AuditTrail.Count);
  }

  [Fact]
  public void InspectionSessionRejectsDuplicateChildIdentifiers()
  {
    var session = CreateSession();
    session.Start("inspector@example.invalid", Timestamp(1));

    var fieldNoteId = FieldNoteId.New();
    var evidenceAttachmentId = EvidenceAttachmentId.New();

    session.RecordFieldNote(
      fieldNoteId,
      "inspector@example.invalid",
      Timestamp(2),
      new FieldNoteRawText("Observed leak at gasket seam."));

    session.AttachEvidence(
      evidenceAttachmentId,
      "inspector@example.invalid",
      Timestamp(3),
      new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));

    Assert.Throws<DomainInvariantException>(() => session.RecordFieldNote(
      fieldNoteId,
      "inspector@example.invalid",
      Timestamp(4),
      new FieldNoteRawText("Duplicate note id.")));

    Assert.Throws<DomainInvariantException>(() => session.AttachEvidence(
      evidenceAttachmentId,
      "inspector@example.invalid",
      Timestamp(5),
      new RawEvidenceReference("photo-2.jpg", "image/jpeg", "evidence/photo-2.jpg", "sha256:ghi")));
  }

  [Fact]
  public void InspectionSessionAllowsCompletionAfterStart()
  {
    var session = CreateSession();
    session.Start("inspector@example.invalid", Timestamp(1));
    session.Complete("inspector@example.invalid", Timestamp(2));

    Assert.Equal(InspectionSessionLifecycle.Completed, session.Lifecycle);
    Assert.Equal(Timestamp(2), session.CompletedAtUtc);
  }

  [Fact]
  public void InspectionSessionRejectsInvalidLifecycleTransitions()
  {
    var session = CreateSession();

    Assert.Throws<LifecycleTransitionException>(() => session.Complete("inspector@example.invalid", Timestamp(1)));

    session.Cancel("inspector@example.invalid", Timestamp(2));

    Assert.Throws<LifecycleTransitionException>(() => session.Start("inspector@example.invalid", Timestamp(3)));
    Assert.Throws<LifecycleTransitionException>(() => session.InterpretEvidence(
      EvidenceAttachmentId.New(),
      new EvidenceInterpretation("Not used", "reviewer@example.invalid", Timestamp(4))));
  }

  private static InspectionSession CreateSession()
  {
    return new InspectionSession(
      InspectionSessionId.New(),
      ProjectId.New(),
      "Initial Walkdown",
      "inspector@example.invalid",
      Timestamp(0));
  }

  private static DateTimeOffset Timestamp(int offsetHours)
  {
    return new DateTimeOffset(2026, 7, 15, 9 + offsetHours, 0, 0, TimeSpan.Zero);
  }
}
