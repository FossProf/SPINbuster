using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Domain;
using System.Security.Cryptography;
using System.Text;

namespace SPINbuster.Documents.Tests;

public sealed class PlainTextDocumentParserTests
{
  private static readonly ImportedSourceId TestImportedSourceId = ImportedSourceId.New();
  private static readonly ProjectId TestProjectId = ProjectId.New();

  [Fact]
  public void DescribeReturnsCorrectParserKey()
  {
    var parser = new PlainTextDocumentParser();
    var descriptor = parser.Describe();
    Assert.Equal("plain-text-deterministic", descriptor.ParserKey);
  }

  [Fact]
  public void DescribeReturnsCorrectContractVersion()
  {
    var parser = new PlainTextDocumentParser();
    var descriptor = parser.Describe();
    Assert.Equal("1.0.0", descriptor.ContractVersion);
  }

  [Fact]
  public void DescribeReturnsPlainTextMediaTypes()
  {
    var parser = new PlainTextDocumentParser();
    var descriptor = parser.Describe();
    Assert.Contains("text/plain", descriptor.SupportedMediaTypes);
  }

  [Fact]
  public void ContractHashStableAcrossMultipleCalls()
  {
    var hashes = Enumerable.Range(0, 10)
      .Select(_ => new PlainTextDocumentParser().Describe().ContractHash)
      .Distinct()
      .ToArray();
    Assert.Single(hashes);
  }

  [Fact]
  public async Task MalformedUtf8BytesFailTerminally()
  {
    var parser = new PlainTextDocumentParser();
    var invalidBytes = new byte[] { 0x80, 0x81, 0x82, 0xFE };
    var stream = new MemoryStream(invalidBytes, writable: false);
    var input = new ParserInput(
      TestImportedSourceId,
      TestProjectId,
      "test.txt",
      "text/plain",
      "text/plain",
      Convert.ToHexString(SHA256.HashData(invalidBytes)),
      "SHA-256",
      1,
      invalidBytes.Length,
      stream);

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Failed, result.Status);
    Assert.Equal(ParserRunFailureClassification.MalformedOutput, result.FailureClassification);
    Assert.Contains("UTF-8", result.FailureDetails, StringComparison.OrdinalIgnoreCase);
    Assert.Empty(result.Fragments);
  }

  [Fact]
  public async Task ParseAsyncProducesWholeDocumentFragment()
  {
    var parser = new PlainTextDocumentParser();
    var input = CreateParserInput("Hello world");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Completed, result.Status);
    Assert.Contains(result.Fragments, f => f.LocatorType == FragmentLocatorType.WholeDocument);
  }

  [Fact]
  public void DescribeContractHashDiffersFromStructuredTextParser()
  {
    var plainHash = new PlainTextDocumentParser().Describe().ContractHash;
    var structuredHash = new StructuredTextDocumentParser().Describe().ContractHash;
    Assert.NotEqual(plainHash, structuredHash);
  }

  private static ParserInput CreateParserInput(string content)
  {
    var bytes = Encoding.UTF8.GetBytes(content);
    return new ParserInput(
      TestImportedSourceId,
      TestProjectId,
      "test.txt",
      "text/plain",
      "text/plain",
      Convert.ToHexString(SHA256.HashData(bytes)),
      "SHA-256",
      1,
      bytes.Length,
      new MemoryStream(bytes, writable: false));
  }
}
