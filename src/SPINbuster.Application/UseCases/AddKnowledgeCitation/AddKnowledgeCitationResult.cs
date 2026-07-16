using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddKnowledgeCitation;

public sealed record AddKnowledgeCitationResult(
  KnowledgeCitationId KnowledgeCitationId,
  KnowledgeDocumentRevisionId KnowledgeDocumentRevisionId,
  KnowledgeCitationLocationType LocatorType,
  string LocatorValue);
