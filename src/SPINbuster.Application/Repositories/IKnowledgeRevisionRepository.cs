using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IKnowledgeRevisionRepository
{
  Task<KnowledgeDocumentRevision?> GetByIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default);

  Task<KnowledgeDocumentRevision?> GetCurrentByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<KnowledgeDocumentRevision>> GetByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default);

  Task UpdateAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default);
}
