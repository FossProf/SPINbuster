using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    var initialTimestampText = configuration["DesktopWorkflow:InitialTimestampUtc"] ?? "2026-07-15T14:00:00Z";

    if (!Guid.TryParse(reportOperationIdText, out var reportOperationId))
    {
      throw new InvalidOperationException(
        $"Desktop workflow report operation ID '{reportOperationIdText}' is not a valid GUID.");
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
      initialTimestampUtc);
  }
}
