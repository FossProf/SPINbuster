using System.Security.Cryptography;
using System.Text;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Documents;

public sealed class PlainTextDocumentParser : IDocumentParser
{
  private const string ParserKey = "plain-text-deterministic";
  private const string ParserVersion = "1.0.0";
  private const string ContractVersion = "1.0.0";
  private const int MaxContentLength = 10 * 1024 * 1024;
  private const int LinesPerGroup = 50;

  private static readonly IReadOnlyList<string> SupportedMediaTypes =
    ["text/plain", "text/*"];

  private static readonly string ContractHash = ComputeContractHash();

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
        $"Media type '{input.DetectedMediaType ?? input.DeclaredMediaType}' is not supported. Supported: text/plain.",
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

    var fragments = new List<ParserFragmentResult>();
    var ordinal = 1;

    fragments.Add(new ParserFragmentResult(
      FragmentLocatorType.WholeDocument,
      "*",
      ordinal++,
      ContentKind.PlainText,
      text,
      ConfidenceBand.High));

    var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    var paragraphs = SplitIntoParagraphs(lines);

    foreach (var paragraph in paragraphs)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var paragraphText = string.Join("\n", paragraph.Lines);
      if (string.IsNullOrWhiteSpace(paragraphText))
      {
        continue;
      }

      fragments.Add(new ParserFragmentResult(
        FragmentLocatorType.Paragraph,
        $"1:{paragraph.Ordinal}",
        ordinal++,
        ContentKind.PlainText,
        paragraphText,
        ConfidenceBand.High));
    }

    for (var i = 0; i < lines.Length; i += LinesPerGroup)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var startLine = i + 1;
      var endLine = Math.Min(i + LinesPerGroup, lines.Length);
      var groupText = string.Join("\n", lines[i..endLine]);
      if (string.IsNullOrWhiteSpace(groupText))
      {
        continue;
      }

      fragments.Add(new ParserFragmentResult(
        FragmentLocatorType.LineRange,
        $"{startLine}-{endLine}",
        ordinal++,
        ContentKind.PlainText,
        groupText,
        ConfidenceBand.High));
    }

    return new ParserExecutionResult(
      ParserExecutionStatus.Completed,
      ParserRunFailureClassification.None,
      null,
      fragments,
      []);
  }

  private static bool IsSupportedMediaType(string? mediaType)
  {
    if (string.IsNullOrWhiteSpace(mediaType))
    {
      return false;
    }

    var normalized = mediaType.Trim().ToLowerInvariant();
    return string.Equals(normalized, "text/plain", StringComparison.Ordinal)
      || string.Equals(normalized, "text/*", StringComparison.Ordinal);
  }

  private static List<ParagraphInfo> SplitIntoParagraphs(string[] lines)
  {
    var paragraphs = new List<ParagraphInfo>();
    var currentGroup = new List<string>();
    var paragraphOrdinal = 1;

    foreach (var line in lines)
    {
      if (string.IsNullOrWhiteSpace(line))
      {
        if (currentGroup.Count > 0)
        {
          paragraphs.Add(new ParagraphInfo(paragraphOrdinal++, currentGroup.ToArray()));
          currentGroup.Clear();
        }
      }
      else
      {
        currentGroup.Add(line);
      }
    }

    if (currentGroup.Count > 0)
    {
      paragraphs.Add(new ParagraphInfo(paragraphOrdinal, currentGroup.ToArray()));
    }

    return paragraphs;
  }

  private static string ComputeContractHash()
  {
    var definition = $"{ParserKey}:{ParserVersion}:{ContractVersion}:{string.Join(",", SupportedMediaTypes)}:{ContentKind.PlainText}";
    var bytes = Encoding.UTF8.GetBytes(definition);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }

  private sealed record ParagraphInfo(int Ordinal, string[] Lines);
}
