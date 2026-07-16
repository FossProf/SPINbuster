namespace SPINbuster.Domain.Tests;

public sealed class ReportTests
{
  [Fact]
  public void ReportStartsAsDraftWithRevisionOneAndSourceProvenance()
  {
    var report = CreateReport();

    Assert.Equal(ReportLifecycle.Draft, report.Lifecycle);
    Assert.Equal(1, report.RevisionNumber);
    Assert.Null(report.ApprovedAtUtc);
    Assert.Null(report.ApprovedBy);
    Assert.Single(report.SourceFieldNoteIds);
    Assert.Single(report.SourceEvidenceAttachmentIds);
    Assert.Single(report.AuditTrail);
  }

  [Fact]
  public void ReportCannotBeApprovedWithoutExplicitReviewFlow()
  {
    var report = CreateReport();

    Assert.Throws<LifecycleTransitionException>(() => report.Approve("approver@example.invalid", Timestamp(1)));
  }

  [Fact]
  public void ReportCanBeReviewedReturnedAndApprovedThroughExplicitOperations()
  {
    var report = CreateReport();

    report.UpdateDraft(
      new ReportTitle("Updated Title"),
      [new ReportDraftSection("Updated Summary", "Updated draft content.")],
      "author@example.invalid",
      Timestamp(1));
    report.SubmitForReview("author@example.invalid", Timestamp(2));
    report.ReturnToDraft("reviewer@example.invalid", Timestamp(3));
    report.SubmitForReview("author@example.invalid", Timestamp(4));
    report.Approve("approver@example.invalid", Timestamp(5));

    Assert.Equal(ReportLifecycle.Approved, report.Lifecycle);
    Assert.Equal("approver@example.invalid", report.ApprovedBy);
    Assert.Equal(Timestamp(5), report.ApprovedAtUtc);
    Assert.Equal(2, report.RevisionNumber);
    Assert.Equal(6, report.AuditTrail.Count);
  }

  [Fact]
  public void ReportRejectsDraftUpdatesOutsideDraftState()
  {
    var report = CreateReport();
    report.SubmitForReview("author@example.invalid", Timestamp(1));

    Assert.Throws<LifecycleTransitionException>(() => report.UpdateDraft(
      new ReportTitle("Updated Title"),
      [new ReportDraftSection("Updated Summary", "Updated draft content.")],
      "author@example.invalid",
      Timestamp(2)));
  }

  [Fact]
  public void ReportRequiresAtLeastOneSourceReference()
  {
    Assert.Throws<DomainInvariantException>(() => new Report(
      ReportId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      new ReportTitle("Draft Report"),
      [new ReportDraftSection("Summary", "Findings pending review.")],
      [],
      [],
      "author@example.invalid",
      Timestamp(0)));
  }

  [Fact]
  public void ReportRejectsDuplicateFieldNoteSourceReferences()
  {
    var fieldNoteId = FieldNoteId.New();

    Assert.Throws<DomainInvariantException>(() => new Report(
      ReportId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      new ReportTitle("Draft Report"),
      [new ReportDraftSection("Summary", "Findings pending review.")],
      [fieldNoteId, fieldNoteId],
      [],
      "author@example.invalid",
      Timestamp(0)));
  }

  [Fact]
  public void ReportRejectsDuplicateEvidenceSourceReferences()
  {
    var evidenceAttachmentId = EvidenceAttachmentId.New();

    Assert.Throws<DomainInvariantException>(() => new Report(
      ReportId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      new ReportTitle("Draft Report"),
      [new ReportDraftSection("Summary", "Findings pending review.")],
      [],
      [evidenceAttachmentId, evidenceAttachmentId],
      "author@example.invalid",
      Timestamp(0)));
  }

  private static Report CreateReport()
  {
    return new Report(
      ReportId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      new ReportTitle("Draft Report"),
      [new ReportDraftSection("Summary", "Findings pending review.")],
      [FieldNoteId.New()],
      [EvidenceAttachmentId.New()],
      "author@example.invalid",
      Timestamp(0));
  }

  private static DateTimeOffset Timestamp(int offsetHours)
  {
    return new DateTimeOffset(2026, 7, 15, 9 + offsetHours, 0, 0, TimeSpan.Zero);
  }
}
