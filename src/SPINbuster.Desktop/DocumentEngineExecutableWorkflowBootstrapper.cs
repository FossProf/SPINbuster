using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Infrastructure.Services;

namespace SPINbuster.Desktop;

public static class DocumentEngineExecutableWorkflowBootstrapper
{
  public static async Task<DocumentEngineExecutableWorkflowResult> RunAsync(
    IServiceProvider rootServiceProvider,
    CancellationToken cancellationToken = default)
  {
    await MigrateAsync(rootServiceProvider, cancellationToken);

    await using var scope = rootServiceProvider.CreateAsyncScope();
    var workflowRunner = scope.ServiceProvider.GetRequiredService<DocumentEngineExecutableWorkflowRunner>();
    return await workflowRunner.RunAsync(cancellationToken);
  }

  public static async Task MigrateAsync(
    IServiceProvider rootServiceProvider,
    CancellationToken cancellationToken = default)
  {
    await using var scope = rootServiceProvider.CreateAsyncScope();
    var migrator = scope.ServiceProvider.GetRequiredService<ISpinbusterDatabaseMigrator>();
    await migrator.MigrateAsync(cancellationToken);
  }
}
