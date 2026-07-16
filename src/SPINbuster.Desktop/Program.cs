using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPINbuster.Desktop;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
  options.SingleLine = true;
  options.TimestampFormat = "HH:mm:ss ";
});

var connectionString = builder.Configuration.GetConnectionString("Spinbuster")
  ?? "Data Source=spinbuster.desktop.sqlite";
var settings = DesktopCompositionRoot.LoadSettings(builder.Configuration);

DesktopCompositionRoot.ConfigureServices(builder.Services, connectionString, settings);

using var host = builder.Build();
var result = await DesktopWorkflowBootstrapper.RunAsync(host.Services);

Console.WriteLine($"Project created: {result.CreatedProject.ProjectId} ({result.PersistedInspectionSnapshot.Project.Lifecycle})");
Console.WriteLine($"Inspection session started: {result.StartedInspectionSession.InspectionSessionId} ({result.PersistedInspectionSnapshot.InspectionSession.Lifecycle})");
Console.WriteLine($"Field note captured: {result.CapturedFieldNote.FieldNoteId}");
Console.WriteLine($"Evidence attached: {result.AttachedEvidence.EvidenceAttachmentId}");
Console.WriteLine($"Evidence interpreted: {result.AddedInterpretation.EvidenceAttachmentId}");
Console.WriteLine($"Report draft created: {result.CreatedReportDraft.ReportId} ({result.PersistedReportSnapshot.Lifecycle})");
Console.WriteLine();
Console.WriteLine("Reloaded Project");
Console.WriteLine($"  Id: {result.PersistedInspectionSnapshot.Project.ProjectId}");
Console.WriteLine($"  Name: {result.PersistedInspectionSnapshot.Project.Name}");
Console.WriteLine($"  Lifecycle: {result.PersistedInspectionSnapshot.Project.Lifecycle}");
Console.WriteLine();
Console.WriteLine("Reloaded Inspection Session");
Console.WriteLine($"  Id: {result.PersistedInspectionSnapshot.InspectionSession.InspectionSessionId}");
Console.WriteLine($"  Name: {result.PersistedInspectionSnapshot.InspectionSession.Name}");
Console.WriteLine($"  Lifecycle: {result.PersistedInspectionSnapshot.InspectionSession.Lifecycle}");
Console.WriteLine($"  Field notes: {result.PersistedInspectionSnapshot.InspectionSession.FieldNotes.Count}");

foreach (var fieldNote in result.PersistedInspectionSnapshot.InspectionSession.FieldNotes)
{
  Console.WriteLine($"    {fieldNote.FieldNoteId}: {fieldNote.RawText}");
}

Console.WriteLine();
Console.WriteLine("Reloaded Report Draft");
Console.WriteLine($"  Id: {result.PersistedReportSnapshot.ReportId}");
Console.WriteLine($"  Title: {result.PersistedReportSnapshot.Title}");
Console.WriteLine($"  Revision: {result.PersistedReportSnapshot.RevisionNumber}");
Console.WriteLine($"  Lifecycle: {result.PersistedReportSnapshot.Lifecycle}");
Console.WriteLine("  Sections");
foreach (var section in result.PersistedReportSnapshot.Sections)
{
  Console.WriteLine($"    {section.Heading}: {section.Content}");
}

Console.WriteLine("  Field note sources");
foreach (var fieldNote in result.PersistedReportSnapshot.FieldNotes)
{
  Console.WriteLine($"    {fieldNote.FieldNoteId}: {fieldNote.RawText}");
}

Console.WriteLine("  Evidence sources");
foreach (var evidenceAttachment in result.PersistedReportSnapshot.EvidenceAttachments)
{
  Console.WriteLine(
    $"    {evidenceAttachment.EvidenceAttachmentId}: {evidenceAttachment.FileName} | interpretation={evidenceAttachment.InterpretationSummary}");
}

Console.WriteLine();
Console.WriteLine("Persisted Audit History");
Console.WriteLine("  Project");
foreach (var auditEntry in result.PersistedInspectionSnapshot.Project.AuditHistory)
{
  Console.WriteLine($"    {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor} | {auditEntry.Description}");
}

Console.WriteLine("  InspectionSession");
foreach (var auditEntry in result.PersistedInspectionSnapshot.InspectionSession.AuditHistory)
{
  Console.WriteLine($"    {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor} | {auditEntry.Description}");
}

Console.WriteLine("  Report");
foreach (var auditEntry in result.PersistedReportSnapshot.AuditHistory)
{
  Console.WriteLine($"    {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor} | {auditEntry.Description}");
}
