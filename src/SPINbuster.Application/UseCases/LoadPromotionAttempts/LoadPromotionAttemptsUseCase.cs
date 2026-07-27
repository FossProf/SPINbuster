using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;

namespace SPINbuster.Application.UseCases.LoadPromotionAttempts;

public sealed class LoadPromotionAttemptsUseCase
  : IQueryHandler<LoadPromotionAttemptsQuery, LoadPromotionAttemptsResult>
{
  private readonly IPromotionAttemptRepository _promotionAttemptRepository;

  public LoadPromotionAttemptsUseCase(IPromotionAttemptRepository promotionAttemptRepository)
  {
    _promotionAttemptRepository = promotionAttemptRepository;
  }

  public async Task<LoadPromotionAttemptsResult> HandleAsync(
    LoadPromotionAttemptsQuery query,
    CancellationToken cancellationToken = default)
  {
    var attempts = await _promotionAttemptRepository.GetByFragmentCandidateAsync(
      query.FragmentCandidateId,
      cancellationToken);

    var results = attempts.Select(a => new PromotionAttemptResult(
      a.Id,
      a.RecordId,
      a.Outcome,
      a.DiagnosticId,
      a.FragmentCandidateId,
      a.ContentHash,
      a.AttemptedAtUtc,
      a.FailureReason)).ToArray();

    return new LoadPromotionAttemptsResult(results);
  }
}
