using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateKnowledgeRelationship;

public sealed record CreateKnowledgeRelationshipCommand(
  ProjectId ProjectId,
  KnowledgeSubjectReference Source,
  KnowledgeSubjectReference Target,
  KnowledgeRelationshipType RelationshipType,
  string EvidenceOrRationale)
  : ICommand<CreateKnowledgeRelationshipResult>;
