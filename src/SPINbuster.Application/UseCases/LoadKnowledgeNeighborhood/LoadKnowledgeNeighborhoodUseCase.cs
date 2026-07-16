using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeNeighborhood;

public sealed class LoadKnowledgeNeighborhoodUseCase
  : IQueryHandler<LoadKnowledgeNeighborhoodQuery, LoadKnowledgeNeighborhoodResult>
{
  private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
  private readonly IKnowledgeRelationshipRepository _knowledgeRelationshipRepository;
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;

  public LoadKnowledgeNeighborhoodUseCase(
    IKnowledgeDocumentRepository knowledgeDocumentRepository,
    IKnowledgeRevisionRepository knowledgeRevisionRepository,
    IKnowledgeRelationshipRepository knowledgeRelationshipRepository)
  {
    _knowledgeDocumentRepository = knowledgeDocumentRepository;
    _knowledgeRevisionRepository = knowledgeRevisionRepository;
    _knowledgeRelationshipRepository = knowledgeRelationshipRepository;
  }

  public async Task<LoadKnowledgeNeighborhoodResult> HandleAsync(
    LoadKnowledgeNeighborhoodQuery query,
    CancellationToken cancellationToken = default)
  {
    if (query.MaxDepth < 0)
    {
      throw new DomainInvariantException($"{nameof(query.MaxDepth)} cannot be negative.");
    }

    if (query.MaxRelationships < 1)
    {
      throw new DomainInvariantException($"{nameof(query.MaxRelationships)} must be at least 1.");
    }

    var relationships = new List<KnowledgeRelationship>();
    var visitedSubjects = new HashSet<KnowledgeSubjectReference> { query.Anchor };
    var frontier = new Queue<(KnowledgeSubjectReference Subject, int Depth)>();
    frontier.Enqueue((query.Anchor, 0));

    while (frontier.Count > 0 && relationships.Count < query.MaxRelationships)
    {
      var (subject, depth) = frontier.Dequeue();
      if (depth > query.MaxDepth)
      {
        continue;
      }

      var remaining = query.MaxRelationships - relationships.Count;
      var discovered = await _knowledgeRelationshipRepository.GetBySubjectAsync(
        query.ProjectId,
        subject,
        remaining,
        cancellationToken);

      foreach (var relationship in discovered)
      {
        if (relationships.Any(existing => existing.Id == relationship.Id))
        {
          continue;
        }

        relationships.Add(relationship);
        var adjacentSubject = relationship.Source == subject
          ? relationship.Target
          : relationship.Source;

        if (visitedSubjects.Add(adjacentSubject) && depth < query.MaxDepth)
        {
          frontier.Enqueue((adjacentSubject, depth + 1));
        }

        if (relationships.Count >= query.MaxRelationships)
        {
          break;
        }
      }
    }

    var nodes = new List<KnowledgeNeighborhoodNode>();
    foreach (var subject in visitedSubjects.OrderBy(subject => subject.ToStableKey(), StringComparer.Ordinal))
    {
      nodes.Add(await LoadNodeAsync(subject, cancellationToken));
    }

    return new LoadKnowledgeNeighborhoodResult(
      query.Anchor,
      nodes,
      relationships.Select(relationship => new KnowledgeNeighborhoodRelationship(
        relationship.Id,
        relationship.Source,
        relationship.Target,
        relationship.RelationshipType,
        relationship.VerificationStatus,
        relationship.EvidenceOrRationale)).ToArray());
  }

  private async Task<KnowledgeNeighborhoodNode> LoadNodeAsync(
    KnowledgeSubjectReference subject,
    CancellationToken cancellationToken)
  {
    switch (subject.SubjectKind)
    {
      case KnowledgeSubjectKind.Document:
        {
          var documentId = subject.DocumentId
            ?? throw new DomainInvariantException("Document subject references must include a document ID.");
          var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocument), documentId.ToString());
          return new KnowledgeNeighborhoodNode(
            subject,
            document.CanonicalTitle,
            document.DocumentType,
            null);
        }

      case KnowledgeSubjectKind.Revision:
        {
          var revisionId = subject.RevisionId
            ?? throw new DomainInvariantException("Revision subject references must include a revision ID.");
          var revision = await _knowledgeRevisionRepository.GetByIdAsync(revisionId, cancellationToken)
            ?? throw new ApplicationEntityNotFoundException(nameof(KnowledgeDocumentRevision), revisionId.ToString());
          return new KnowledgeNeighborhoodNode(
            subject,
            revision.RevisionLabel,
            null,
            revision.RevisionLabel);
        }

      default:
        throw new DomainInvariantException($"Unsupported knowledge subject kind {subject.SubjectKind}.");
    }
  }
}
