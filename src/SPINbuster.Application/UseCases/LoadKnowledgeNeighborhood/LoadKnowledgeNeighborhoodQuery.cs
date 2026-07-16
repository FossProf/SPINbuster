using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeNeighborhood;

public sealed record LoadKnowledgeNeighborhoodQuery(
  ProjectId ProjectId,
  KnowledgeSubjectReference Anchor,
  int MaxDepth,
  int MaxRelationships)
  : IQuery<LoadKnowledgeNeighborhoodResult>;
