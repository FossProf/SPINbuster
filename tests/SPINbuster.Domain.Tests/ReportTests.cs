namespace SPINbuster.Domain.Tests;

public sealed class ReportTests
{
  [Fact]
  public void ReportStartsAsDraft()
  {
    var report = CreateReport();

    Assert.Equal(ReportLifecycle.Draft, report.Lifecycle);
    Assert.Null(report.ApprovedAtUtc);
    Assert.Null(report.ApprovedBy);
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

    report.UpdateDraft("Updated Title", "Updated Body", "author@example.invalid", Timestamp(1));
    report.SubmitForReview("author@example.invalid", Timestamp(2));
    report.ReturnToDraft("reviewer@example.invalid", Timestamp(3));
    report.SubmitForReview("author@example.invalid", Timestamp(4));
    report.Approve("approver@example.invalid", Timestamp(5));

    Assert.Equal(ReportLifecycle.Approved, report.Lifecycle);
    Assert.Equal("approver@example.invalid", report.ApprovedBy);
    Assert.Equal(Timestamp(5), report.ApprovedAtUtc);
    Assert.Equal(6, report.AuditTrail.Count);
  }

  [Fact]
  public void ReportRejectsDraftUpdatesOutsideDraftState()
  {
    var report = CreateReport();
    report.SubmitForReview("author@example.invalid", Timestamp(1));

    Assert.Throws<LifecycleTransitionException>(() => report.UpdateDraft(
      "Updated Title",
      "Updated Body",
      "author@example.invalid",
      Timestamp(2)));
  }

  private static Report CreateReport()
  {
    return new Report(
      ReportId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      "Draft Report",
      "Findings pending review.",
      "author@example.invalid",
      Timestamp(0));
  }

  private static DateTimeOffset Timestamp(int offsetHours)
  {
    return new DateTimeOffset(2026, 7, 15, 9 + offsetHours, 0, 0, TimeSpan.Zero);
  }
}
