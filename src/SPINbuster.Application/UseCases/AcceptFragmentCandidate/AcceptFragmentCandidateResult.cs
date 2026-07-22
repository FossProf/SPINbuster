using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AcceptFragmentCandidate;

public sealed record AcceptFragmentCandidateResult(
  FragmentCandidateId FragmentCandidateId,
  FragmentCandidateReviewState ReviewState,
  string Reviewer,
  DateTimeOffset ReviewedAtUtc);
