using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectDocumentCandidate;

public sealed record RejectDocumentCandidateResult(
  DocumentCandidateId DocumentCandidateId,
  DocumentCandidateStatus Status,
  string Reviewer,
  DateTimeOffset ReviewedAtUtc);
