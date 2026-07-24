using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IPromotionRecordRepository
{
  Task<PromotionRecord?> GetByIdAsync(
    PromotionRecordId promotionRecordId,
    CancellationToken cancellationToken = default);

  Task<PromotionRecord?> FindByIdentityHashAsync(
    ProjectId projectId,
    string identityHash,
    CancellationToken cancellationToken = default);

  Task AddAsync(
    PromotionRecord promotionRecord,
    CancellationToken cancellationToken = default);

  Task UpdateAsync(
    PromotionRecord promotionRecord,
    CancellationToken cancellationToken = default);
}
