using SPINbuster.Application.Logging;

namespace SPINbuster.Application.Tests.Logging;

public sealed class SensitiveDataRulesTests
{
  [Fact]
  public void RedactedMarkerIsNonEmpty()
  {
    Assert.False(string.IsNullOrWhiteSpace(SensitiveDataRules.RedactedMarker));
    Assert.Equal("[REDACTED]", SensitiveDataRules.RedactedMarker);
  }

  [Fact]
  public void TruncateHashReturnsNoneForNull()
  {
    Assert.Equal("(none)", SensitiveDataRules.TruncateHash(null));
  }

  [Fact]
  public void TruncateHashReturnsNoneForEmpty()
  {
    Assert.Equal("(none)", SensitiveDataRules.TruncateHash(""));
  }

  [Fact]
  public void TruncateHashReturnsNoneForWhitespace()
  {
    Assert.Equal("(none)", SensitiveDataRules.TruncateHash("   "));
  }

  [Fact]
  public void TruncateHashReturnsFullHashWhen16CharactersOrLess()
  {
    Assert.Equal("abc123", SensitiveDataRules.TruncateHash("abc123"));
    Assert.Equal("0123456789abcdef", SensitiveDataRules.TruncateHash("0123456789abcdef"));
  }

  [Fact]
  public void TruncateHashTruncatesHashLongerThan16Characters()
  {
    var fullHash = "0123456789abcdef0123456789abcdef";
    var result = SensitiveDataRules.TruncateHash(fullHash);

    Assert.Equal("0123456789abcdef...", result);
    Assert.Equal(19, result.Length);
  }

  [Fact]
  public void TruncateHashPreservesFirst16Characters()
  {
    var fullHash = "abcdefghijklmnopqrstuvwx";
    var result = SensitiveDataRules.TruncateHash(fullHash);

    Assert.StartsWith("abcdefghijklmnop", result);
    Assert.EndsWith("...", result);
  }
}
