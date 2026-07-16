using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeDocument;

public sealed record LoadKnowledgeDocumentQuery(KnowledgeDocumentId KnowledgeDocumentId)
  : IQuery<LoadKnowledgeDocumentResult>;
