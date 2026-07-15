namespace SPINbuster.Domain.Tests;

public sealed class SaveTransactionTests
{
  [Fact]
  public void SaveTransactionStartsCreated()
  {
    var transaction = CreateTransaction();

    Assert.Equal(SaveTransactionState.Created, transaction.State);
    Assert.Single(transaction.AuditTrail);
  }

  [Fact]
  public void SaveTransactionAllowsValidStateFlow()
  {
    var transaction = CreateTransaction();

    transaction.Prepare("system@example.invalid", Timestamp(1));
    transaction.Persist("system@example.invalid", Timestamp(2));
    transaction.Commit("system@example.invalid", Timestamp(3));

    Assert.Equal(SaveTransactionState.Committed, transaction.State);
    Assert.Equal(Timestamp(3), transaction.CompletedAtUtc);
    Assert.Equal(4, transaction.AuditTrail.Count);
  }

  [Fact]
  public void SaveTransactionRejectsInvalidTransitions()
  {
    var transaction = CreateTransaction();

    Assert.Throws<LifecycleTransitionException>(() => transaction.Commit("system@example.invalid", Timestamp(1)));

    transaction.Prepare("system@example.invalid", Timestamp(2));
    transaction.Abort("system@example.invalid", Timestamp(3), "Operator cancelled.");

    Assert.Throws<LifecycleTransitionException>(() => transaction.Persist("system@example.invalid", Timestamp(4)));
    Assert.Throws<LifecycleTransitionException>(() => transaction.Fail("system@example.invalid", Timestamp(5), "Too late."));
  }

  [Fact]
  public void SaveTransactionCapturesFailureReason()
  {
    var transaction = CreateTransaction();
    transaction.Prepare("system@example.invalid", Timestamp(1));
    transaction.Fail("system@example.invalid", Timestamp(2), "Write conflict.");

    Assert.Equal(SaveTransactionState.Failed, transaction.State);
    Assert.Equal("Write conflict.", transaction.FailureReason);
  }

  private static SaveTransaction CreateTransaction()
  {
    return new SaveTransaction(
      SaveTransactionId.New(),
      ReportId.New(),
      "system@example.invalid",
      Timestamp(0));
  }

  private static DateTimeOffset Timestamp(int offsetHours)
  {
    return new DateTimeOffset(2026, 7, 15, 9 + offsetHours, 0, 0, TimeSpan.Zero);
  }
}
