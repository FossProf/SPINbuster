using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IFragmentCandidateRepository
{
  Task<FragmentCandidate?> GetByIdAsync(FragmentCandidateId fragmentCandidateId, CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<FragmentCandidate>> GetByParserRunAsync(
    ParserRunId parserRunId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<FragmentCandidate>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<FragmentCandidate>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<FragmentCandidate>> GetByProjectFilteredAsync(
    ProjectId projectId,
    int maxResults,
    FragmentCandidateReviewState? reviewStateFilter,
    CancellationToken cancellationToken = default);

  Task AddAsync(FragmentCandidate fragmentCandidate, CancellationToken cancellationToken = default);

  Task UpdateAsync(FragmentCandidate fragmentCandidate, CancellationToken cancellationToken = default);
}
