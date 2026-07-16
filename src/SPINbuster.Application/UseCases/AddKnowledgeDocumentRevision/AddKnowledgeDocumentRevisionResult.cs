using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddKnowledgeDocumentRevision;

public sealed record AddKnowledgeDocumentRevisionResult(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeDocumentRevisionId KnowledgeDocumentRevisionId,
  KnowledgeDocumentRevisionId? CurrentAuthoritativeRevisionId,
  KnowledgeRevisionLifecycle Lifecycle);
