using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RegisterKnowledgeDocument;

public sealed record RegisterKnowledgeDocumentResult(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeDocumentLifecycle Lifecycle,
  KnowledgeDocumentType DocumentType,
  string CanonicalTitle);
