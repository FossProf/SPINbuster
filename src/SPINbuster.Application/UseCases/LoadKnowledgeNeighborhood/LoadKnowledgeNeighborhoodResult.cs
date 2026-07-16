using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeNeighborhood;

public sealed record LoadKnowledgeNeighborhoodResult(
  KnowledgeSubjectReference Anchor,
  IReadOnlyCollection<KnowledgeNeighborhoodNode> Nodes,
  IReadOnlyCollection<KnowledgeNeighborhoodRelationship> Relationships);

public sealed record KnowledgeNeighborhoodNode(
  KnowledgeSubjectReference Subject,
  string DisplayLabel,
  KnowledgeDocumentType? DocumentType,
  string? RevisionLabel);

public sealed record KnowledgeNeighborhoodRelationship(
  KnowledgeRelationshipId KnowledgeRelationshipId,
  KnowledgeSubjectReference Source,
  KnowledgeSubjectReference Target,
  KnowledgeRelationshipType RelationshipType,
  KnowledgeVerificationStatus VerificationStatus,
  string EvidenceOrRationale);
