namespace SPINbuster.Domain.Tests;

public sealed class FieldNoteAndEvidenceTests
{
  [Fact]
  public void FieldNotePreservesRawTextExactly()
  {
    const string rawText = "  observed pressure drop at valve A  ";
    var note = new FieldNote(
      FieldNoteId.New(),
      InspectionSessionId.New(),
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero),
      new FieldNoteRawText(rawText));

    Assert.Equal(rawText, note.RawText.Value);
  }

  [Fact]
  public void EvidenceAttachmentKeepsRawEvidenceSeparateFromInterpretation()
  {
    var session = new InspectionSession(
      InspectionSessionId.New(),
      ProjectId.New(),
      "Initial Walkdown",
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero));

    session.Start("inspector@example.invalid", new DateTimeOffset(2026, 7, 15, 11, 5, 0, TimeSpan.Zero));

    var attachment = session.AttachEvidence(
      EvidenceAttachmentId.New(),
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero),
      new RawEvidenceReference("photo-01.jpg", "image/jpeg", "evidence/photo-01.jpg", "sha256:abc"));

    session.InterpretEvidence(
      attachment.Id,
      new EvidenceInterpretation(
        "Corrosion visible near seam.",
        "reviewer@example.invalid",
        new DateTimeOffset(2026, 7, 15, 13, 0, 0, TimeSpan.Zero)));

    Assert.Equal("evidence/photo-01.jpg", attachment.RawEvidence.StorageKey);
    Assert.NotNull(attachment.Interpretation);
    Assert.Equal("Corrosion visible near seam.", attachment.Interpretation!.Summary);
  }

  [Fact]
  public void EvidenceAttachmentRejectsReplacingAnExistingInterpretation()
  {
    var session = new InspectionSession(
      InspectionSessionId.New(),
      ProjectId.New(),
      "Initial Walkdown",
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero));

    session.Start("inspector@example.invalid", new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero));

    var attachment = session.AttachEvidence(
      EvidenceAttachmentId.New(),
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 12, 5, 0, TimeSpan.Zero),
      new RawEvidenceReference("photo-01.jpg", "image/jpeg", "evidence/photo-01.jpg", "sha256:abc"));

    session.InterpretEvidence(
      attachment.Id,
      new EvidenceInterpretation(
        "Corrosion visible near seam.",
        "reviewer@example.invalid",
        new DateTimeOffset(2026, 7, 15, 13, 0, 0, TimeSpan.Zero)));

    Assert.Throws<DomainInvariantException>(() => session.InterpretEvidence(
      attachment.Id,
      new EvidenceInterpretation(
        "Replacement interpretation.",
        "reviewer@example.invalid",
        new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero))));
  }
}
