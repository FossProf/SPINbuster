using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionProvenance;

public sealed class LoadPromotionProvenanceUseCase
  : IQueryHandler<LoadPromotionProvenanceQuery, LoadPromotionProvenanceResult>
{
  private readonly IPromotionProvenanceRepository _provenanceRepository;

  public LoadPromotionProvenanceUseCase(IPromotionProvenanceRepository provenanceRepository)
  {
    _provenanceRepository = provenanceRepository;
  }

  public async Task<LoadPromotionProvenanceResult> HandleAsync(
    LoadPromotionProvenanceQuery query,
    CancellationToken cancellationToken = default)
  {
    PromotionProvenance? provenance = null;

    if (query.RevisionId is not null)
    {
      provenance = await _provenanceRepository.GetByRevisionIdAsync(
        query.RevisionId.Value,
        cancellationToken);
    }
    else if (query.FragmentCandidateId is not null)
    {
      provenance = await _provenanceRepository.GetByFragmentCandidateIdAsync(
        query.FragmentCandidateId.Value,
        cancellationToken);
    }

    if (provenance is null)
    {
      var key = query.RevisionId?.ToString() ?? query.FragmentCandidateId?.ToString() ?? "unknown";
      throw new ApplicationEntityNotFoundException(nameof(PromotionProvenance), key);
    }

    return new LoadPromotionProvenanceResult(
      provenance.Id,
      provenance.ProjectId,
      provenance.PromotedRevisionId,
      provenance.DiagnosticId,
      provenance.FragmentCandidateId,
      provenance.FragmentSourceContentHash,
      provenance.ReviewState,
      provenance.ReviewedBy,
      provenance.ReviewedAtUtc,
      provenance.ParserRunId,
      provenance.ParserKey,
      provenance.ParserVersion,
      provenance.ParserContractVersion,
      provenance.ParserContractHash,
      provenance.ImportedSourceId,
      provenance.ImportedSourceContentHash,
      provenance.PromotionIdentityHash,
      provenance.PromotionAttemptId,
      provenance.PromotedBy,
      provenance.PromotedAtUtc);
  }
}
