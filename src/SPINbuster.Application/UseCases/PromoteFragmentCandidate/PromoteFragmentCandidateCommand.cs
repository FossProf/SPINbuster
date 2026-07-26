using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.PromoteFragmentCandidate;

public sealed record PromoteFragmentCandidateCommand(
  FragmentCandidateId FragmentCandidateId,
  KnowledgeDocumentType DocumentType,
  string CanonicalTitle,
  string? ExternalReferenceNumber,
  string? DisciplineOrCategory) : ICommand<PromoteFragmentCandidateResult>;
