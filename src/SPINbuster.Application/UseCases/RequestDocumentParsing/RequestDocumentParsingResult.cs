using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestDocumentParsing;

public sealed record RequestDocumentParsingResult(
  ParserRunId ParserRunId,
  ParserRunState State,
  ParserRunFailureClassification FailureClassification,
  string? FailureDetails,
  IReadOnlyList<FragmentCandidateId> FragmentCandidateIds);
