using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.SupersedeKnowledgeRevision;

public sealed record SupersedeKnowledgeRevisionResult(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeDocumentRevisionId SuccessorRevisionId,
  KnowledgeDocumentRevisionId SupersededRevisionId,
  KnowledgeDocumentRevisionId CurrentAuthoritativeRevisionId);
