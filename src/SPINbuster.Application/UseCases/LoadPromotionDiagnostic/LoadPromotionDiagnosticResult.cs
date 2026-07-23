using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionDiagnostic;

public sealed record LoadPromotionDiagnosticResult(
  PromotionDiagnosticId Id,
  FragmentCandidateId FragmentCandidateId,
  ParserRunId ParserRunId,
  ProjectId ProjectId,
  PromotionDiagnosticStatus Status,
  string? FailureReason,
  KnowledgeDocumentId? KnowledgeDocumentId,
  KnowledgeDocumentRevisionId? KnowledgeDocumentRevisionId,
  KnowledgeCitationId? KnowledgeCitationId,
  bool SupersededExistingRevision,
  KnowledgeDocumentRevisionId? SupersededRevisionId,
  DateTimeOffset PromotedAtUtc);
