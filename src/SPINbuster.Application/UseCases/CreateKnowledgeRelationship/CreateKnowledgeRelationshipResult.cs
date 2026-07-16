using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateKnowledgeRelationship;

public sealed record CreateKnowledgeRelationshipResult(
  KnowledgeRelationshipId KnowledgeRelationshipId,
  bool ContradictionDetected);
