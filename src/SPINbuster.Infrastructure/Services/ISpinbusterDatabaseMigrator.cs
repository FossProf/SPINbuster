namespace SPINbuster.Infrastructure.Services;

public interface ISpinbusterDatabaseMigrator
{
  Task MigrateAsync(CancellationToken cancellationToken = default);
}
