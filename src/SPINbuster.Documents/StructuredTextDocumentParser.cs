using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Documents;

public sealed class StructuredTextDocumentParser : IDocumentParser
{
  private const string ParserKey = "structured-text-deterministic";
  private const string ParserVersion = "1.0.0";
  private const string ContractVersion = "1.0.0";
  private const int MaxContentLength = 10 * 1024 * 1024;

  private static readonly IReadOnlyList<string> SupportedMediaTypes =
    ["text/markdown", "text/x-markdown"];

  private static readonly string ContractHash = ComputeContractHash();

  private static readonly Regex HeadingPattern = new(
    @"^(#{1,6})\s+(.+)$",
    RegexOptions.Compiled | RegexOptions.Multiline);

  private static readonly Regex NumberedClausePattern = new(
    @"^\s*(\d+(?:\.\d+)+)\s+(.+)$|^\s*(\d+)\.\s+(.+)$",
    RegexOptions.Compiled | RegexOptions.Multiline);

  private static readonly Regex LetteredClausePattern = new(
    @"^\s*([a-z])\)\s+(.+)$",
    RegexOptions.Compiled | RegexOptions.Multiline);

  private static readonly Regex TableSeparatorPattern = new(
    @"^\|?\s*[-:]+(?:\s*\|\s*[-:]+)*\s*\|?\s*$",
    RegexOptions.Compiled);

  private static readonly Regex TableRowPattern = new(
    @"^\|(.+)\|$",
    RegexOptions.Compiled);

  public ParserDescriptor Describe()
  {
    return new ParserDescriptor(
      ParserKey,
      ParserVersion,
      ContractVersion,
      ContractHash,
      SupportedMediaTypes,
      ContentKind.PlainText,
      ParserDeterminism.Deterministic);
  }

  public async Task<ParserExecutionResult> ParseAsync(ParserInput input, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(input);

    cancellationToken.ThrowIfCancellationRequested();

    if (!IsSupportedMediaType(input.DeclaredMediaType) && !IsSupportedMediaType(input.DetectedMediaType))
    {
      return new ParserExecutionResult(
        ParserExecutionStatus.Failed,
        ParserRunFailureClassification.UnsupportedMedia,
        $"Media type '{input.DetectedMediaType ?? input.DeclaredMediaType}' is not supported. Supported: text/markdown, text/x-markdown.",
        [],
        []);
    }

    if (input.ContentLength > MaxContentLength)
    {
      return new ParserExecutionResult(
        ParserExecutionStatus.Failed,
        ParserRunFailureClassification.LimitExceeded,
        $"Content length {input.ContentLength} exceeds maximum of {MaxContentLength} bytes.",
        [],
        []);
    }

    string text;
    try
    {
      using var reader = new StreamReader(input.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
      var buffer = new char[32768];
      var sb = new StringBuilder();
      int totalRead = 0;

      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var read = await reader.ReadAsync(buffer, cancellationToken);
        if (read == 0)
        {
          break;
        }

        totalRead += read;
        if (totalRead > MaxContentLength)
        {
          return new ParserExecutionResult(
            ParserExecutionStatus.Failed,
            ParserRunFailureClassification.LimitExceeded,
            $"Content length exceeds maximum of {MaxContentLength} bytes.",
            [],
            []);
        }

        sb.Append(buffer, 0, read);
      }

      text = sb.ToString();
    }
    catch (DecoderFallbackException ex)
    {
      return new ParserExecutionResult(
        ParserExecutionStatus.Failed,
        ParserRunFailureClassification.MalformedOutput,
        $"Content is not valid UTF-8: {ex.Message}",
        [],
        []);
    }

    if (string.IsNullOrWhiteSpace(text))
    {
      return new ParserExecutionResult(
        ParserExecutionStatus.Failed,
        ParserRunFailureClassification.MalformedOutput,
        "Content is empty or contains only whitespace.",
        [],
        []);
    }

    var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    var fragments = new List<ParserFragmentResult>();
    var diagnostics = new List<ParserDiagnosticResult>();
    var lineRanges = new List<FragmentLineRange>();
    var ordinal = 1;

    fragments.Add(new ParserFragmentResult(
      FragmentLocatorType.WholeDocument,
      "*",
      ordinal++,
      ContentKind.PlainText,
      text,
      ConfidenceBand.High));

    ExtractHeadings(lines, fragments, lineRanges, ref ordinal);
    ExtractClauses(lines, fragments, lineRanges, ref ordinal);
    ExtractTables(lines, text, fragments, lineRanges, ref ordinal);

    DetectOverlaps(lineRanges, diagnostics);

    var status = diagnostics.Count > 0
      ? ParserExecutionStatus.CompletedWithWarnings
      : ParserExecutionStatus.Completed;

    return new ParserExecutionResult(
      status,
      ParserRunFailureClassification.None,
      null,
      fragments,
      diagnostics);
  }

  private static void ExtractHeadings(
    string[] lines,
    List<ParserFragmentResult> fragments,
    List<FragmentLineRange> lineRanges,
    ref int ordinal)
  {
    for (var i = 0; i < lines.Length; i++)
    {
      var match = HeadingPattern.Match(lines[i]);
      if (!match.Success)
      {
        continue;
      }

      var level = match.Groups[1].Value.Length;
      var headingText = match.Groups[2].Value.Trim();
      var startLine = i + 1;
      var endLine = i + 1;

      if (i + 1 < lines.Length)
      {
        var nextLine = lines[i + 1];
        if (IsTableSeparatorLine(nextLine))
        {
          continue;
        }
      }

      var locatorValue = $"heading/{level}/{ordinal}";
      var extractedText = lines[i];

      fragments.Add(new ParserFragmentResult(
        FragmentLocatorType.StructuralPath,
        locatorValue,
        ordinal++,
        ContentKind.PlainText,
        extractedText,
        ConfidenceBand.High));

      lineRanges.Add(new FragmentLineRange(
        FragmentLocatorType.StructuralPath,
        locatorValue,
        ordinal - 1,
        ContentKind.PlainText,
        startLine,
        endLine));
    }
  }

  private static void ExtractClauses(
    string[] lines,
    List<ParserFragmentResult> fragments,
    List<FragmentLineRange> lineRanges,
    ref int ordinal)
  {
    for (var i = 0; i < lines.Length; i++)
    {
      var match = NumberedClausePattern.Match(lines[i]);
      if (!match.Success)
      {
        match = LetteredClausePattern.Match(lines[i]);
      }

      if (!match.Success)
      {
        continue;
      }

      var clauseRef = match.Groups[1].Success && match.Groups[1].Value.Length > 0
        ? match.Groups[1].Value
        : match.Groups[3].Value;
      var clauseText = match.Groups[2].Success && match.Groups[2].Value.Length > 0
        ? match.Groups[2].Value.Trim()
        : match.Groups[4].Value.Trim();
      var startLine = i + 1;
      var endLine = i + 1;

      while (endLine < lines.Length)
      {
        var nextLine = lines[endLine];
        if (string.IsNullOrWhiteSpace(nextLine))
        {
          break;
        }

        if (HeadingPattern.IsMatch(nextLine)
            || NumberedClausePattern.IsMatch(nextLine)
            || LetteredClausePattern.IsMatch(nextLine))
        {
          break;
        }

        endLine++;
      }

      var locatorValue = $"clause/{clauseRef}";
      var extractedText = string.Join("\n", lines[(startLine - 1)..endLine]);

      fragments.Add(new ParserFragmentResult(
        FragmentLocatorType.StructuralPath,
        locatorValue,
        ordinal++,
        ContentKind.PlainText,
        extractedText,
        ConfidenceBand.High));

      lineRanges.Add(new FragmentLineRange(
        FragmentLocatorType.StructuralPath,
        locatorValue,
        ordinal - 1,
        ContentKind.PlainText,
        startLine,
        endLine));
    }
  }

  private static void ExtractTables(
    string[] lines,
    string fullText,
    List<ParserFragmentResult> fragments,
    List<FragmentLineRange> lineRanges,
    ref int ordinal)
  {
    var i = 0;
    while (i < lines.Length)
    {
      var rowMatch = TableRowPattern.Match(lines[i]);
      if (!rowMatch.Success)
      {
        i++;
        continue;
      }

      var tableStartLine = i + 1;
      var tableLines = new List<int>();
      tableLines.Add(i);

      i++;
      if (i < lines.Length && IsTableSeparatorLine(lines[i]))
      {
        tableLines.Add(i);
        i++;

        while (i < lines.Length && TableRowPattern.IsMatch(lines[i]))
        {
          tableLines.Add(i);
          i++;
        }
      }
      else
      {
        continue;
      }

      if (tableLines.Count < 3)
      {
        continue;
      }

      var tableEndLine = tableLines[^1] + 1;
      var tableText = string.Join("\n", tableLines.Select(l => lines[l]));
      var locatorValue = $"table/{ordinal}";

      fragments.Add(new ParserFragmentResult(
        FragmentLocatorType.StructuralPath,
        locatorValue,
        ordinal++,
        ContentKind.Table,
        tableText,
        ConfidenceBand.High));

      lineRanges.Add(new FragmentLineRange(
        FragmentLocatorType.StructuralPath,
        locatorValue,
        ordinal - 1,
        ContentKind.Table,
        tableStartLine,
        tableEndLine));
    }
  }

  private static void DetectOverlaps(List<FragmentLineRange> lineRanges, List<ParserDiagnosticResult> diagnostics)
  {
    for (var i = 0; i < lineRanges.Count; i++)
    {
      for (var j = i + 1; j < lineRanges.Count; j++)
      {
        var a = lineRanges[i];
        var b = lineRanges[j];

        if (a.StartLine <= b.EndLine && b.StartLine <= a.EndLine)
        {
          diagnostics.Add(new ParserDiagnosticResult(
            DiagnosticSeverity.Warning,
            "OVERLAPPING_CONTENT",
            $"Fragment at '{a.LocatorValue}' (lines {a.StartLine}-{a.EndLine}) overlaps with fragment at '{b.LocatorValue}' (lines {b.StartLine}-{b.EndLine}).",
            DiagnosticRefType.Ordinal,
            a.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
            a.LocatorType,
            a.LocatorValue));
        }
      }
    }
  }

  private static bool IsSupportedMediaType(string? mediaType)
  {
    if (string.IsNullOrWhiteSpace(mediaType))
    {
      return false;
    }

    var normalized = mediaType.Trim().ToLowerInvariant();
    return string.Equals(normalized, "text/markdown", StringComparison.Ordinal)
      || string.Equals(normalized, "text/x-markdown", StringComparison.Ordinal);
  }

  private static bool IsTableSeparatorLine(string line)
  {
    return TableSeparatorPattern.IsMatch(line.Trim());
  }

  private static string ComputeContractHash()
  {
    var definition = $"{ParserKey}:{ParserVersion}:{ContractVersion}:{string.Join(",", SupportedMediaTypes)}:{ContentKind.PlainText}";
    var bytes = Encoding.UTF8.GetBytes(definition);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }

  private sealed record FragmentLineRange(
    FragmentLocatorType LocatorType,
    string LocatorValue,
    int Ordinal,
    ContentKind ContentKind,
    int StartLine,
    int EndLine);
}
