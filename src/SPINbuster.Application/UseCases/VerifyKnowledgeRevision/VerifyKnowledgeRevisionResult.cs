using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.VerifyKnowledgeRevision;

public sealed record VerifyKnowledgeRevisionResult(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeDocumentRevisionId KnowledgeDocumentRevisionId,
  KnowledgeVerificationStatus VerificationStatus);
