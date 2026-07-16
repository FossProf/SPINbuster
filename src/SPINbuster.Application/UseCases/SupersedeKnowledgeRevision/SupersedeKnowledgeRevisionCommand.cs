using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.SupersedeKnowledgeRevision;

public sealed record SupersedeKnowledgeRevisionCommand(
  KnowledgeDocumentId KnowledgeDocumentId,
  KnowledgeDocumentRevisionId SupersededRevisionId,
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
  : ICommand<SupersedeKnowledgeRevisionResult>;
