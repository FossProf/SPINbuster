using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IPromotionProvenanceRepository
{
  Task<PromotionProvenance?> GetByRevisionIdAsync(
    KnowledgeDocumentRevisionId revisionId,
    CancellationToken cancellationToken = default);

  Task<PromotionProvenance?> GetByFragmentCandidateIdAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    PromotionProvenance provenance,
    CancellationToken cancellationToken = default);
}
