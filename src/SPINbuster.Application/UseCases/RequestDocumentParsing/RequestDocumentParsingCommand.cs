using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestDocumentParsing;

public sealed record RequestDocumentParsingCommand(
  ProjectId ProjectId,
  ImportedSourceId ImportedSourceId,
  string ParserKey,
  string ParserContractVersion) : ICommand<RequestDocumentParsingResult>;
