using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectFragmentCandidate;

public sealed record RejectFragmentCandidateCommand(
  FragmentCandidateId FragmentCandidateId,
  string? ReviewNotes) : ICommand<RejectFragmentCandidateResult>;
