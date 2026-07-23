using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.PromoteFragmentCandidate;

public sealed record PromoteFragmentCandidateResult(
  PromotionDiagnosticId PromotionDiagnosticId,
  PromotionDiagnosticStatus Status,
  KnowledgeDocumentId? KnowledgeDocumentId,
  KnowledgeDocumentRevisionId? KnowledgeDocumentRevisionId,
  KnowledgeCitationId? KnowledgeCitationId,
  bool SupersededExistingRevision,
  KnowledgeDocumentRevisionId? SupersededRevisionId,
  string? FailureReason);
