using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeRevisionHistory;

public sealed record LoadKnowledgeRevisionHistoryQuery(KnowledgeDocumentId KnowledgeDocumentId)
  : IQuery<LoadKnowledgeRevisionHistoryResult>;
