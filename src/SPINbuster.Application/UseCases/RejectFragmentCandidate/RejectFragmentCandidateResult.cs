using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectFragmentCandidate;

public sealed record RejectFragmentCandidateResult(
  FragmentCandidateId FragmentCandidateId,
  FragmentCandidateReviewState ReviewState,
  string Reviewer,
  DateTimeOffset ReviewedAtUtc);
