using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IKnowledgeRelationshipRepository
{
  Task<KnowledgeRelationship?> GetByIdAsync(
    KnowledgeRelationshipId knowledgeRelationshipId,
    CancellationToken cancellationToken = default);

  Task<KnowledgeRelationship?> FindByEndpointsAsync(
    ProjectId projectId,
    KnowledgeSubjectReference source,
    KnowledgeSubjectReference target,
    KnowledgeRelationshipType relationshipType,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyCollection<KnowledgeRelationship>> GetBySubjectAsync(
    ProjectId projectId,
    KnowledgeSubjectReference subject,
    int maxResults,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default);

  Task UpdateAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default);
}
