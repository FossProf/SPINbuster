using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SPINbuster.AI;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;
using SPINbuster.Domain;

namespace SPINbuster.Desktop.Tests;

public sealed class LocalVerticalSliceWorkflowTests
{
  [Fact]
  public async Task WorkflowBootstrapperAppliesMigrationsBuildsKnowledgeSnapshotAndPreservesAuthoritativeState()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString, CreateSettings());
      var result = await DesktopWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.True(File.Exists(databasePath));
      Assert.Equal(ProjectLifecycle.Active, result.PersistedInspectionSnapshot.Project.Lifecycle);
      Assert.Equal(InspectionSessionLifecycle.InProgress, result.PersistedInspectionSnapshot.InspectionSession.Lifecycle);
      Assert.Single(result.PersistedInspectionSnapshot.InspectionSession.FieldNotes);
      Assert.Equal("Section 03 30 00 - Cast-in-Place Concrete", result.ReloadedKnowledgeSnapshot.Documents.Single(document => document.DocumentType == KnowledgeDocumentType.Specification).CanonicalTitle);

      var specification = result.ReloadedKnowledgeSnapshot.Documents.Single(document => document.KnowledgeDocumentId == result.RegisteredSpecificationDocument.KnowledgeDocumentId);
      Assert.Equal(result.SupersededSpecificationRevision.CurrentAuthoritativeRevisionId, specification.CurrentAuthoritativeRevisionId);
      Assert.Equal(2, specification.Revisions.Count);
      Assert.Contains(specification.Revisions, revision => revision.KnowledgeDocumentRevisionId == result.AddedSpecificationInitialRevision.KnowledgeDocumentRevisionId && revision.Lifecycle == KnowledgeRevisionLifecycle.Superseded);
      Assert.Contains(specification.Revisions, revision => revision.KnowledgeDocumentRevisionId == result.SupersededSpecificationRevision.SuccessorRevisionId && revision.Lifecycle == KnowledgeRevisionLifecycle.CurrentAuthoritative);
      Assert.Single(specification.Revisions.Single(revision => revision.KnowledgeDocumentRevisionId == result.SupersededSpecificationRevision.SuccessorRevisionId).Citations);

      var relationship = Assert.Single(result.ReloadedKnowledgeSnapshot.Relationships);
      Assert.Equal(KnowledgeRelationshipType.Clarifies, relationship.RelationshipType);
      Assert.Equal($"Revision:{result.AddedRfiInitialRevision.KnowledgeDocumentRevisionId}", relationship.Source.StableKey);
      Assert.Equal($"Revision:{result.SupersededSpecificationRevision.SuccessorRevisionId}", relationship.Target.StableKey);

      AssertReportUnchangedAcrossKnowledgeWorkflow(result);
      AssertAiWorkflowUnchangedAcrossKnowledgeWorkflow(result);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowDataCanBeReloadedFromAFreshServiceProviderAndSnapshotQueriesRemainSideEffectFree()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";
    var settings = CreateSettings() with
    {
      ProposalReviewAction = DesktopAiReviewAction.HumanAccept,
      ProposalReviewNotes = "Persist accepted proposal review intent across providers.",
    };

    try
    {
      LocalVerticalSliceWorkflowResult initialResult;
      using (var firstProvider = CreateServiceProvider(connectionString, settings))
      {
        initialResult = await DesktopWorkflowBootstrapper.RunAsync(firstProvider);
      }

      using (var secondProvider = CreateServiceProvider(connectionString, settings with
      {
        InitialTimestampUtc = settings.InitialTimestampUtc.AddHours(6),
      }))
      {
        await DesktopWorkflowBootstrapper.MigrateAsync(secondProvider);

        await using var scope = secondProvider.CreateAsyncScope();
        var inspectionQuery = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult>>();
        var aiQuery = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult>>();
        var knowledgeQuery = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult>>();

        var reloadedInspection = await inspectionQuery.HandleAsync(new LoadInspectionWorkflowSnapshotQuery(
          initialResult.CreatedProject.ProjectId,
          initialResult.StartedInspectionSession.InspectionSessionId));
        var reloadedAi = await aiQuery.HandleAsync(new LoadAiProposalWorkflowSnapshotQuery(
          initialResult.RequestedAiProposal.ModelRunId,
          initialResult.RequestedAiProposal.ProposalId));
        var reloadedKnowledge = await knowledgeQuery.HandleAsync(new LoadProjectKnowledgeSnapshotQuery(initialResult.CreatedProject.ProjectId));

        Assert.Equal(initialResult.PersistedInspectionSnapshot.Project.ProjectId, reloadedInspection.Project.ProjectId);
        Assert.Equal(initialResult.PersistedInspectionSnapshot.InspectionSession.InspectionSessionId, reloadedInspection.InspectionSession.InspectionSessionId);
        Assert.Equal(initialResult.ReviewedAiProposalSnapshot.Proposal?.Status, reloadedAi.Proposal?.Status);
        AssertKnowledgeSnapshotsEquivalent(initialResult.ReloadedKnowledgeSnapshot, initialResult.ReplayedKnowledgeSnapshot);
        AssertKnowledgeSnapshotsEquivalent(initialResult.ReloadedKnowledgeSnapshot, reloadedKnowledge);
      }
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowPersistsFailedAiRunAndStillPresentsKnowledgeExecutableSlice()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";
    var settings = CreateSettings() with
    {
      ProjectName = "Failure Slice",
      SessionName = "AI Failure Session",
      AiScenario = DeterministicAiScenario.Timeout,
      ProposalReviewAction = DesktopAiReviewAction.None,
      ProposalReviewNotes = "No review action should occur for failed runs.",
    };

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString, settings);
      var result = await DesktopWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Null(result.RequestedAiProposal.ProposalId);
      Assert.True(result.ReplayedAiProposalRequest.IsIdempotentReplay);
      Assert.Equal(ModelRunState.Failed, result.ReviewedAiProposalSnapshot.ModelRunState);
      Assert.Equal(ModelRunFailureClassification.Timeout, result.ReviewedAiProposalSnapshot.FailureClassification);
      Assert.NotEmpty(result.ReloadedKnowledgeSnapshot.Documents);
      Assert.Single(result.ReloadedKnowledgeSnapshot.Relationships);
      Assert.Equal(ReportLifecycle.Draft, result.PersistedReportSnapshot.Lifecycle);
      Assert.Single(result.PersistedReportSnapshot.AuditHistory);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowCapturesExpectedFailurePresentationsWithoutCrashing()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString, CreateSettings());
      var result = await DesktopWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(
        [
          "Duplicate revision",
          "Cross-project relationship",
          "Missing revision citation",
          "Invalid supersession",
          "Duplicate relationship",
          "Invalid citation locator",
        ],
        result.FailurePresentations.Select(item => item.Scenario));
      Assert.All(result.FailurePresentations, failure => Assert.False(string.IsNullOrWhiteSpace(failure.Message)));
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowAuditHistoryReloadsInStableOrder()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString, CreateSettings());
      var result = await DesktopWorkflowBootstrapper.RunAsync(serviceProvider);

      var specification = result.ReloadedKnowledgeSnapshot.Documents.Single(document => document.DocumentType == KnowledgeDocumentType.Specification);
      Assert.True(IsOrdered(specification.AuditHistory.Select(entry => (entry.OccurredAtUtc, entry.AuditEventId.Value))));
      Assert.All(specification.Revisions, revision => Assert.True(IsOrdered(revision.AuditHistory.Select(entry => (entry.OccurredAtUtc, entry.AuditEventId.Value)))));
      Assert.All(result.ReloadedKnowledgeSnapshot.Relationships, relationship => Assert.True(IsOrdered(relationship.AuditHistory.Select(entry => (entry.OccurredAtUtc, entry.AuditEventId.Value)))));
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task CommitFailureIsSurfacedAndDoesNotReportWorkflowSuccess()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString, CreateSettings(), services =>
      {
        services.RemoveAll<IUnitOfWork>();
        services.AddScoped<IUnitOfWork, ThrowingUnitOfWork>();
      });

      var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DesktopWorkflowBootstrapper.RunAsync(serviceProvider));
      Assert.Equal("Forced commit failure.", exception.Message);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  private static DesktopWorkflowSettings CreateSettings()
  {
    return new DesktopWorkflowSettings(
      "desktop.bootstrap@local.invalid",
      "Knowledge Engine Executable Slice",
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
      DeterministicAiScenario.Success,
      DesktopAiReviewAction.Reject,
      "Deterministic rejection during desktop workflow test.",
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
      new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
  }

  private static ServiceProvider CreateServiceProvider(
    string connectionString,
    DesktopWorkflowSettings settings,
    Action<IServiceCollection>? configureServices = null)
  {
    var services = new ServiceCollection();
    DesktopCompositionRoot.ConfigureServices(services, connectionString, settings);
    configureServices?.Invoke(services);
    return services.BuildServiceProvider();
  }

  private static void AssertKnowledgeSnapshotsEquivalent(
    LoadProjectKnowledgeSnapshotResult expected,
    LoadProjectKnowledgeSnapshotResult actual)
  {
    Assert.Equal(expected.ProjectId, actual.ProjectId);
    Assert.Equal(expected.Documents.Select(document => document.KnowledgeDocumentId), actual.Documents.Select(document => document.KnowledgeDocumentId));
    Assert.Equal(expected.Relationships.Select(relationship => relationship.KnowledgeRelationshipId), actual.Relationships.Select(relationship => relationship.KnowledgeRelationshipId));
    Assert.Equal(
      expected.Documents.SelectMany(document => document.Revisions).Select(revision => revision.KnowledgeDocumentRevisionId),
      actual.Documents.SelectMany(document => document.Revisions).Select(revision => revision.KnowledgeDocumentRevisionId));
    Assert.Equal(
      expected.Documents.SelectMany(document => document.Revisions).SelectMany(revision => revision.Citations).Select(citation => citation.KnowledgeCitationId),
      actual.Documents.SelectMany(document => document.Revisions).SelectMany(revision => revision.Citations).Select(citation => citation.KnowledgeCitationId));
  }

  private static void AssertReportUnchangedAcrossKnowledgeWorkflow(LocalVerticalSliceWorkflowResult result)
  {
    Assert.Equal(result.PersistedReportSnapshotBeforeKnowledge.RevisionNumber, result.PersistedReportSnapshot.RevisionNumber);
    Assert.Equal(result.PersistedReportSnapshotBeforeKnowledge.Lifecycle, result.PersistedReportSnapshot.Lifecycle);
    Assert.Equal(
      result.PersistedReportSnapshotBeforeKnowledge.Sections.Select(section => (section.Heading, section.Content)),
      result.PersistedReportSnapshot.Sections.Select(section => (section.Heading, section.Content)));
    Assert.Equal(
      result.PersistedReportSnapshotBeforeKnowledge.AuditHistory.Select(entry => entry.AuditEventId),
      result.PersistedReportSnapshot.AuditHistory.Select(entry => entry.AuditEventId));
  }

  private static void AssertAiWorkflowUnchangedAcrossKnowledgeWorkflow(LocalVerticalSliceWorkflowResult result)
  {
    Assert.Equal(result.ReviewedAiProposalSnapshotBeforeKnowledge.ModelRunState, result.ReviewedAiProposalSnapshot.ModelRunState);
    Assert.Equal(result.ReviewedAiProposalSnapshotBeforeKnowledge.FailureClassification, result.ReviewedAiProposalSnapshot.FailureClassification);
    Assert.Equal(result.ReviewedAiProposalSnapshotBeforeKnowledge.ModelRunAuditHistory.Select(entry => entry.AuditEventId), result.ReviewedAiProposalSnapshot.ModelRunAuditHistory.Select(entry => entry.AuditEventId));
    Assert.Equal(result.ReviewedAiProposalSnapshotBeforeKnowledge.Proposal?.ProposalId, result.ReviewedAiProposalSnapshot.Proposal?.ProposalId);
    Assert.Equal(result.ReviewedAiProposalSnapshotBeforeKnowledge.Proposal?.Status, result.ReviewedAiProposalSnapshot.Proposal?.Status);
    Assert.Equal(
      result.ReviewedAiProposalSnapshotBeforeKnowledge.Proposal?.AuditHistory.Select(entry => entry.AuditEventId) ?? [],
      result.ReviewedAiProposalSnapshot.Proposal?.AuditHistory.Select(entry => entry.AuditEventId) ?? []);
  }

  private static bool IsOrdered(IEnumerable<(DateTimeOffset OccurredAtUtc, Guid AuditEventId)> entries)
  {
    (DateTimeOffset OccurredAtUtc, Guid AuditEventId)? previous = null;
    foreach (var entry in entries)
    {
      if (previous is not null && (entry.OccurredAtUtc < previous.Value.OccurredAtUtc
        || entry.OccurredAtUtc == previous.Value.OccurredAtUtc && entry.AuditEventId.CompareTo(previous.Value.AuditEventId) < 0))
      {
        return false;
      }

      previous = entry;
    }

    return true;
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
          break;
        }
      }
    }
  }

  private sealed class ThrowingUnitOfWork : IUnitOfWork
  {
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
      throw new InvalidOperationException("Forced commit failure.");
    }
  }
}
