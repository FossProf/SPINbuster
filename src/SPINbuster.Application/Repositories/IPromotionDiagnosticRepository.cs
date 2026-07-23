using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IPromotionDiagnosticRepository
{
  Task<PromotionDiagnostic?> GetByIdAsync(
    PromotionDiagnosticId promotionDiagnosticId,
    CancellationToken cancellationToken = default);

  Task<PromotionDiagnostic?> GetByFragmentCandidateAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<PromotionDiagnostic>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task<PromotionDiagnostic?> FindSuccessfulByContentHashAsync(
    ProjectId projectId,
    string contentHash,
    string normalizedLocatorValue,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    PromotionDiagnostic promotionDiagnostic,
    CancellationToken cancellationToken = default);

  Task UpdateAsync(
    PromotionDiagnostic promotionDiagnostic,
    CancellationToken cancellationToken = default);
}
