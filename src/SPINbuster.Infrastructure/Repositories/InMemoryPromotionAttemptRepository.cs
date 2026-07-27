using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Repositories;

/// <summary>
/// Temporary in-memory implementation of IPromotionAttemptRepository.
/// Will be replaced by an EF Core implementation in a later Work Order.
/// </summary>
public sealed class InMemoryPromotionAttemptRepository : IPromotionAttemptRepository
{
  private readonly Dictionary<PromotionAttemptId, PromotionAttempt> _attempts = new();

  public Task<PromotionAttempt?> GetByIdAsync(
    PromotionAttemptId promotionAttemptId,
    CancellationToken cancellationToken = default)
  {
    _attempts.TryGetValue(promotionAttemptId, out var attempt);
    return Task.FromResult(attempt);
  }

  public Task<PromotionAttempt?> GetLatestSuccessfulByRecordIdAsync(
    PromotionRecordId promotionRecordId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _attempts.Values
        .Where(attempt =>
          attempt.RecordId == promotionRecordId
          && attempt.Outcome == PromotionAttemptOutcome.Promoted)
        .OrderByDescending(attempt => attempt.AttemptedAtUtc)
        .FirstOrDefault());
  }

  public Task<IReadOnlyList<PromotionAttempt>> GetByFragmentCandidateAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default)
  {
    IReadOnlyList<PromotionAttempt> result = _attempts.Values
      .Where(attempt => attempt.FragmentCandidateId == fragmentCandidateId)
      .OrderBy(attempt => attempt.AttemptedAtUtc)
      .ThenBy(attempt => attempt.Id.Value)
      .ToArray();
    return Task.FromResult(result);
  }

  public Task AddAsync(
    PromotionAttempt promotionAttempt,
    CancellationToken cancellationToken = default)
  {
    _attempts[promotionAttempt.Id] = promotionAttempt;
    return Task.CompletedTask;
  }
}
