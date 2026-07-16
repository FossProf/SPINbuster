using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AddKnowledgeDocumentRevision;

public sealed record AddKnowledgeDocumentRevisionCommand(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeSourceId KnowledgeSourceId,
  string RevisionLabel,
  DateOnly? EffectiveDate,
  DateTimeOffset ReceivedAtUtc,
  KnowledgeSourceAuthorityLevel SourceAuthority,
  string ContentHash,
  string MetadataHash,
  string? SourceSystemReference,
  string? DescriptiveNotes,
  KnowledgeIngestionStatus IngestionStatus)
  : ICommand<AddKnowledgeDocumentRevisionResult>;
