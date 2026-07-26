using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;
using SPINbuster.Application.UseCases.LoadPromotionDiagnostic;
using SPINbuster.Application.UseCases.LoadPromotionProvenance;
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

      Assert.Equal(PromotionDiagnosticStatus.Failed, result.SupersedingPromotion.Status);
      Assert.Equal(PromotionConflictType.HigherAuthorityExists, result.SupersedingPromotion.ConflictType);
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
      Assert.Single(document.Revisions);
      Assert.Contains(document.Revisions, r => r.Lifecycle == KnowledgeRevisionLifecycle.CurrentAuthoritative);

      var citations = document.Revisions.SelectMany(r => r.Citations).ToArray();
      Assert.Single(citations);

      var derivedFrom = result.KnowledgeSnapshot.Relationships
        .Where(r => r.RelationshipType == KnowledgeRelationshipType.DerivedFrom)
        .ToArray();
      Assert.Single(derivedFrom);
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
      Assert.Contains(result.PromotionDiagnostics, d => d.Status == PromotionDiagnosticStatus.Promoted);
      Assert.Contains(result.PromotionDiagnostics, d => d.Status == PromotionDiagnosticStatus.Failed);
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
      Assert.Equal(PromotionDiagnosticStatus.Failed, reloadedSupersession.Status);
      Assert.False(reloadedSupersession.SupersededExistingRevision);
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
      Assert.Single(reloadedSnapshot.Documents[0].Revisions);
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
      Assert.Single(firstProjectSnapshot.Documents[0].Revisions);
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
      Assert.True(await CountAsync("knowledge_document_revisions") >= 1);
      Assert.Equal(0, await CountAsync("reports"));
      Assert.Equal(0, await CountAsync("ai_proposals"));
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task PromotionProvenanceSurviveDisposeAndRecreateProvider()
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
      var loadProvenance = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadPromotionProvenanceQuery, LoadPromotionProvenanceResult>>();

      var reloaded = await loadProvenance.HandleAsync(
        new LoadPromotionProvenanceQuery { RevisionId = firstResult.FirstPromotion.KnowledgeDocumentRevisionId });

      Assert.NotNull(reloaded);
      Assert.Equal(firstResult.FirstPromotion.KnowledgeDocumentRevisionId, reloaded.PromotedRevisionId);
      Assert.Equal(firstResult.AcceptedCandidateA.FragmentCandidateId, reloaded.FragmentCandidateId);
      Assert.False(string.IsNullOrWhiteSpace(reloaded.ParserKey));
      Assert.False(string.IsNullOrWhiteSpace(reloaded.PromotionIdentityHash));
      Assert.Equal(firstResult.FirstPromotion.PromotionDiagnosticId, reloaded.DiagnosticId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ProvenanceUnchangedAfterIdempotentReplay()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      await using var scope = serviceProvider.CreateAsyncScope();
      var loadProvenance = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadPromotionProvenanceQuery, LoadPromotionProvenanceResult>>();

      var firstProvenance = await loadProvenance.HandleAsync(
        new LoadPromotionProvenanceQuery { RevisionId = result.FirstPromotion.KnowledgeDocumentRevisionId });

      var replayProvenance = await loadProvenance.HandleAsync(
        new LoadPromotionProvenanceQuery { RevisionId = result.IdempotentReplay.KnowledgeDocumentRevisionId });

      Assert.Equal(firstProvenance.PromotedRevisionId, replayProvenance.PromotedRevisionId);
      Assert.Equal(firstProvenance.FragmentCandidateId, replayProvenance.FragmentCandidateId);
      Assert.Equal(firstProvenance.ParserRunId, replayProvenance.ParserRunId);
      Assert.Equal(firstProvenance.ImportedSourceId, replayProvenance.ImportedSourceId);
      Assert.Equal(firstProvenance.PromotionIdentityHash, replayProvenance.PromotionIdentityHash);
      Assert.Equal(firstProvenance.PromotionAttemptId, replayProvenance.PromotionAttemptId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ConsoleFormatterProvenanceChainDisplaysIdsAndBoundedMetadataOnly()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);
      var output = KnowledgePromotionWorkflowConsoleFormatter.Format(result);

      Assert.Contains(result.FirstPromotion.KnowledgeDocumentRevisionId!.ToString()!, output, StringComparison.Ordinal);
      Assert.Contains(result.FirstPromotion.KnowledgeCitationId!.ToString()!, output, StringComparison.Ordinal);
      Assert.Contains(result.FirstPromotion.KnowledgeDocumentId!.ToString()!, output, StringComparison.Ordinal);
      Assert.Contains("Provenance", output, StringComparison.Ordinal);
      Assert.Contains("Promoted", output, StringComparison.Ordinal);
      Assert.Contains("CurrentAuthoritative", output, StringComparison.Ordinal);
      Assert.Contains("Superseded", output, StringComparison.Ordinal);
      Assert.Contains("Specification", output, StringComparison.Ordinal);
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
  public async Task ProvenanceChainIncludesAllRequiredNodes()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      await using var scope = serviceProvider.CreateAsyncScope();
      var loadProvenance = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadPromotionProvenanceQuery, LoadPromotionProvenanceResult>>();

      var provenance = await loadProvenance.HandleAsync(
        new LoadPromotionProvenanceQuery { RevisionId = result.FirstPromotion.KnowledgeDocumentRevisionId });

      Assert.NotEqual(default, provenance.Id);
      Assert.Equal(result.AcceptedCandidateA.FragmentCandidateId, provenance.FragmentCandidateId);
      Assert.False(string.IsNullOrWhiteSpace(provenance.ParserKey));
      Assert.NotEqual(default, provenance.ImportedSourceId);
      Assert.Equal(FragmentCandidateReviewState.HumanAccepted, provenance.ReviewState);
      Assert.False(string.IsNullOrWhiteSpace(provenance.PromotionIdentityHash));
      Assert.NotEqual(default, provenance.PromotionAttemptId);
      Assert.Equal(result.FirstPromotion.KnowledgeDocumentRevisionId, provenance.PromotedRevisionId);
      Assert.Equal(result.FirstPromotion.PromotionDiagnosticId, provenance.DiagnosticId);
      Assert.False(string.IsNullOrWhiteSpace(provenance.ParserVersion));
      Assert.False(string.IsNullOrWhiteSpace(provenance.ParserContractVersion));
      Assert.False(string.IsNullOrWhiteSpace(provenance.ImportedSourceContentHash));

      var output = KnowledgePromotionWorkflowConsoleFormatter.Format(result);
      Assert.Contains("Provenance", output, StringComparison.Ordinal);
      Assert.Contains("Accepted Candidate:", output, StringComparison.Ordinal);
      Assert.Contains("HumanAccepted", output, StringComparison.Ordinal);
      Assert.Contains("Promoted", output, StringComparison.Ordinal);
      Assert.Contains("DerivedFrom", output, StringComparison.Ordinal);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task ProvenanceMetadataDoesNotExposeRawContent()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);
      var output = KnowledgePromotionWorkflowConsoleFormatter.Format(result);

      Assert.DoesNotContain("Clarifies the curing sequence", output, StringComparison.Ordinal);
      Assert.DoesNotContain("Provide curing protection", output, StringComparison.Ordinal);
      Assert.DoesNotContain("RFI-027 clarifies", output, StringComparison.Ordinal);
      Assert.DoesNotContain("Revised curing requirements", output, StringComparison.Ordinal);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task FirstPromotionConflictTypeIsNone()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(PromotionConflictType.None, result.FirstPromotion.ConflictType);
      Assert.Equal(PromotionDiagnosticStatus.Promoted, result.FirstPromotion.Status);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task SupersedingPromotionConflictTypeIsNone()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(PromotionConflictType.HigherAuthorityExists, result.SupersedingPromotion.ConflictType);
      Assert.Equal(PromotionDiagnosticStatus.Failed, result.SupersedingPromotion.Status);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task IdempotentReplayConflictTypeIsNone()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(PromotionConflictType.None, result.IdempotentReplay.ConflictType);
      Assert.Equal(PromotionDiagnosticStatus.Promoted, result.IdempotentReplay.Status);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task RecoverableFailedPromotionFailsBeforeActivation()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(PromotionDiagnosticStatus.Failed, result.RecoverableFailedPromotion.Status);
      Assert.Null(result.RecoverableFailedPromotion.KnowledgeDocumentId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task RecoverableRetryPromotionSucceedsAfterActivation()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(PromotionDiagnosticStatus.Promoted, result.RecoverableRetryPromotion.Status);
      Assert.NotNull(result.RecoverableRetryPromotion.KnowledgeDocumentId);
      Assert.NotNull(result.RecoverableRetryPromotion.KnowledgeDocumentRevisionId);
      Assert.NotNull(result.RecoverableRetryPromotion.KnowledgeCitationId);
    }
    finally
    {
      DeleteEnvironmentIfPresent(environment);
    }
  }

  [Fact]
  public async Task RecoverableRetryProducesDifferentDiagnosticThanFailure()
  {
    var environment = CreateEnvironmentPaths();

    try
    {
      using var serviceProvider = CreateServiceProvider(environment);
      var result = await KnowledgePromotionWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.NotEqual(
        result.RecoverableFailedPromotion.PromotionDiagnosticId,
        result.RecoverableRetryPromotion.PromotionDiagnosticId);
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
