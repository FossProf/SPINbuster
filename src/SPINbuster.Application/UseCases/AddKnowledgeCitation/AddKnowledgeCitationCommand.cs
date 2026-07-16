using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddKnowledgeCitation;

public sealed record AddKnowledgeCitationCommand(
  ProjectId ProjectId,
  KnowledgeDocumentRevisionId KnowledgeDocumentRevisionId,
  KnowledgeCitationLocationType LocatorType,
  string LocatorValue,
  string? QuotedOrSummarizedText)
  : ICommand<AddKnowledgeCitationResult>;
