using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

/// <summary>
/// Persists and queries immutable parser diagnostic records. Diagnostics are
/// durable parser evidence attached to a run, not independently authoritative
/// aggregates. They carry no audit lifecycle and no review state.
/// </summary>
public interface IParserDiagnosticRepository
{
  Task AddRangeAsync(IReadOnlyList<ParserDiagnostic> diagnostics, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ParserDiagnostic>> GetByParserRunAsync(ParserRunId parserRunId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ParserDiagnostic>> GetByParserRunAndCandidateAsync(ParserRunId parserRunId, string candidateRefValue, CancellationToken cancellationToken = default);
}
