using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqlitePromotionAttemptRepository : IPromotionAttemptRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqlitePromotionAttemptRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<PromotionAttempt?> GetByIdAsync(
    PromotionAttemptId promotionAttemptId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.PromotionAttempts
      .AsNoTracking()
      .SingleOrDefaultAsync(record => record.Id == promotionAttemptId, cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<PromotionAttempt?> GetLatestSuccessfulByRecordIdAsync(
    PromotionRecordId promotionRecordId,
    CancellationToken cancellationToken = default)
  {
    var record = (await _dbContext.PromotionAttempts
      .AsNoTracking()
      .Where(record => record.RecordId == promotionRecordId && record.Outcome == PromotionAttemptOutcome.Promoted)
      .ToArrayAsync(cancellationToken))
      .OrderByDescending(record => record.AttemptedAtUtc)
      .ThenBy(record => record.Id.Value)
      .FirstOrDefault();

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<IReadOnlyList<PromotionAttempt>> GetByFragmentCandidateAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default)
  {
    var records = (await _dbContext.PromotionAttempts
      .AsNoTracking()
      .Where(record => record.FragmentCandidateId == fragmentCandidateId)
      .ToArrayAsync(cancellationToken))
      .OrderBy(record => record.AttemptedAtUtc)
      .ThenBy(record => record.Id.Value)
      .ToArray();

    return records.Select(InfrastructureMapper.ToDomain).ToArray();
  }

  public Task AddAsync(
    PromotionAttempt promotionAttempt,
    CancellationToken cancellationToken = default)
  {
    _dbContext.PromotionAttempts.Add(InfrastructureMapper.ToRecord(promotionAttempt));
    return Task.CompletedTask;
  }
}
