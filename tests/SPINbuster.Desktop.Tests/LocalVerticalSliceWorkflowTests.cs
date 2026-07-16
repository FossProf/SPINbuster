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
      "photo-01.jpg",
      "image/jpeg",
      "evidence/photo-01.jpg",
      "sha256:deterministic",
      "Deterministic interpretation summary.",
      "Initial Draft Report",
      "Summary",
      "Deterministic report summary.",
      "Observations",
      "Deterministic report observations.",
      Guid.Parse("0f74d133-75a0-4cf3-9d80-1f66144d96ac"),
      new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero));

    try
    {
      using (var serviceProvider = CreateServiceProvider(connectionString, settings))
      {
        var result = await DesktopWorkflowBootstrapper.RunAsync(serviceProvider);

        Assert.True(File.Exists(databasePath));
        Assert.Equal(ProjectLifecycle.Active, result.PersistedInspectionSnapshot.Project.Lifecycle);
        Assert.Equal(InspectionSessionLifecycle.InProgress, result.PersistedInspectionSnapshot.InspectionSession.Lifecycle);
        Assert.Single(result.PersistedInspectionSnapshot.InspectionSession.FieldNotes);
        Assert.Equal(
          settings.FieldNoteText,
          result.PersistedInspectionSnapshot.InspectionSession.FieldNotes.Single().RawText);
        Assert.Equal(
          ["ProjectCreated", "ProjectActivated"],
          result.PersistedInspectionSnapshot.Project.AuditHistory.Select(entry => entry.EventType));
        Assert.Equal(
          ["InspectionSessionCreated", "InspectionSessionStarted", "FieldNoteRecorded", "EvidenceAttached", "EvidenceInterpreted"],
          result.PersistedInspectionSnapshot.InspectionSession.AuditHistory.Select(entry => entry.EventType));
        Assert.Equal(ReportLifecycle.Draft, result.PersistedReportSnapshot.Lifecycle);
        Assert.Equal(settings.DraftTitle, result.PersistedReportSnapshot.Title);
        Assert.Equal(["Summary", "Observations"], result.PersistedReportSnapshot.Sections.Select(section => section.Heading));
        Assert.Single(result.PersistedReportSnapshot.FieldNotes);
        Assert.Single(result.PersistedReportSnapshot.EvidenceAttachments);
        Assert.Equal("ReportCreated", result.PersistedReportSnapshot.AuditHistory.Single().EventType);
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
      "photo-01.jpg",
      "image/jpeg",
      "evidence/photo-01.jpg",
      "sha256:deterministic",
      "Deterministic interpretation summary.",
      "Initial Draft Report",
      "Summary",
      "Deterministic report summary.",
      "Observations",
      "Deterministic report observations.",
      Guid.Parse("11111111-2222-3333-4444-555555555555"),
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
        var reloadedInspectionSnapshot = await queryHandler.HandleAsync(
          new LoadInspectionWorkflowSnapshotQuery(
            result.CreatedProject.ProjectId,
            result.StartedInspectionSession.InspectionSessionId));
        var reportQueryHandler = scope.ServiceProvider.GetRequiredService<
          IQueryHandler<SPINbuster.Application.UseCases.LoadReportDraftSnapshot.LoadReportDraftSnapshotQuery, SPINbuster.Application.UseCases.LoadReportDraftSnapshot.LoadReportDraftSnapshotResult>>();
        var reloadedReportSnapshot = await reportQueryHandler.HandleAsync(
          new SPINbuster.Application.UseCases.LoadReportDraftSnapshot.LoadReportDraftSnapshotQuery(
            result.CreatedReportDraft.ReportId));

        Assert.Equal(result.CreatedProject.ProjectId, reloadedInspectionSnapshot.Project.ProjectId);
        Assert.Equal(result.StartedInspectionSession.InspectionSessionId, reloadedInspectionSnapshot.InspectionSession.InspectionSessionId);
        Assert.Equal(firstSettings.ProjectName, reloadedInspectionSnapshot.Project.Name);
        Assert.Equal(firstSettings.FieldNoteText, reloadedInspectionSnapshot.InspectionSession.FieldNotes.Single().RawText);
        Assert.Equal(
          result.PersistedInspectionSnapshot.InspectionSession.AuditHistory.Select(entry => entry.AuditEventId),
          reloadedInspectionSnapshot.InspectionSession.AuditHistory.Select(entry => entry.AuditEventId));
        Assert.Equal(result.CreatedReportDraft.ReportId, reloadedReportSnapshot.ReportId);
        Assert.Equal(firstSettings.DraftTitle, reloadedReportSnapshot.Title);
        Assert.Equal(
          result.PersistedReportSnapshot.AuditHistory.Select(entry => entry.AuditEventId),
          reloadedReportSnapshot.AuditHistory.Select(entry => entry.AuditEventId));
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
