using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;

public sealed record LoadProjectKnowledgeSnapshotQuery(
  ProjectId ProjectId,
  int MaxRelationships = LoadProjectKnowledgeSnapshotUseCase.DefaultRelationshipLimit)
  : IQuery<LoadProjectKnowledgeSnapshotResult>;
