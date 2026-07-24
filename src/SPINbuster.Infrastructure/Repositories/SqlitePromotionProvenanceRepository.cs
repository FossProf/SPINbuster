using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqlitePromotionProvenanceRepository : IPromotionProvenanceRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqlitePromotionProvenanceRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<PromotionProvenance?> GetByRevisionIdAsync(
    KnowledgeDocumentRevisionId revisionId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.PromotionProvenances
      .AsNoTracking()
      .Where(provenance => provenance.PromotedRevisionId == revisionId)
      .FirstOrDefaultAsync(cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public async Task<PromotionProvenance?> GetByFragmentCandidateIdAsync(
    FragmentCandidateId fragmentCandidateId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.PromotionProvenances
      .AsNoTracking()
      .Where(provenance => provenance.FragmentCandidateId == fragmentCandidateId)
      .FirstOrDefaultAsync(cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public Task AddAsync(
    PromotionProvenance provenance,
    CancellationToken cancellationToken = default)
  {
    _dbContext.PromotionProvenances.Add(InfrastructureMapper.ToRecord(provenance));
    return Task.CompletedTask;
  }
}
