using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IPromotionAttemptRepository
{
  Task<PromotionAttempt?> GetByIdAsync(
    PromotionAttemptId promotionAttemptId,
    CancellationToken cancellationToken = default);

  Task<PromotionAttempt?> GetLatestSuccessfulByRecordIdAsync(
    PromotionRecordId promotionRecordId,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    PromotionAttempt promotionAttempt,
    CancellationToken cancellationToken = default);
}
