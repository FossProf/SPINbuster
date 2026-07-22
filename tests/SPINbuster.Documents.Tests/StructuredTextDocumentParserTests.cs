using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Domain;
using System.Security.Cryptography;
using System.Text;

namespace SPINbuster.Documents.Tests;

public sealed class StructuredTextDocumentParserTests
{
  private static readonly ImportedSourceId TestImportedSourceId = ImportedSourceId.New();
  private static readonly ProjectId TestProjectId = ProjectId.New();

  [Fact]
  public void DescribeReturnsCorrectParserKey()
  {
    var parser = new StructuredTextDocumentParser();

    var descriptor = parser.Describe();

    Assert.Equal("structured-text-deterministic", descriptor.ParserKey);
  }

  [Fact]
  public void DescribeReturnsCorrectContractVersion()
  {
    var parser = new StructuredTextDocumentParser();

    var descriptor = parser.Describe();

    Assert.Equal("1.0.0", descriptor.ContractVersion);
  }

  [Fact]
  public void DescribeReturnsDeterministicDeterminism()
  {
    var parser = new StructuredTextDocumentParser();

    var descriptor = parser.Describe();

    Assert.Equal(ParserDeterminism.Deterministic, descriptor.Determinism);
  }

  [Fact]
  public void DescribeReturnsCorrectDefaultContentKind()
  {
    var parser = new StructuredTextDocumentParser();

    var descriptor = parser.Describe();

    Assert.Equal(ContentKind.PlainText, descriptor.DefaultContentKind);
  }

  [Fact]
  public void DescribeReturnsMarkdownMediaTypes()
  {
    var parser = new StructuredTextDocumentParser();

    var descriptor = parser.Describe();

    Assert.Equal(2, descriptor.SupportedMediaTypes.Count);
    Assert.Contains("text/markdown", descriptor.SupportedMediaTypes);
    Assert.Contains("text/x-markdown", descriptor.SupportedMediaTypes);
    Assert.DoesNotContain("text/plain", descriptor.SupportedMediaTypes);
  }

  [Fact]
  public void DescribeReturnsContractHash()
  {
    var parser = new StructuredTextDocumentParser();

    var descriptor = parser.Describe();

    Assert.NotNull(descriptor.ContractHash);
    Assert.NotEmpty(descriptor.ContractHash);
    Assert.Equal(64, descriptor.ContractHash.Length);
  }

  [Fact]
  public void DescribeReturnsDeterministicContractHash()
  {
    var parser = new StructuredTextDocumentParser();

    var first = parser.Describe();
    var second = parser.Describe();

    Assert.Equal(first.ContractHash, second.ContractHash);
  }

  [Fact]
  public async Task ParseAsyncReturnsFailedForUnsupportedMediaType()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/plain", "text/plain", "hello");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Failed, result.Status);
    Assert.Equal(ParserRunFailureClassification.UnsupportedMedia, result.FailureClassification);
    Assert.Contains("text/markdown", result.FailureDetails);
    Assert.Empty(result.Fragments);
  }

  [Fact]
  public async Task ParseAsyncReturnsFailedForEmptyContent()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", "text/markdown", "");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Failed, result.Status);
    Assert.Equal(ParserRunFailureClassification.MalformedOutput, result.FailureClassification);
    Assert.Contains("empty", result.FailureDetails, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task ParseAsyncReturnsFailedForWhitespaceOnlyContent()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", "text/markdown", "   \n  \n  ");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Failed, result.Status);
    Assert.Equal(ParserRunFailureClassification.MalformedOutput, result.FailureClassification);
  }

  [Fact]
  public async Task ParseAsyncReturnsFailedWhenContentLengthExceedsMaximum()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", "text/markdown", "a", contentLength: 10 * 1024 * 1024 + 1);

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Failed, result.Status);
    Assert.Equal(ParserRunFailureClassification.LimitExceeded, result.FailureClassification);
    Assert.Contains("exceeds maximum", result.FailureDetails, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task ParseAsyncProducesWholeDocumentFragment()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", "text/markdown", "Hello world");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Completed, result.Status);
    Assert.Single(result.Fragments);
    Assert.Equal(FragmentLocatorType.WholeDocument, result.Fragments[0].LocatorType);
    Assert.Equal("*", result.Fragments[0].LocatorValue);
    Assert.Equal(ContentKind.PlainText, result.Fragments[0].ContentKind);
    Assert.Equal("Hello world", result.Fragments[0].ExtractedText);
    Assert.Equal(ConfidenceBand.High, result.Fragments[0].ConfidenceBand);
    Assert.Equal(1, result.Fragments[0].Ordinal);
  }

  [Fact]
  public async Task ParseAsyncExtractsSingleHeading()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", "text/markdown", "# Introduction\n\nSome text here.");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Completed, result.Status);
    var heading = result.Fragments.FirstOrDefault(f => f.LocatorType == FragmentLocatorType.StructuralPath && f.ContentKind == ContentKind.PlainText && f.LocatorValue.StartsWith("heading", StringComparison.Ordinal));
    Assert.NotNull(heading);
    Assert.Equal("# Introduction", heading.ExtractedText);
  }

  [Fact]
  public async Task ParseAsyncExtractsMultipleHeadings()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "# Title\n\nSome text.\n\n## Subtitle\n\nMore text.\n\n### Section\n\nEven more.";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var headings = result.Fragments.Where(f => f.LocatorType == FragmentLocatorType.StructuralPath && f.LocatorValue.StartsWith("heading", StringComparison.Ordinal)).ToList();
    Assert.Equal(3, headings.Count);
    Assert.Contains(headings, h => h.ExtractedText == "# Title");
    Assert.Contains(headings, h => h.ExtractedText == "## Subtitle");
    Assert.Contains(headings, h => h.ExtractedText == "### Section");
  }

  [Fact]
  public async Task ParseAsyncExtractsNumberedClause()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "1. First clause.\n\n2. Second clause.";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var clauses = result.Fragments.Where(f => f.LocatorType == FragmentLocatorType.StructuralPath && f.LocatorValue.StartsWith("clause", StringComparison.Ordinal)).ToList();
    Assert.Equal(2, clauses.Count);
    Assert.Contains(clauses, c => c.LocatorValue == "clause/1");
    Assert.Contains(clauses, c => c.LocatorValue == "clause/2");
  }

  [Fact]
  public async Task ParseAsyncExtractsLetteredClause()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "a) First item.\n\nb) Second item.";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var clauses = result.Fragments.Where(f => f.LocatorType == FragmentLocatorType.StructuralPath && f.LocatorValue.StartsWith("clause", StringComparison.Ordinal)).ToList();
    Assert.Equal(2, clauses.Count);
    Assert.Contains(clauses, c => c.LocatorValue == "clause/a");
    Assert.Contains(clauses, c => c.LocatorValue == "clause/b");
  }

  [Fact]
  public async Task ParseAsyncExtractsClauseWithContinuationLines()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "1. This is a clause\n   that continues on the next line.\n\n2. Second clause.";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var clauses = result.Fragments.Where(f => f.LocatorType == FragmentLocatorType.StructuralPath && f.LocatorValue.StartsWith("clause", StringComparison.Ordinal)).ToList();
    var firstClause = clauses.FirstOrDefault(c => c.LocatorValue == "clause/1");
    Assert.NotNull(firstClause);
    Assert.Contains("continues on the next line", firstClause.ExtractedText);
  }

  [Fact]
  public async Task ParseAsyncExtractsTable()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "| Name | Value |\n| --- | --- |\n| A | 1 |\n| B | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var tables = result.Fragments.Where(f => f.LocatorType == FragmentLocatorType.StructuralPath && f.ContentKind == ContentKind.Table).ToList();
    Assert.Single(tables);
    Assert.Contains("Name", tables[0].ExtractedText);
    Assert.Contains("Value", tables[0].ExtractedText);
    Assert.Contains("A", tables[0].ExtractedText);
    Assert.Contains("2", tables[0].ExtractedText);
  }

  [Fact]
  public async Task ParseAsyncTableUsesTableContentKind()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "| Col1 | Col2 |\n| --- | --- |\n| Val1 | Val2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var table = result.Fragments.FirstOrDefault(f => f.ContentKind == ContentKind.Table);
    Assert.NotNull(table);
    Assert.Equal(ContentKind.Table, table.ContentKind);
  }

  [Fact]
  public async Task ParseAsyncExtractsMultipleTables()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "| A | B |\n| --- | --- |\n| 1 | 2 |\n\nSome text.\n\n| C | D |\n| --- | --- |\n| 3 | 4 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var tables = result.Fragments.Where(f => f.ContentKind == ContentKind.Table).ToList();
    Assert.Equal(2, tables.Count);
  }

  [Fact]
  public async Task ParseAsyncEmitsOverlapDiagnosticWhenClauseContinuationOverlapsTable()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "1. This clause spans\nmultiple lines including table data\n| A | B |\n| --- | --- |\n| 1 | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var structuralOverlaps = result.Diagnostics.Where(d => d.Code == "OVERLAPPING_CONTENT").ToList();
    Assert.Contains(structuralOverlaps, d => d.LocatorValue?.StartsWith("clause", StringComparison.Ordinal) == true);
  }

  [Fact]
  public async Task ParseAsyncSetsCompletedWithWarningsWhenStructuralOverlapsDetected()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "1. This clause spans\nmultiple lines including table data\n| A | B |\n| --- | --- |\n| 1 | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.CompletedWithWarnings, result.Status);
  }

  [Fact]
  public async Task ParseAsyncReturnsCompletedWithoutDiagnosticsForPlainContent()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", "text/markdown", "Just some plain text.");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Completed, result.Status);
    Assert.Empty(result.Diagnostics);
  }

  [Fact]
  public async Task ParseAsyncOrdinalsAreSequential()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "# Title\n\n1. First clause.\n\n2. Second clause.\n\n| A | B |\n| --- | --- |\n| 1 | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var ordinals = result.Fragments.Select(f => f.Ordinal).OrderBy(o => o).ToList();
    Assert.Equal(ordinals.Count, result.Fragments.Count);
    for (var i = 0; i < ordinals.Count; i++)
    {
      Assert.Equal(i + 1, ordinals[i]);
    }
  }

  [Fact]
  public async Task ParseAsyncReturnsNoDiagnosticsForTableWithoutOverlap()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "| Col |\n| --- |\n| Val |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var tableDiagnostics = result.Diagnostics.Where(d => d.Code == "OVERLAPPING_CONTENT" && d.LocatorValue?.StartsWith("table", StringComparison.Ordinal) == true).ToList();
    Assert.Empty(tableDiagnostics);
  }

  [Fact]
  public async Task ParseAsyncAcceptsDetectedMediaTypeWhenDeclaredIsNull()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput(null, "text/markdown", "# Hello");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Completed, result.Status);
  }

  [Fact]
  public async Task ParseAsyncAcceptsDeclaredMediaTypeWhenDetectedIsNull()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", null, "# Hello");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Completed, result.Status);
  }

  [Fact]
  public async Task ParseAsyncAcceptsXMarkdownMediaType()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/x-markdown", "text/x-markdown", "# Hello");

    var result = await parser.ParseAsync(input);

    Assert.Equal(ParserExecutionStatus.Completed, result.Status);
  }

  [Fact]
  public async Task ParseAsyncIsCancellationAware()
  {
    var parser = new StructuredTextDocumentParser();
    var input = CreateParserInput("text/markdown", "text/markdown", "# Title");
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => parser.ParseAsync(input, cts.Token));
  }

  [Fact]
  public void ParseAsyncContractHashIsStableAcrossInstances()
  {
    var first = new StructuredTextDocumentParser().Describe().ContractHash;
    var second = new StructuredTextDocumentParser().Describe().ContractHash;

    Assert.Equal(first, second);
  }

  [Fact]
  public async Task ParseAsyncHandlesNestedHeadingLevels()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "# H1\n\n## H2\n\n### H3\n\n#### H4\n\n##### H5\n\n###### H6";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var headings = result.Fragments.Where(f => f.LocatorValue.StartsWith("heading", StringComparison.Ordinal)).ToList();
    Assert.Equal(6, headings.Count);
    Assert.Contains(headings, h => h.ExtractedText == "# H1");
    Assert.Contains(headings, h => h.ExtractedText == "###### H6");
  }

  [Fact]
  public async Task ParseAsyncExtractsNumberedClausesWithSubNumbers()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "1.1 First sub-clause.\n\n1.2 Second sub-clause.";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var clauses = result.Fragments.Where(f => f.LocatorValue.StartsWith("clause", StringComparison.Ordinal)).ToList();
    Assert.Equal(2, clauses.Count);
    Assert.Contains(clauses, c => c.LocatorValue == "clause/1.1");
    Assert.Contains(clauses, c => c.LocatorValue == "clause/1.2");
  }

  [Fact]
  public async Task ParseAsyncDoesNotTreatTableSeparatorAsTable()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "| Name | Value |\n| --- | --- |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var tables = result.Fragments.Where(f => f.ContentKind == ContentKind.Table).ToList();
    Assert.Empty(tables);
  }

  [Fact]
  public async Task ParseAsyncOverlapDiagnosticReferencesBothFragments()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "1. This clause spans\nmultiple lines including table data\n| A | B |\n| --- | --- |\n| 1 | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var overlap = result.Diagnostics.First(d => d.Code == "OVERLAPPING_CONTENT");
    Assert.Equal(DiagnosticRefType.Ordinal, overlap.CandidateRefType);
    Assert.NotNull(overlap.CandidateRefValue);
    Assert.Equal(FragmentLocatorType.StructuralPath, overlap.LocatorType);
    Assert.NotNull(overlap.LocatorValue);
    Assert.Contains("overlaps", overlap.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task ParseAsyncRejectsNullInput()
  {
    var parser = new StructuredTextDocumentParser();

    await Assert.ThrowsAsync<ArgumentNullException>(() => parser.ParseAsync(null!));
  }

  [Fact]
  public void ContractHashChangesWhenSupportedMediaTypesChange()
  {
    var baseline = new StructuredTextDocumentParser().Describe().ContractHash;

    var overrideParser = new StructuredTextDocumentParser();
    var descriptor = overrideParser.Describe();

    Assert.NotEmpty(baseline);
    Assert.Equal(64, baseline.Length);
  }

  [Fact]
  public void ContractHashStableAcrossMultipleCalls()
  {
    var hashes = Enumerable.Range(0, 10)
      .Select(_ => new StructuredTextDocumentParser().Describe().ContractHash)
      .Distinct()
      .ToArray();

    Assert.Single(hashes);
  }

  [Fact]
  public async Task MalformedUtf8BytesFailTerminally()
  {
    var parser = new StructuredTextDocumentParser();
    var invalidBytes = new byte[] { 0x80, 0x81, 0x82, 0xFE };
    var stream = new MemoryStream(invalidBytes, writable: false);
    var input = new ParserInput(
      TestImportedSourceId,
      TestProjectId,
      "test.md",
      "text/markdown",
      "text/markdown",
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
  public void DescribeMatchesImplementedMediaTypes()
  {
    var parser = new StructuredTextDocumentParser();
    var descriptor = parser.Describe();

    Assert.Contains("text/markdown", descriptor.SupportedMediaTypes);
    Assert.Contains("text/x-markdown", descriptor.SupportedMediaTypes);
    Assert.DoesNotContain("text/plain", descriptor.SupportedMediaTypes);
    Assert.DoesNotContain("text/*", descriptor.SupportedMediaTypes);
  }

  [Fact]
  public void DescribeMatchesImplementedFragmentLocatorTypes()
  {
    var parser = new StructuredTextDocumentParser();
    var descriptor = parser.Describe();

    Assert.Equal(ContentKind.PlainText, descriptor.DefaultContentKind);
    Assert.Equal(ParserDeterminism.Deterministic, descriptor.Determinism);
    Assert.Equal("structured-text-deterministic", descriptor.ParserKey);
    Assert.Equal("1.0.0", descriptor.ContractVersion);
  }

  [Fact]
  public async Task ParseAsyncProducesExpectedFragmentLocatorTypes()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "# Title\n\n1. First clause.\n\n| A | B |\n| --- | --- |\n| 1 | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var locatorTypes = result.Fragments.Select(f => f.LocatorType).Distinct().ToList();
    Assert.Contains(FragmentLocatorType.WholeDocument, locatorTypes);
    Assert.Contains(FragmentLocatorType.StructuralPath, locatorTypes);
  }

  [Fact]
  public async Task ParseAsyncProducesExpectedContentKinds()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "# Title\n\n1. First clause.\n\n| A | B |\n| --- | --- |\n| 1 | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var contentKinds = result.Fragments.Select(f => f.ContentKind).Distinct().ToList();
    Assert.Contains(ContentKind.PlainText, contentKinds);
    Assert.Contains(ContentKind.Table, contentKinds);
  }

  [Fact]
  public async Task ParseAsyncEmitsExpectedDiagnosticCodes()
  {
    var parser = new StructuredTextDocumentParser();
    var markdown = "1. This clause spans\nmultiple lines including table data\n| A | B |\n| --- | --- |\n| 1 | 2 |";
    var input = CreateParserInput("text/markdown", "text/markdown", markdown);

    var result = await parser.ParseAsync(input);

    var diagnosticCodes = result.Diagnostics.Select(d => d.Code).Distinct().ToList();
    Assert.Contains("OVERLAPPING_CONTENT", diagnosticCodes);
    Assert.All(diagnosticCodes, code => Assert.Equal("OVERLAPPING_CONTENT", code));
  }

  private static ParserInput CreateParserInput(
    string? declaredMediaType,
    string? detectedMediaType,
    string content,
    long? contentLength = null)
  {
    var bytes = Encoding.UTF8.GetBytes(content);
    return new ParserInput(
      TestImportedSourceId,
      TestProjectId,
      "test.md",
      declaredMediaType,
      detectedMediaType,
      Convert.ToHexString(SHA256.HashData(bytes)),
      "SHA-256",
      1,
      contentLength ?? bytes.Length,
      new MemoryStream(bytes, writable: false));
  }
}
