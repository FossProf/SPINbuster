using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IKnowledgeCitationRepository
{
  Task<IReadOnlyCollection<KnowledgeCitation>> GetByRevisionIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    KnowledgeCitation knowledgeCitation,
    CancellationToken cancellationToken = default);
}
