using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IKnowledgeDocumentRepository
{
  Task<KnowledgeDocument?> GetByIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<KnowledgeDocument>> GetByProjectAsync(
    ProjectId projectId,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default);

  Task UpdateAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default);
}
