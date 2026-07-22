using SPINbuster.Domain;

namespace SPINbuster.Application.Contracts;

public sealed record ParserExecutionResult(
  ParserExecutionStatus Status,
  ParserRunFailureClassification FailureClassification,
  string? FailureDetails,
  IReadOnlyList<ParserFragmentResult> Fragments,
  IReadOnlyList<ParserDiagnosticResult> Diagnostics);

public sealed record ParserFragmentResult(
  FragmentLocatorType LocatorType,
  string LocatorValue,
  int Ordinal,
  ContentKind ContentKind,
  string ExtractedText,
  ConfidenceBand ConfidenceBand);

public sealed record ParserDiagnosticResult(
  DiagnosticSeverity Severity,
  string Code,
  string Message,
  DiagnosticRefType? CandidateRefType,
  string? CandidateRefValue,
  FragmentLocatorType? LocatorType,
  string? LocatorValue);

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
