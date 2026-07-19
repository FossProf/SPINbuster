using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using SPINbuster.Application;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Desktop;
using SPINbuster.Domain;
using SPINbuster.Documents;
using SPINbuster.Infrastructure.Persistence;
using System.Text;

namespace SPINbuster.Desktop.Tests;

public sealed class ParsingExecutableWorkflowTests
{
  private const string ParserKey = "plain-text-deterministic";
  private const string ParserContractVersion = "1.0.0";

  [Fact]
  public async Task ParsingWorkflowRunsAndProducesFragmentCandidates()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.True(File.Exists(environment.DatabasePath));
      Assert.Equal(ParserRunState.Completed, result.FirstParseResult.State);
      Assert.True(result.FirstParseResult.FragmentCandidateIds.Count > 0);
      Assert.Equal(ParserRunState.Completed, result.ReplayParseResult.State);
      Assert.Equal(result.FirstParseResult.ParserRunId, result.ReplayParseResult.ParserRunId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ParsingWorkflowIdempotentReplayReturnsSameRunAndCandidates()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(result.FirstParseResult.ParserRunId, result.ReplayParseResult.ParserRunId);
      Assert.Equal(result.FirstParseResult.State, result.ReplayParseResult.State);
      Assert.Equal(result.FirstParseResult.FragmentCandidateIds, result.ReplayParseResult.FragmentCandidateIds);
      Assert.Equal(result.FirstSnapshot.ParserRuns.Count, result.ReplaySnapshot.ParserRuns.Count);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ParsingSnapshotContainsParserKeyVersionAndLocators()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      var snapshot = result.FirstSnapshot;
      Assert.Single(snapshot.ParserRuns);
      var run = snapshot.ParserRuns[0];
      Assert.Equal(ParserKey, run.ParserKey);
      Assert.Equal("1.0.0", run.ParserVersion);
      Assert.Equal(ParserContractVersion, run.ParserContractVersion);
      Assert.False(string.IsNullOrWhiteSpace(run.ParserContractHash));
      Assert.True(run.FragmentCandidates.Count > 0);

      Assert.Contains(run.FragmentCandidates, c => c.LocatorType == FragmentLocatorType.WholeDocument);
      Assert.Contains(run.FragmentCandidates, c => c.LocatorType == FragmentLocatorType.Paragraph);
      Assert.Contains(run.FragmentCandidates, c => c.LocatorType == FragmentLocatorType.LineRange);

      Assert.True(run.AuditHistory.Count >= 2);
      Assert.Contains(run.AuditHistory, a => a.EventType == "ParserRunCreated");
      Assert.Contains(run.AuditHistory, a => a.EventType == "ParserRunCompleted");
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ParsingWorkflowSurvivesDisposeAndRecreateProvider()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      ParsingExecutableWorkflowResult firstResult;
      using (var firstProvider = CreateServiceProvider(environment))
      {
        firstResult = await ParsingExecutableWorkflowBootstrapper.RunAsync(firstProvider);
      }

      using var secondProvider = CreateServiceProvider(environment);
      await ParsingExecutableWorkflowBootstrapper.MigrateAsync(secondProvider);
      await using var scope = secondProvider.CreateAsyncScope();
      var loadSnapshot = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult>>();

      var reloadedSnapshot = await loadSnapshot.HandleAsync(
        new LoadParsingSnapshotQuery(
          firstResult.CreatedProject.ProjectId,
          firstResult.ImportedSource.ImportedSourceId));

      Assert.Single(reloadedSnapshot.ParserRuns);
      Assert.Equal(firstResult.FirstParseResult.ParserRunId, reloadedSnapshot.ParserRuns[0].ParserRunId);
      Assert.Equal(ParserRunState.Completed, reloadedSnapshot.ParserRuns[0].State);
      Assert.Equal(firstResult.FirstParseResult.FragmentCandidateIds.Count, reloadedSnapshot.ParserRuns[0].FragmentCandidates.Count);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ParsingWorkflowRunsTwiceAgainstSameDatabasePreservingPriorData()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      ParsingExecutableWorkflowResult firstRun;
      using (var firstProvider = CreateServiceProvider(environment))
      {
        firstRun = await ParsingExecutableWorkflowBootstrapper.RunAsync(firstProvider);
      }

      using var secondProvider = CreateServiceProvider(environment);
      var secondResult = await ParsingExecutableWorkflowBootstrapper.RunAsync(secondProvider);
      await using var scope = secondProvider.CreateAsyncScope();
      var loadSnapshot = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult>>();
      var preservedFirstRunSnapshot = await loadSnapshot.HandleAsync(
        new LoadParsingSnapshotQuery(
          firstRun.CreatedProject.ProjectId,
          firstRun.ImportedSource.ImportedSourceId));

      var preservedFirstRun = preservedFirstRunSnapshot.ParserRuns
        .SingleOrDefault(r => r.ParserRunId == firstRun.FirstParseResult.ParserRunId);

      Assert.NotNull(preservedFirstRun);
      Assert.Equal(ParserRunState.Completed, preservedFirstRun.State);
      Assert.Equal(firstRun.FirstParseResult.FragmentCandidateIds.Count, preservedFirstRun.FragmentCandidates.Count);

      Assert.NotEqual(firstRun.CreatedProject.ProjectId, secondResult.CreatedProject.ProjectId);
      Assert.NotEqual(firstRun.ImportedSource.ImportedSourceId, secondResult.ImportedSource.ImportedSourceId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task UnsupportedMediaProducesFailedParseWithoutCrashing()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(ParserRunState.Failed, result.UnsupportedMediaResult.State);
      Assert.NotEqual(ParserRunFailureClassification.None, result.UnsupportedMediaResult.FailureClassification);
      Assert.Empty(result.UnsupportedMediaResult.FragmentCandidateIds);
      Assert.False(string.IsNullOrWhiteSpace(result.UnsupportedMediaResult.FailureDetails));
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task CancelledParseProducesCancelledStateWithoutCrashing()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(ParserRunState.Cancelled, result.CancelledParseResult.State);
      Assert.Equal(ParserRunFailureClassification.Cancelled, result.CancelledParseResult.FailureClassification);
      Assert.Empty(result.CancelledParseResult.FragmentCandidateIds);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task MalformedOutputProducesFailedParseWithoutCrashing()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(ParserRunState.Failed, result.MalformedOutputResult.State);
      Assert.NotEqual(ParserRunFailureClassification.None, result.MalformedOutputResult.FailureClassification);
      Assert.Empty(result.MalformedOutputResult.FragmentCandidateIds);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ParserVersionsCanCoexistAndPreserveHistoricalCandidates()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Single(result.FinalSnapshot.ParserRuns);
      var run = result.FinalSnapshot.ParserRuns[0];
      Assert.Equal(ParserKey, run.ParserKey);
      Assert.Equal("1.0.0", run.ParserVersion);
      Assert.Equal(ParserContractVersion, run.ParserContractVersion);
      Assert.Equal(ParserRunState.Completed, run.State);
      Assert.True(run.FragmentCandidates.Count > 0);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ConsoleFormatterProducesReadableOutputWithoutExposingPaths()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);
      var output = ParsingExecutableWorkflowConsoleFormatter.Format(result);

      Assert.Contains("Parsing and Fragment Foundation", output, StringComparison.Ordinal);
      Assert.Contains(ParserKey, output, StringComparison.Ordinal);
      Assert.Contains("1.0.0", output, StringComparison.Ordinal);
      Assert.Contains("WholeDocument", output, StringComparison.Ordinal);
      Assert.Contains("Completed", output, StringComparison.Ordinal);
      Assert.Contains("Authority Isolation", output, StringComparison.Ordinal);
      Assert.DoesNotContain(environment.StorageRootPath, output, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain(environment.DatabasePath, output, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("C:\\", output, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ParsingWorkflowDoesNotMutateKnowledgeReportOrAiRecords()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await ParsingExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      await using var scope = serviceProvider.CreateAsyncScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<SpinbusterDbContext>();
      await dbContext.Database.OpenConnectionAsync();

      async Task<long> CountAsync(string table)
      {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
      }

      Assert.Equal(0, await CountAsync("knowledge_documents"));
      Assert.Equal(0, await CountAsync("knowledge_document_revisions"));
      Assert.Equal(0, await CountAsync("reports"));
      Assert.Equal(0, await CountAsync("ai_proposals"));
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  private static ServiceProvider CreateServiceProvider(
    TestEnvironmentPaths environment,
    Action<IServiceCollection>? configureServices = null)
  {
    Directory.CreateDirectory(environment.WorkingRootPath);

    var services = new ServiceCollection();
    var documentStorageSettings = new DesktopDocumentStorageSettings(
      environment.StorageRootPath,
      true,
      true,
      true,
      true,
      256);
    DesktopCompositionRoot.ConfigureServices(
      services,
      $"Data Source={environment.DatabasePath}",
      CreateSettings(),
      documentStorageSettings);
    configureServices?.Invoke(services);
    return services.BuildServiceProvider();
  }

  private static DesktopWorkflowSettings CreateSettings()
  {
    return new DesktopWorkflowSettings(
      "desktop.bootstrap@local.invalid",
      "Parsing Proof",
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
      Guid.Parse("5fbbdb98-6e5d-48e8-930c-4da04db60336"),
      "report-draft-proposal-default",
      "0.1.0",
      0.2m,
      SPINbuster.AI.DeterministicAiScenario.Success,
      DesktopAiReviewAction.None,
      "No AI review for parsing workflow.",
      "Section 03 30 00 - Cast-in-Place Concrete",
      "03 30 00",
      "Concrete",
      "0",
      "Initial issue.",
      "1",
      "Revised curing requirements.",
      "Request for Information 027",
      "RFI-027",
      "Concrete",
      "0",
      "Clarifies the curing sequence.",
      "RFI-027 clarifies the revised curing requirement.",
      "Section 3.6.B",
      "Provide curing protection immediately after finishing.",
      new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));
  }

  private static void DeleteEnvironmentIfPresent(TestEnvironmentPaths environment)
  {
    SqliteConnection.ClearAllPools();

    if (File.Exists(environment.DatabasePath))
    {
      try { File.Delete(environment.DatabasePath); } catch (IOException) { }
    }

    if (Directory.Exists(environment.WorkingRootPath))
    {
      try { Directory.Delete(environment.WorkingRootPath, recursive: true); } catch (IOException) { }
    }
  }

  private sealed record TestEnvironmentPaths(
    string WorkingRootPath,
    string DatabasePath,
    string StorageRootPath);

  private static TestEnvironmentPaths CreateEnvironmentPaths()
  {
    var workingRootPath = Path.Combine(Path.GetTempPath(), "spinbuster-parsing-tests", Guid.NewGuid().ToString("N"));
    return new TestEnvironmentPaths(
      workingRootPath,
      Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite"),
      Path.Combine(workingRootPath, "immutable-content"));
  }
}
