using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionProvenance;

public sealed record LoadPromotionProvenanceResult(
  PromotionProvenanceId Id,
  ProjectId ProjectId,
  KnowledgeDocumentRevisionId PromotedRevisionId,
  PromotionDiagnosticId DiagnosticId,
  FragmentCandidateId FragmentCandidateId,
  string FragmentSourceContentHash,
  FragmentCandidateReviewState ReviewState,
  string? ReviewedBy,
  DateTimeOffset? ReviewedAtUtc,
  ParserRunId ParserRunId,
  string ParserKey,
  string ParserVersion,
  string ParserContractVersion,
  string ParserContractHash,
  ImportedSourceId ImportedSourceId,
  string ImportedSourceContentHash,
  string PromotionIdentityHash,
  PromotionAttemptId PromotionAttemptId,
  string PromotedBy,
  DateTimeOffset PromotedAtUtc);
