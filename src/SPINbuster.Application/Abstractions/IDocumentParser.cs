using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.Abstractions;

public interface IDocumentParser
{
  ParserDescriptor Describe();

  Task<ParserExecutionResult> ParseAsync(ParserInput input, CancellationToken cancellationToken = default);
}

public sealed record ParserDescriptor(
  string ParserKey,
  string ParserVersion,
  string ContractVersion,
  string ContractHash,
  IReadOnlyList<string> SupportedMediaTypes,
  ContentKind DefaultContentKind,
  ParserDeterminism Determinism);

public enum ParserDeterminism
{
  Deterministic = 0,
  NonDeterministic = 1,
}
