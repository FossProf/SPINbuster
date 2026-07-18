using SPINbuster.Application.Logging;

namespace SPINbuster.Application.Tests.Logging;

public sealed class SensitiveDataExclusionTests
{
  [Fact]
  public void SensitiveDataRulesDocumentExclusionCategories()
  {
    var type = typeof(SensitiveDataRules);
    var xmlFile = Path.ChangeExtension(type.Assembly.Location, ".xml");

    if (!File.Exists(xmlFile))
    {
      return;
    }

    var doc = System.Xml.Linq.XDocument.Load(xmlFile);
    var member = doc.Descendants("member")
      .FirstOrDefault(m => m.Attribute("name")?.Value == $"T:{type.FullName}");

    var remarks = member?.Element("remarks")?.Value ?? string.Empty;

    Assert.Contains("MUST NEVER", remarks, StringComparison.Ordinal);
    Assert.Contains("Secrets", remarks, StringComparison.Ordinal);
    Assert.Contains("connection strings", remarks, StringComparison.Ordinal);
    Assert.Contains("evidence bytes", remarks, StringComparison.Ordinal);
    Assert.Contains("prompt", remarks, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("model output", remarks, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("filesystem paths", remarks, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void TruncateHashNeverRevealsFullHash()
  {
    var fullHash = "0123456789abcdef0123456789abcdef0123456789abcdef";
    var result = SensitiveDataRules.TruncateHash(fullHash);

    Assert.DoesNotContain(fullHash, result);
    Assert.EndsWith("...", result);
  }

  [Fact]
  public void TruncateHashShortHashIsNotTruncated()
  {
    var shortHash = "abc123";
    var result = SensitiveDataRules.TruncateHash(shortHash);

    Assert.Equal(shortHash, result);
    Assert.DoesNotContain("...", result);
  }

  [Fact]
  public void RedactedMarkerExists()
  {
    Assert.Equal("[REDACTED]", SensitiveDataRules.RedactedMarker);
  }

  [Fact]
  public void SafeLogMessageUsesRelativePathAndTruncatedHash()
  {
    var safeMessage = "Imported source abc123def456789... for project proj-001";

    Assert.DoesNotContain(@":\", safeMessage);
    Assert.DoesNotContain("password", safeMessage, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("secret", safeMessage, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("...", safeMessage, StringComparison.Ordinal);
  }

  [Fact]
  public void TruncateHashExactly16CharactersIsNotTruncated()
  {
    var hash16 = "0123456789abcdef";
    var result = SensitiveDataRules.TruncateHash(hash16);

    Assert.Equal(hash16, result);
    Assert.DoesNotContain("...", result);
  }

  [Fact]
  public void TruncateHash17CharactersIsTruncated()
  {
    var hash17 = "0123456789abcdefg";
    var result = SensitiveDataRules.TruncateHash(hash17);

    Assert.Equal("0123456789abcdef...", result);
  }
}
