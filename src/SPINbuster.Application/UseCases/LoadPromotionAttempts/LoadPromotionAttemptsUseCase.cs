using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;

namespace SPINbuster.Application.UseCases.LoadPromotionAttempts;

public sealed class LoadPromotionAttemptsUseCase
  : IQueryHandler<LoadPromotionAttemptsQuery, LoadPromotionAttemptsResult>
{
  private readonly IFragmentCandidateRepository _fragmentCandidateRepository;
  private readonly IPromotionAttemptRepository _promotionAttemptRepository;

  public LoadPromotionAttemptsUseCase(
    IPromotionAttemptRepository promotionAttemptRepository,
    IFragmentCandidateRepository fragmentCandidateRepository)
  {
    _promotionAttemptRepository = promotionAttemptRepository;
    _fragmentCandidateRepository = fragmentCandidateRepository;
  }

  public async Task<LoadPromotionAttemptsResult> HandleAsync(
    LoadPromotionAttemptsQuery query,
    CancellationToken cancellationToken = default)
  {
    var candidate = await _fragmentCandidateRepository.GetByIdAsync(
      query.FragmentCandidateId,
      cancellationToken);

    if (candidate is null || candidate.ProjectId != query.ProjectId)
    {
      return new LoadPromotionAttemptsResult([]);
    }

    var attempts = await _promotionAttemptRepository.GetByFragmentCandidateAsync(
      query.FragmentCandidateId,
      cancellationToken);

    var results = attempts
      .OrderBy(a => a.AttemptedAtUtc)
      .ThenBy(a => a.Id.Value)
      .Take(query.MaxResults)
      .Select(a => new PromotionAttemptResult(
        a.Id,
        a.RecordId,
        a.Outcome,
        a.DiagnosticId,
        a.FragmentCandidateId,
        a.ContentHash,
        a.AttemptedAtUtc,
        a.FailureReason))
      .ToArray();

    return new LoadPromotionAttemptsResult(results);
  }
}
