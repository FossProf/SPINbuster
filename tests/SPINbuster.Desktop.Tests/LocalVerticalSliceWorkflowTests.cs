using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.AI;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Domain;

namespace SPINbuster.Desktop.Tests;

public sealed class LocalVerticalSliceWorkflowTests
{
  [Fact]
  public async Task WorkflowBootstrapperAppliesMigrationsReloadsAiProposalAndRejectsWithoutMutatingReport()
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
      Guid.Parse("5fbbdb98-6e5d-48e8-930c-4da04db60336"),
      "report-draft-proposal-default",
      "0.1.0",
      0.2m,
      DeterministicAiScenario.Success,
      DesktopAiReviewAction.Reject,
      "Deterministic rejection during desktop workflow test.",
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
        Assert.NotNull(result.RequestedAiProposal.ProposalId);
        Assert.True(result.ReplayedAiProposalRequest.IsIdempotentReplay);
        Assert.Equal(result.RequestedAiProposal.ProposalId, result.ReplayedAiProposalRequest.ProposalId);
        Assert.Equal(
          result.PersistedReportSnapshotBeforeReview.RevisionNumber,
          result.PersistedReportSnapshot.RevisionNumber);
        Assert.Equal(
          result.PersistedReportSnapshotBeforeReview.Lifecycle,
          result.PersistedReportSnapshot.Lifecycle);
        Assert.Equal(
          result.PersistedReportSnapshotBeforeReview.Sections.Select(section => (section.Heading, section.Content)),
          result.PersistedReportSnapshot.Sections.Select(section => (section.Heading, section.Content)));
        Assert.Equal(
          result.PersistedReportSnapshotBeforeReview.AuditHistory.Select(entry => entry.AuditEventId),
          result.PersistedReportSnapshot.AuditHistory.Select(entry => entry.AuditEventId));
        Assert.Equal(ModelRunState.Closed, result.ReviewedAiProposalSnapshot.ModelRunState);
        Assert.NotNull(result.ReviewedAiProposalSnapshot.Proposal);
        Assert.Equal(ProposalStatus.Rejected, result.ReviewedAiProposalSnapshot.Proposal!.Status);
        Assert.Equal(settings.ProposalReviewNotes, result.ReviewedAiProposalSnapshot.Proposal.ReviewDispositionNotes);
        Assert.Contains(result.ReviewedAiProposalSnapshot.Proposal.AuditHistory, entry => entry.EventType == "AiProposalRejected");
        Assert.Equal(
          [
            "AiModelRunRequested",
            "AiProviderAttemptRecorded",
            "AiValidationCompleted",
            "AiModelRunCompleted",
          ],
          result.ReviewedAiProposalSnapshot.ModelRunAuditHistory
            .Select(entry => entry.EventType)
            .Where(eventType => eventType != "AiContextManifestCreated"));
        Assert.Single(result.PersistedReportSnapshot.AuditHistory);
        Assert.Equal(1, result.PersistedReportSnapshot.RevisionNumber);
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
      Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
      "report-draft-proposal-default",
      "0.1.0",
      0.2m,
      DeterministicAiScenario.Success,
      DesktopAiReviewAction.HumanAccept,
      "Persist accepted proposal review intent across providers.",
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
        var aiWorkflowQueryHandler = scope.ServiceProvider.GetRequiredService<
          IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult>>();
        var reloadedAiWorkflowSnapshot = await aiWorkflowQueryHandler.HandleAsync(
          new LoadAiProposalWorkflowSnapshotQuery(
            result.RequestedAiProposal.ModelRunId,
            result.RequestedAiProposal.ProposalId));

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
        Assert.Equal(ModelRunState.Closed, reloadedAiWorkflowSnapshot.ModelRunState);
        Assert.NotNull(reloadedAiWorkflowSnapshot.Proposal);
        Assert.Equal(ProposalStatus.HumanAccepted, reloadedAiWorkflowSnapshot.Proposal!.Status);
        Assert.Equal(firstSettings.ProposalReviewNotes, reloadedAiWorkflowSnapshot.Proposal.ReviewDispositionNotes);
        Assert.Equal(
          result.PersistedReportSnapshotBeforeReview.AuditHistory.Select(entry => entry.AuditEventId),
          reloadedReportSnapshot.AuditHistory.Select(entry => entry.AuditEventId));
        Assert.Equal(
          result.PersistedReportSnapshotBeforeReview.Sections.Select(section => (section.Heading, section.Content)),
          reloadedReportSnapshot.Sections.Select(section => (section.Heading, section.Content)));
      }
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowPersistsFailedAiRunAndDoesNotCreateProposalOrMutateReport()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    var connectionString = $"Data Source={databasePath}";
    var settings = new DesktopWorkflowSettings(
      "desktop.bootstrap@local.invalid",
      "Failure Slice",
      "AI Failure Session",
      "Persist this note across failure.",
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
      Guid.Parse("bbbbbbbb-2222-3333-4444-555555555555"),
      Guid.Parse("cccccccc-7777-8888-9999-aaaaaaaaaaaa"),
      "report-draft-proposal-default",
      "0.1.0",
      0.2m,
      DeterministicAiScenario.Timeout,
      DesktopAiReviewAction.None,
      "No review action should occur for failed runs.",
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero));

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString, settings);
      var result = await DesktopWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Null(result.RequestedAiProposal.ProposalId);
      Assert.True(result.ReplayedAiProposalRequest.IsIdempotentReplay);
      Assert.Equal(ModelRunState.Failed, result.ReviewedAiProposalSnapshot.ModelRunState);
      Assert.Equal(ModelRunFailureClassification.Timeout, result.ReviewedAiProposalSnapshot.FailureClassification);
      Assert.Null(result.ReviewedAiProposalSnapshot.Proposal);
      Assert.Single(result.ReviewedAiProposalSnapshot.Attempts);
      Assert.Contains(result.ReviewedAiProposalSnapshot.ModelRunAuditHistory, entry => entry.EventType == "AiModelRunRequested");
      Assert.Contains(result.ReviewedAiProposalSnapshot.ModelRunAuditHistory, entry => entry.EventType == "AiProviderAttemptRecorded");
      Assert.Contains(result.ReviewedAiProposalSnapshot.ModelRunAuditHistory, entry => entry.EventType == "AiValidationCompleted");
      Assert.Equal(ReportLifecycle.Draft, result.PersistedReportSnapshot.Lifecycle);
      Assert.Equal(1, result.PersistedReportSnapshot.RevisionNumber);
      Assert.Single(result.PersistedReportSnapshot.AuditHistory);
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
