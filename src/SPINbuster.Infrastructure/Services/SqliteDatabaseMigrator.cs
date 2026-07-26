using Microsoft.EntityFrameworkCore;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Infrastructure.Services;

public sealed class SqliteDatabaseMigrator : ISpinbusterDatabaseMigrator
{
  private readonly SpinbusterDbContext _dbContext;

  public SqliteDatabaseMigrator(SpinbusterDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task MigrateAsync(CancellationToken cancellationToken = default)
  {
    await SpinbusterDbContext.MigrateWithSha256Async(_dbContext, cancellationToken);
  }
}
