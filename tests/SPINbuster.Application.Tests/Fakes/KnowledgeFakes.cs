using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeKnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
  private readonly Dictionary<KnowledgeDocumentId, KnowledgeDocument> _documents = [];

  public List<KnowledgeDocument> AddedDocuments { get; } = [];

  public List<KnowledgeDocument> UpdatedDocuments { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<KnowledgeDocument?> GetByIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _documents.TryGetValue(knowledgeDocumentId, out var knowledgeDocument);
    return Task.FromResult(knowledgeDocument);
  }

  public Task<IReadOnlyCollection<KnowledgeDocument>> GetByProjectAsync(
    ProjectId projectId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeDocument>>(
      _documents.Values.Where(document => document.ProjectId == projectId).ToArray());
  }

  public Task AddAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _documents[knowledgeDocument.Id] = knowledgeDocument;
    AddedDocuments.Add(knowledgeDocument);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeDocument knowledgeDocument,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _documents[knowledgeDocument.Id] = knowledgeDocument;
    UpdatedDocuments.Add(knowledgeDocument);
    return Task.CompletedTask;
  }
}

internal sealed class FakeKnowledgeRevisionRepository : IKnowledgeRevisionRepository
{
  private readonly Dictionary<KnowledgeDocumentRevisionId, KnowledgeDocumentRevision> _revisions = [];

  public List<KnowledgeDocumentRevision> AddedRevisions { get; } = [];

  public List<KnowledgeDocumentRevision> UpdatedRevisions { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<KnowledgeDocumentRevision?> GetByIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _revisions.TryGetValue(knowledgeDocumentRevisionId, out var knowledgeRevision);
    return Task.FromResult(knowledgeRevision);
  }

  public Task<KnowledgeDocumentRevision?> GetCurrentByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult(
      _revisions.Values.SingleOrDefault(revision =>
        revision.DocumentId == knowledgeDocumentId
        && revision.Lifecycle == KnowledgeRevisionLifecycle.CurrentAuthoritative));
  }

  public Task<IReadOnlyCollection<KnowledgeDocumentRevision>> GetByDocumentIdAsync(
    KnowledgeDocumentId knowledgeDocumentId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeDocumentRevision>>(
      _revisions.Values.Where(revision => revision.DocumentId == knowledgeDocumentId).ToArray());
  }

  public Task AddAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _revisions[knowledgeDocumentRevision.Id] = knowledgeDocumentRevision;
    AddedRevisions.Add(knowledgeDocumentRevision);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeDocumentRevision knowledgeDocumentRevision,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _revisions[knowledgeDocumentRevision.Id] = knowledgeDocumentRevision;
    UpdatedRevisions.Add(knowledgeDocumentRevision);
    return Task.CompletedTask;
  }
}

internal sealed class FakeKnowledgeRelationshipRepository : IKnowledgeRelationshipRepository
{
  private readonly Dictionary<KnowledgeRelationshipId, KnowledgeRelationship> _relationships = [];

  public List<KnowledgeRelationship> AddedRelationships { get; } = [];

  public List<KnowledgeRelationship> UpdatedRelationships { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<KnowledgeRelationship?> GetByIdAsync(
    KnowledgeRelationshipId knowledgeRelationshipId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _relationships.TryGetValue(knowledgeRelationshipId, out var knowledgeRelationship);
    return Task.FromResult(knowledgeRelationship);
  }

  public Task<KnowledgeRelationship?> FindByEndpointsAsync(
    ProjectId projectId,
    KnowledgeSubjectReference source,
    KnowledgeSubjectReference target,
    KnowledgeRelationshipType relationshipType,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult(
      _relationships.Values.SingleOrDefault(relationship =>
        relationship.ProjectId == projectId
        && relationship.Source == source
        && relationship.Target == target
        && relationship.RelationshipType == relationshipType));
  }

  public Task<IReadOnlyCollection<KnowledgeRelationship>> GetBySubjectAsync(
    ProjectId projectId,
    KnowledgeSubjectReference subject,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeRelationship>>(
      _relationships.Values
        .Where(relationship =>
          relationship.ProjectId == projectId
          && (relationship.Source == subject || relationship.Target == subject))
        .OrderBy(relationship => relationship.CreatedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public Task AddAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _relationships[knowledgeRelationship.Id] = knowledgeRelationship;
    AddedRelationships.Add(knowledgeRelationship);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    KnowledgeRelationship knowledgeRelationship,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _relationships[knowledgeRelationship.Id] = knowledgeRelationship;
    UpdatedRelationships.Add(knowledgeRelationship);
    return Task.CompletedTask;
  }
}

internal sealed class FakeKnowledgeCitationRepository : IKnowledgeCitationRepository
{
  private readonly Dictionary<KnowledgeCitationId, KnowledgeCitation> _citations = [];

  public List<KnowledgeCitation> AddedCitations { get; } = [];

  public CancellationToken LastCancellationToken { get; private set; }

  public Task<IReadOnlyCollection<KnowledgeCitation>> GetByRevisionIdAsync(
    KnowledgeDocumentRevisionId knowledgeDocumentRevisionId,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    return Task.FromResult<IReadOnlyCollection<KnowledgeCitation>>(
      _citations.Values.Where(citation => citation.CitedRevisionId == knowledgeDocumentRevisionId).ToArray());
  }

  public Task AddAsync(
    KnowledgeCitation knowledgeCitation,
    CancellationToken cancellationToken = default)
  {
    LastCancellationToken = cancellationToken;
    _citations[knowledgeCitation.Id] = knowledgeCitation;
    AddedCitations.Add(knowledgeCitation);
    return Task.CompletedTask;
  }
}
