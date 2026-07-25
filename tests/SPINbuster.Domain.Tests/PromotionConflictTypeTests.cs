namespace SPINbuster.Domain.Tests;

public sealed class PromotionConflictTypeTests
{
  [Fact]
  public void NoneHasValueZero()
  {
    Assert.Equal(0, (int)PromotionConflictType.None);
  }

  [Fact]
  public void AmbiguousDocumentMatchHasValueOne()
  {
    Assert.Equal(1, (int)PromotionConflictType.AmbiguousDocumentMatch);
  }

  [Fact]
  public void HigherAuthorityExistsHasValueTwo()
  {
    Assert.Equal(2, (int)PromotionConflictType.HigherAuthorityExists);
  }

  [Fact]
  public void ConcurrentPromotionHasValueThree()
  {
    Assert.Equal(3, (int)PromotionConflictType.ConcurrentPromotion);
  }

  [Fact]
  public void TemporalOrderViolationHasValueFour()
  {
    Assert.Equal(4, (int)PromotionConflictType.TemporalOrderViolation);
  }
}
