using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IFragmentCandidateRepository
{
  Task<IReadOnlyCollection<FragmentCandidate>> GetByParserRunAsync(
    ParserRunId parserRunId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<FragmentCandidate>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task AddAsync(FragmentCandidate fragmentCandidate, CancellationToken cancellationToken = default);
}
