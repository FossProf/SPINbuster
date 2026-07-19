using SPINbuster.Domain;

namespace SPINbuster.Application.Contracts;

public sealed record ParserExecutionResult(
  bool Success,
  ParserRunFailureClassification FailureClassification,
  string? FailureDetails,
  IReadOnlyList<ParserFragmentResult> Fragments);

public sealed record ParserFragmentResult(
  FragmentLocatorType LocatorType,
  string LocatorValue,
  int Ordinal,
  ContentKind ContentKind,
  string ExtractedText,
  ConfidenceBand ConfidenceBand,
  IReadOnlyList<string> DiagnosticCodes);

public enum ParserRunFailureClassification
{
  None = 0,
  UnsupportedMedia = 1,
  SourceUnavailable = 2,
  IntegrityMismatch = 3,
  MalformedOutput = 4,
  LimitExceeded = 5,
  Cancelled = 6,
  ParserFailure = 7,
  Unknown = 99,
}
