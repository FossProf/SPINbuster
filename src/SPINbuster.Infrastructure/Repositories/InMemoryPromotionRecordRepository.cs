using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Repositories;

/// <summary>
/// Temporary in-memory implementation of IPromotionRecordRepository.
/// Will be replaced by an EF Core implementation in a later Work Order.
/// </summary>
public sealed class InMemoryPromotionRecordRepository : IPromotionRecordRepository
{
  private readonly Dictionary<PromotionRecordId, PromotionRecord> _records = new();

  public Task<PromotionRecord?> GetByIdAsync(
    PromotionRecordId promotionRecordId,
    CancellationToken cancellationToken = default)
  {
    _records.TryGetValue(promotionRecordId, out var record);
    return Task.FromResult(record);
  }

  public Task<PromotionRecord?> FindByIdentityHashAsync(
    ProjectId projectId,
    string identityHash,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _records.Values.FirstOrDefault(record =>
        record.Identity.ProjectId == projectId
        && string.Equals(record.Identity.Hash, identityHash, StringComparison.Ordinal)));
  }

  public Task AddAsync(
    PromotionRecord promotionRecord,
    CancellationToken cancellationToken = default)
  {
    _records[promotionRecord.Id] = promotionRecord;
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    PromotionRecord promotionRecord,
    CancellationToken cancellationToken = default)
  {
    _records[promotionRecord.Id] = promotionRecord;
    return Task.CompletedTask;
  }
}
