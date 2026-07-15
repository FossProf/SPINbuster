using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SPINbuster.Desktop;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Spinbuster")
  ?? "Data Source=spinbuster.desktop.sqlite";
var settings = DesktopCompositionRoot.LoadSettings(builder.Configuration);

DesktopCompositionRoot.ConfigureServices(builder.Services, connectionString, settings);

using var host = builder.Build();
var result = await DesktopWorkflowBootstrapper.RunAsync(host.Services);

Console.WriteLine($"Project created: {result.CreatedProject.ProjectId} ({result.PersistedSnapshot.Project.Lifecycle})");
Console.WriteLine($"Inspection session started: {result.StartedInspectionSession.InspectionSessionId} ({result.PersistedSnapshot.InspectionSession.Lifecycle})");
Console.WriteLine($"Field note captured: {result.CapturedFieldNote.FieldNoteId}");
Console.WriteLine();
Console.WriteLine("Reloaded Project");
Console.WriteLine($"  Id: {result.PersistedSnapshot.Project.ProjectId}");
Console.WriteLine($"  Name: {result.PersistedSnapshot.Project.Name}");
Console.WriteLine($"  Lifecycle: {result.PersistedSnapshot.Project.Lifecycle}");
Console.WriteLine();
Console.WriteLine("Reloaded Inspection Session");
Console.WriteLine($"  Id: {result.PersistedSnapshot.InspectionSession.InspectionSessionId}");
Console.WriteLine($"  Name: {result.PersistedSnapshot.InspectionSession.Name}");
Console.WriteLine($"  Lifecycle: {result.PersistedSnapshot.InspectionSession.Lifecycle}");
Console.WriteLine($"  Field notes: {result.PersistedSnapshot.InspectionSession.FieldNotes.Count}");

foreach (var fieldNote in result.PersistedSnapshot.InspectionSession.FieldNotes)
{
  Console.WriteLine($"    {fieldNote.FieldNoteId}: {fieldNote.RawText}");
}

Console.WriteLine();
Console.WriteLine("Persisted Audit History");
Console.WriteLine("  Project");
foreach (var auditEntry in result.PersistedSnapshot.Project.AuditHistory)
{
  Console.WriteLine($"    {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor} | {auditEntry.Description}");
}

Console.WriteLine("  InspectionSession");
foreach (var auditEntry in result.PersistedSnapshot.InspectionSession.AuditHistory)
{
  Console.WriteLine($"    {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor} | {auditEntry.Description}");
}
