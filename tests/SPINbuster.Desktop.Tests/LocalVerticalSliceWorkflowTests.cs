using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Domain;

namespace SPINbuster.Desktop.Tests;

public sealed class LocalVerticalSliceWorkflowTests
{
  [Fact]
  public async Task WorkflowBootstrapperAppliesMigrationsAndReloadsPersistedState()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";
    var settings = new DesktopWorkflowSettings(
      "desktop.bootstrap@local.invalid",
      "Local Vertical Slice",
      "Initial Inspection Session",
      "Observed deterministic bootstrap workflow note.",
      new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero));

    try
    {
      using (var serviceProvider = CreateServiceProvider(connectionString, settings))
      {
        var result = await DesktopWorkflowBootstrapper.RunAsync(serviceProvider);

        Assert.True(File.Exists(databasePath));
        Assert.Equal(ProjectLifecycle.Active, result.PersistedSnapshot.Project.Lifecycle);
        Assert.Equal(InspectionSessionLifecycle.InProgress, result.PersistedSnapshot.InspectionSession.Lifecycle);
        Assert.Single(result.PersistedSnapshot.InspectionSession.FieldNotes);
        Assert.Equal(
          settings.FieldNoteText,
          result.PersistedSnapshot.InspectionSession.FieldNotes.Single().RawText);
        Assert.Equal(
          ["ProjectCreated", "ProjectActivated"],
          result.PersistedSnapshot.Project.AuditHistory.Select(entry => entry.EventType));
        Assert.Equal(
          ["InspectionSessionCreated", "InspectionSessionStarted", "FieldNoteRecorded"],
          result.PersistedSnapshot.InspectionSession.AuditHistory.Select(entry => entry.EventType));
      }
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowDataCanBeReloadedFromAFreshServiceProvider()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";
    var firstSettings = new DesktopWorkflowSettings(
      "desktop.bootstrap@local.invalid",
      "Fresh Provider Validation",
      "Reloadable Session",
      "Persist this note across providers.",
      new DateTimeOffset(2026, 7, 15, 15, 0, 0, TimeSpan.Zero));

    try
    {
      LocalVerticalSliceWorkflowResult result;
      using (var firstProvider = CreateServiceProvider(connectionString, firstSettings))
      {
        result = await DesktopWorkflowBootstrapper.RunAsync(firstProvider);
      }

      var secondSettings = firstSettings with
      {
        InitialTimestampUtc = firstSettings.InitialTimestampUtc.AddHours(2),
      };

      using (var secondProvider = CreateServiceProvider(connectionString, secondSettings))
      {
        await DesktopWorkflowBootstrapper.MigrateAsync(secondProvider);

        await using var scope = secondProvider.CreateAsyncScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<
          IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult>>();
        var reloadedSnapshot = await queryHandler.HandleAsync(
          new LoadInspectionWorkflowSnapshotQuery(
            result.CreatedProject.ProjectId,
            result.StartedInspectionSession.InspectionSessionId));

        Assert.Equal(result.CreatedProject.ProjectId, reloadedSnapshot.Project.ProjectId);
        Assert.Equal(result.StartedInspectionSession.InspectionSessionId, reloadedSnapshot.InspectionSession.InspectionSessionId);
        Assert.Equal(firstSettings.ProjectName, reloadedSnapshot.Project.Name);
        Assert.Equal(firstSettings.FieldNoteText, reloadedSnapshot.InspectionSession.FieldNotes.Single().RawText);
        Assert.Equal(
          result.PersistedSnapshot.InspectionSession.AuditHistory.Select(entry => entry.AuditEventId),
          reloadedSnapshot.InspectionSession.AuditHistory.Select(entry => entry.AuditEventId));
      }
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  private static ServiceProvider CreateServiceProvider(
    string connectionString,
    DesktopWorkflowSettings settings)
  {
    var services = new ServiceCollection();
    DesktopCompositionRoot.ConfigureServices(services, connectionString, settings);
    return services.BuildServiceProvider();
  }

  private static void DeleteIfPresent(string databasePath)
  {
    SqliteConnection.ClearAllPools();

    if (File.Exists(databasePath))
    {
      for (var attempt = 0; attempt < 5; attempt++)
      {
        try
        {
          File.Delete(databasePath);
          break;
        }
        catch (IOException) when (attempt < 4)
        {
          Thread.Sleep(100);
        }
        catch (IOException)
        {
          // Temp SQLite cleanup is best-effort in tests because the workflow
          // assertions above already proved persistence behavior.
          break;
        }
      }
    }
  }
}
