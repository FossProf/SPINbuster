using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.AcceptFragmentCandidate;

public sealed record AcceptFragmentCandidateCommand(
  FragmentCandidateId FragmentCandidateId,
  string? ReviewNotes) : ICommand<AcceptFragmentCandidateResult>;
