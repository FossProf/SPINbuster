using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.AI;
using SPINbuster.Application;
using SPINbuster.Application.Abstractions;
using SPINbuster.Infrastructure;

namespace SPINbuster.Desktop;

public static class DesktopCompositionRoot
{
  public static void ConfigureServices(
    IServiceCollection services,
    string connectionString,
    DesktopWorkflowSettings settings)
  {
    services.AddSingleton(settings);
    services.AddSpinbusterApplication();
    services.AddSpinbusterDeterministicAi(new DeterministicAiProviderOptions
    {
      Scenario = settings.AiScenario,
    });
    services.AddSpinbusterSqliteInfrastructure(connectionString);
    services.AddScoped<IClock>(_ => new DeterministicClock(settings.InitialTimestampUtc));
    services.AddScoped<ICurrentUser>(_ => new FixedCurrentUser(settings.CurrentUserId));
    services.AddScoped<LocalVerticalSliceWorkflowRunner>();
  }

  public static DesktopWorkflowSettings LoadSettings(IConfiguration configuration)
  {
    var currentUserId = configuration["DesktopWorkflow:CurrentUserId"] ?? "desktop.bootstrap@local.invalid";
    var projectName = configuration["DesktopWorkflow:ProjectName"] ?? "Local Vertical Slice";
    var sessionName = configuration["DesktopWorkflow:SessionName"] ?? "Initial Inspection Session";
    var fieldNoteText = configuration["DesktopWorkflow:FieldNoteText"] ?? "Observed deterministic bootstrap workflow note.";
    var evidenceFileName = configuration["DesktopWorkflow:EvidenceFileName"] ?? "photo-01.jpg";
    var evidenceMediaType = configuration["DesktopWorkflow:EvidenceMediaType"] ?? "image/jpeg";
    var evidenceStorageKey = configuration["DesktopWorkflow:EvidenceStorageKey"] ?? "evidence/photo-01.jpg";
    var evidenceChecksum = configuration["DesktopWorkflow:EvidenceChecksum"] ?? "sha256:deterministic";
    var interpretationSummary = configuration["DesktopWorkflow:InterpretationSummary"] ?? "Deterministic interpretation summary.";
    var draftTitle = configuration["DesktopWorkflow:DraftTitle"] ?? "Initial Draft Report";
    var draftSummaryHeading = configuration["DesktopWorkflow:DraftSummaryHeading"] ?? "Summary";
    var draftSummaryContent = configuration["DesktopWorkflow:DraftSummaryContent"] ?? "Deterministic report summary.";
    var draftObservationHeading = configuration["DesktopWorkflow:DraftObservationHeading"] ?? "Observations";
    var draftObservationContent = configuration["DesktopWorkflow:DraftObservationContent"] ?? "Deterministic report observations.";
    var reportOperationIdText = configuration["DesktopWorkflow:ReportOperationId"] ?? "0f74d133-75a0-4cf3-9d80-1f66144d96ac";
    var proposalOperationIdText = configuration["DesktopWorkflow:ProposalOperationId"] ?? "5fbbdb98-6e5d-48e8-930c-4da04db60336";
    var proposalPromptPackageId = configuration["DesktopWorkflow:ProposalPromptPackageId"] ?? "report-draft-proposal-default";
    var proposalPromptPackageVersion = configuration["DesktopWorkflow:ProposalPromptPackageVersion"] ?? "0.1.0";
    var proposalTemperatureText = configuration["DesktopWorkflow:ProposalTemperature"] ?? "0.2";
    var aiScenarioText = configuration["DesktopWorkflow:AiScenario"] ?? nameof(DeterministicAiScenario.Success);
    var proposalReviewActionText = configuration["DesktopWorkflow:ProposalReviewAction"] ?? nameof(DesktopAiReviewAction.Reject);
    var proposalReviewNotes = configuration["DesktopWorkflow:ProposalReviewNotes"] ?? "Deterministic desktop review disposition.";
    var initialTimestampText = configuration["DesktopWorkflow:InitialTimestampUtc"] ?? "2026-07-15T14:00:00Z";

    if (!Guid.TryParse(reportOperationIdText, out var reportOperationId))
    {
      throw new InvalidOperationException(
        $"Desktop workflow report operation ID '{reportOperationIdText}' is not a valid GUID.");
    }

    if (!Guid.TryParse(proposalOperationIdText, out var proposalOperationId))
    {
      throw new InvalidOperationException(
        $"Desktop workflow proposal operation ID '{proposalOperationIdText}' is not a valid GUID.");
    }

    if (!decimal.TryParse(proposalTemperatureText, out var proposalTemperature))
    {
      throw new InvalidOperationException(
        $"Desktop workflow proposal temperature '{proposalTemperatureText}' is not a valid decimal value.");
    }

    if (!Enum.TryParse<DeterministicAiScenario>(aiScenarioText, true, out var aiScenario))
    {
      throw new InvalidOperationException(
        $"Desktop workflow AI scenario '{aiScenarioText}' is not a valid deterministic scenario.");
    }

    if (!Enum.TryParse<DesktopAiReviewAction>(proposalReviewActionText, true, out var proposalReviewAction))
    {
      throw new InvalidOperationException(
        $"Desktop workflow review action '{proposalReviewActionText}' is not a valid AI review action.");
    }

    if (!DateTimeOffset.TryParse(initialTimestampText, out var initialTimestampUtc))
    {
      throw new InvalidOperationException(
        $"Desktop workflow timestamp '{initialTimestampText}' is not a valid UTC timestamp.");
    }

    return new DesktopWorkflowSettings(
      currentUserId,
      projectName,
      sessionName,
      fieldNoteText,
      evidenceFileName,
      evidenceMediaType,
      evidenceStorageKey,
      evidenceChecksum,
      interpretationSummary,
      draftTitle,
      draftSummaryHeading,
      draftSummaryContent,
      draftObservationHeading,
      draftObservationContent,
      reportOperationId,
      proposalOperationId,
      proposalPromptPackageId,
      proposalPromptPackageVersion,
      proposalTemperature,
      aiScenario,
      proposalReviewAction,
      proposalReviewNotes,
      initialTimestampUtc);
  }
}
