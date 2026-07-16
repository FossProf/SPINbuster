using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RegisterKnowledgeDocument;

public sealed record RegisterKnowledgeDocumentCommand(
  ProjectId ProjectId,
  KnowledgeDocumentType DocumentType,
  string CanonicalTitle,
  string? ExternalReferenceNumber,
  string? DisciplineOrCategory) : ICommand<RegisterKnowledgeDocumentResult>;
