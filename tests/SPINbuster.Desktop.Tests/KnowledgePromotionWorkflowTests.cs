using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;
using SPINbuster.Application.UseCases.LoadPromotionDiagnostic;
using SPINbuster.Desktop;
using SPINbuster.Domain;
using SPINbuster.Documents;
using SPINbuster.Infrastructure.Persistence;
using System.Globalization;

namespace SPINbuster.Desktop.Tests;

public sealed class KnowledgePromotionWorkflowTests
{
  [Fact]
  public async Task PromotionWorkflowRunsAndProducesPromotedCandidates()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.True(File.Exists(environment.DatabasePath));
      Assert.Equal(PromotionDiagnosticStatus.Promoted, result.FirstPromotion.Status);
      Assert.NotNull(result.FirstPromotion.KnowledgeDocumentId);
      Assert.NotNull(result.FirstPromotion.KnowledgeDocumentRevisionId);
      Assert.NotNull(result.FirstPromotion.KnowledgeCitationId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task PromotionIdempotentReplayReturnsSameDocumentRevisionCitation()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(result.FirstPromotion.Status, result.IdempotentReplay.Status);
      Assert.Equal(result.FirstPromotion.KnowledgeDocumentId, result.IdempotentReplay.KnowledgeDocumentId);
      Assert.Equal(result.FirstPromotion.KnowledgeDocumentRevisionId, result.IdempotentReplay.KnowledgeDocumentRevisionId);
      Assert.Equal(result.FirstPromotion.KnowledgeCitationId, result.IdempotentReplay.KnowledgeCitationId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task SupersedingPromotionCreatesNewRevisionOnSameDocument()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(PromotionDiagnosticStatus.Promoted, result.SupersedingPromotion.Status);
      Assert.Equal(result.FirstPromotion.KnowledgeDocumentId, result.SupersedingPromotion.KnowledgeDocumentId);
      Assert.NotEqual(result.FirstPromotion.KnowledgeDocumentRevisionId, result.SupersedingPromotion.KnowledgeDocumentRevisionId);
      Assert.True(result.SupersedingPromotion.SupersededExistingRevision);
      Assert.Equal(result.FirstPromotion.KnowledgeDocumentRevisionId, result.SupersedingPromotion.SupersededRevisionId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task SupersessionIdempotentReplayReturnsSameResults()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(result.SupersedingPromotion.Status, result.SupersessionIdempotentReplay.Status);
      Assert.Equal(result.SupersedingPromotion.KnowledgeDocumentId, result.SupersessionIdempotentReplay.KnowledgeDocumentId);
      Assert.Equal(result.SupersedingPromotion.KnowledgeDocumentRevisionId, result.SupersessionIdempotentReplay.KnowledgeDocumentRevisionId);
      Assert.Equal(result.SupersedingPromotion.KnowledgeCitationId, result.SupersessionIdempotentReplay.KnowledgeCitationId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task KnowledgeSnapshotContainsDocumentWithTwoRevisionsAndDerivedFromRelationship()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Single(result.KnowledgeSnapshot.Documents);
      var document = result.KnowledgeSnapshot.Documents[0];
      Assert.Equal(KnowledgeDocumentType.Specification, document.DocumentType);
      Assert.Equal(2, document.Revisions.Count);
      Assert.Contains(document.Revisions, r => r.Lifecycle == KnowledgeRevisionLifecycle.Superseded);
      Assert.Contains(document.Revisions, r => r.Lifecycle == KnowledgeRevisionLifecycle.CurrentAuthoritative);

      var citations = document.Revisions.SelectMany(r => r.Citations).ToArray();
      Assert.Equal(2, citations.Length);

      var derivedFrom = result.KnowledgeSnapshot.Relationships
        .Where(r => r.RelationshipType == KnowledgeRelationshipType.DerivedFrom)
        .ToArray();
      Assert.Equal(2, derivedFrom.Length);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task AuthorityIsolationNoAiDecisionsInPromotion()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.True(result.PromotionDiagnostics.Count >= 4);
      Assert.All(result.PromotionDiagnostics, d => Assert.Equal(PromotionDiagnosticStatus.Promoted, d.Status));
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task PromotionDiagnosticsSurviveDisposeAndRecreateProvider()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      KnowledgePromotionWorkflowResult firstResult;
      using (var firstProvider = CreateServiceProvider(environment))
      {
        firstResult = await KnowledgePromotionWorkflowBootstrapper.RunAsync(firstProvider);
      }

      using var secondProvider = CreateServiceProvider(environment);
      await KnowledgePromotionWorkflowBootstrapper.MigrateAsync(secondProvider);
      await using var scope = secondProvider.CreateAsyncScope();
      var loadDiagnostic = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadPromotionDiagnosticQuery, LoadPromotionDiagnosticResult>>();

      var reloadedFirst = await loadDiagnostic.HandleAsync(
        new LoadPromotionDiagnosticQuery(firstResult.FirstPromotion.PromotionDiagnosticId));

      var reloadedSupersession = await loadDiagnostic.HandleAsync(
        new LoadPromotionDiagnosticQuery(firstResult.SupersedingPromotion.PromotionDiagnosticId));

      Assert.Equal(PromotionDiagnosticStatus.Promoted, reloadedFirst.Status);
      Assert.Equal(firstResult.FirstPromotion.KnowledgeDocumentId, reloadedFirst.KnowledgeDocumentId);
      Assert.Equal(PromotionDiagnosticStatus.Promoted, reloadedSupersession.Status);
      Assert.True(reloadedSupersession.SupersededExistingRevision);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task KnowledgeSnapshotSurvivesDisposeAndRecreateProvider()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      KnowledgePromotionWorkflowResult firstResult;
      using (var firstProvider = CreateServiceProvider(environment))
      {
        firstResult = await KnowledgePromotionWorkflowBootstrapper.RunAsync(firstProvider);
      }

      using var secondProvider = CreateServiceProvider(environment);
      await KnowledgePromotionWorkflowBootstrapper.MigrateAsync(secondProvider);
      await using var scope = secondProvider.CreateAsyncScope();
      var loadKnowledge = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult>>();

      var reloadedSnapshot = await loadKnowledge.HandleAsync(
        new LoadProjectKnowledgeSnapshotQuery(firstResult.CreatedProject.ProjectId));

      Assert.Single(reloadedSnapshot.Documents);
      Assert.Equal(2, reloadedSnapshot.Documents[0].Revisions.Count);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ParsingWorkflowProducesCandidatesBeforePromotion()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

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
  public async Task FragmentReviewSnapshotReflectsAcceptedAndRejectedCandidates()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Contains(result.ReviewSnapshotAfterAccept.Entries,
        e => e.FragmentCandidateId == result.AcceptedCandidateA.FragmentCandidateId
          && e.ReviewState == FragmentCandidateReviewState.HumanAccepted);
      Assert.Contains(result.ReviewSnapshotAfterReject.Entries,
        e => e.FragmentCandidateId == result.RejectedCandidateA.FragmentCandidateId
          && e.ReviewState == FragmentCandidateReviewState.Rejected);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task WorkflowProducesExpectedFailurePresentations()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.True(result.FailurePresentations.Count >= 2, $"Expected at least 2 failure presentations, got {result.FailurePresentations.Count}");
      Assert.Contains(result.FailurePresentations, f => f.Scenario == "promote-rejected-candidate");
      Assert.Contains(result.FailurePresentations, f => f.Scenario == "promote-missing-candidate");
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
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);
      var output = KnowledgePromotionWorkflowConsoleFormatter.Format(result);

      Assert.Contains("Knowledge Promotion Vertical Slice", output, StringComparison.Ordinal);
      Assert.Contains("Promotion (Idempotent)", output, StringComparison.Ordinal);
      Assert.Contains("Supersession", output, StringComparison.Ordinal);
      Assert.Contains("Provenance", output, StringComparison.Ordinal);
      Assert.Contains("Authority Isolation", output, StringComparison.Ordinal);
      Assert.Contains("Promoted", output, StringComparison.Ordinal);
      Assert.Contains("DerivedFrom", output, StringComparison.Ordinal);
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
  public async Task PromotionWorkflowRunsTwiceAgainstSameDatabasePreservingPriorData()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      KnowledgePromotionWorkflowResult firstRun;
      using (var firstProvider = CreateServiceProvider(environment))
      {
        firstRun = await KnowledgePromotionWorkflowBootstrapper.RunAsync(firstProvider);
      }

      using var secondProvider = CreateServiceProvider(environment);
      var secondResult = await KnowledgePromotionWorkflowBootstrapper.RunAsync(secondProvider);

      Assert.NotEqual(firstRun.CreatedProject.ProjectId, secondResult.CreatedProject.ProjectId);
      Assert.NotEqual(firstRun.FirstPromotion.KnowledgeDocumentId, secondResult.FirstPromotion.KnowledgeDocumentId);

      await using var scope = secondProvider.CreateAsyncScope();
      var loadKnowledge = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult>>();
      var firstProjectSnapshot = await loadKnowledge.HandleAsync(
        new LoadProjectKnowledgeSnapshotQuery(firstRun.CreatedProject.ProjectId));

      Assert.Single(firstProjectSnapshot.Documents);
      Assert.Equal(2, firstProjectSnapshot.Documents[0].Revisions.Count);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task PromotionWorkflowDoesNotMutateReportOrAiRecords()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      await using var scope = serviceProvider.CreateAsyncScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<SpinbusterDbContext>();
      await dbContext.Database.OpenConnectionAsync();

      async Task<long> CountAsync(string table)
      {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
      }

      Assert.True(await CountAsync("knowledge_documents") >= 1);
      Assert.True(await CountAsync("knowledge_document_revisions") >= 2);
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
      "Promotion Proof",
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
      "No AI review for promotion workflow.",
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
    var workingRootPath = Path.Combine(Path.GetTempPath(), "spinbuster-promotion-tests", Guid.NewGuid().ToString("N"));
    return new TestEnvironmentPaths(
      workingRootPath,
      Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite"),
      Path.Combine(workingRootPath, "immutable-content"));
  }
}
