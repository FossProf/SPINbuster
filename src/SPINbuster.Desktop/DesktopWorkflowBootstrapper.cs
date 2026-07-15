using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Infrastructure.Persistence;

namespace SPINbuster.Desktop;

public static class DesktopWorkflowBootstrapper
{
  public static async Task<LocalVerticalSliceWorkflowResult> RunAsync(
    IServiceProvider rootServiceProvider,
    CancellationToken cancellationToken = default)
  {
    await MigrateAsync(rootServiceProvider, cancellationToken);

    await using var scope = rootServiceProvider.CreateAsyncScope();
    var workflowRunner = scope.ServiceProvider.GetRequiredService<LocalVerticalSliceWorkflowRunner>();
    return await workflowRunner.RunAsync(cancellationToken);
  }

  public static async Task MigrateAsync(
    IServiceProvider rootServiceProvider,
    CancellationToken cancellationToken = default)
  {
    await using var scope = rootServiceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SpinbusterDbContext>();
    await dbContext.Database.MigrateAsync(cancellationToken);
  }
}
