using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RecordDocumentCandidateReview;

public sealed record RecordDocumentCandidateReviewResult(
  DocumentCandidateId DocumentCandidateId,
  DocumentCandidateStatus Status,
  string Reviewer,
  DateTimeOffset ReviewedAtUtc);
