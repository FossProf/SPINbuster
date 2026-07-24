using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionProvenance;

public sealed record LoadPromotionProvenanceQuery : IQuery<LoadPromotionProvenanceResult>
{
  public KnowledgeDocumentRevisionId? RevisionId { get; init; }

  public FragmentCandidateId? FragmentCandidateId { get; init; }
}
