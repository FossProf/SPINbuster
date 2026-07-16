using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Repositories;

public sealed class SqliteContextManifestRepository : IContextManifestRepository
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteContextManifestRepository(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<ContextManifest?> GetByIdAsync(
    ContextManifestId contextManifestId,
    CancellationToken cancellationToken = default)
  {
    var record = await _dbContext.ContextManifests
      .AsNoTracking()
      .Include(contextManifest => contextManifest.Entries)
      .AsSplitQuery()
      .SingleOrDefaultAsync(contextManifest => contextManifest.Id == contextManifestId, cancellationToken);

    return record is null ? null : InfrastructureMapper.ToDomain(record);
  }

  public Task AddAsync(ContextManifest contextManifest, CancellationToken cancellationToken = default)
  {
    _dbContext.ContextManifests.Add(InfrastructureMapper.ToRecord(contextManifest));
    return Task.CompletedTask;
  }
}
