using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqlitePromotionDiagnosticRepository : IPromotionDiagnosticRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqlitePromotionDiagnosticRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<PromotionDiagnostic?> GetByIdAsync(
    PromotionDiagnosticId promotionDiagnosticId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.PromotionDiagnostics
      .AsNoTracking()
      .Where(diagnostic => diagnostic.Id == promotionDiagnosticId)
      .FirstOrDefaultAsync(cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<PromotionDiagnostic?> GetByFragmentCandidateAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.PromotionDiagnostics
      .AsNoTracking()
      .Where(diagnostic => diagnostic.FragmentCandidateId == fragmentCandidateId)
      .FirstOrDefaultAsync(cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<IReadOnlyCollection<PromotionDiagnostic>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    var records = await _dbContext.PromotionDiagnostics
      .AsNoTracking()
      .Where(diagnostic => diagnostic.ProjectId == projectId)
      .ToArrayAsync(cancellationToken);

    return records
      .OrderByDescending(diagnostic => diagnostic.PromotedAtUtc)
      .ThenBy(diagnostic => diagnostic.Id.Value)
      .Take(maxResults)
      .Select(InfrastructureMapper.ToDomain)
      .ToArray();
  }

  public async Task<PromotionDiagnostic?> FindSuccessfulByContentHashAsync(
    ProjectId projectId,
    string contentHash,
    string normalizedLocatorValue,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.PromotionDiagnostics
      .AsNoTracking()
      .Join(
        _dbContext.FragmentCandidates,
        diagnostic => diagnostic.FragmentCandidateId,
        candidate => candidate.Id,
        (diagnostic, candidate) => new { diagnostic, candidate })
      .Where(pair =>
        pair.diagnostic.ProjectId == projectId
        && pair.diagnostic.Status == PromotionDiagnosticStatus.Promoted
        && pair.candidate.SourceContentHash == contentHash
        && pair.candidate.LocatorNormalizedValue == normalizedLocatorValue)
      .Select(pair => pair.diagnostic)
      .FirstOrDefaultAsync(cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public Task AddAsync(
    PromotionDiagnostic promotionDiagnostic,
    CancellationToken cancellationToken = default)
  {
    _dbContext.PromotionDiagnostics.Add(InfrastructureMapper.ToRecord(promotionDiagnostic));
    return Task.CompletedTask;
  }

  public Task UpdateAsync(
    PromotionDiagnostic promotionDiagnostic,
    CancellationToken cancellationToken = default)
  {
    _dbContext.PromotionDiagnostics.Update(InfrastructureMapper.ToRecord(promotionDiagnostic));
    return Task.CompletedTask;
  }
}
