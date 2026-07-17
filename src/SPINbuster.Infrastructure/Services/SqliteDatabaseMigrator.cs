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

  public Task MigrateAsync(CancellationToken cancellationToken = default)
  {
    return _dbContext.Database.MigrateAsync(cancellationToken);
  }
}
