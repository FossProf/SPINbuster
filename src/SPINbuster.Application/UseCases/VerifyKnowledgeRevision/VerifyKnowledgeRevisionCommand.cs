using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.VerifyKnowledgeRevision;

public sealed record VerifyKnowledgeRevisionCommand(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeDocumentRevisionId KnowledgeDocumentRevisionId,
  KnowledgeVerificationStatus VerificationStatus)
  : ICommand<VerifyKnowledgeRevisionResult>;
