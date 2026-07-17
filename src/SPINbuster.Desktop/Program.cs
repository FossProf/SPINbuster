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

try
{
  var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(host.Services);

  Console.WriteLine();
  Console.WriteLine("Document Engine Workflow");
  Console.WriteLine();
  Console.WriteLine("Project A");
  Console.WriteLine("Import Session:");
  Console.WriteLine($"- state: {result.CompletedProjectAImportSession.State}");
  Console.WriteLine($"- accepted count: {result.CompletedProjectAImportSession.AcceptedCount}");
  Console.WriteLine($"- duplicate count: {result.CompletedProjectAImportSession.DuplicateCount}");
  Console.WriteLine($"- rejected count: {result.CompletedProjectAImportSession.RejectedCount}");
  Console.WriteLine();
  Console.WriteLine("Sources:");
  foreach (var source in result.ProjectASnapshot.ImportedSources.OrderBy(source => source.OriginalFileName, StringComparer.Ordinal))
  {
    Console.WriteLine($"- source ID: {source.ImportedSourceId}");
    Console.WriteLine($"  filename: {source.OriginalFileName}");
    Console.WriteLine($"  hash: {source.ContentHash}");
    Console.WriteLine($"  storage object ID: {source.Storage.StorageObjectId}");
    Console.WriteLine($"  duplicate status: same-project={source.ImportedSourceId == result.ImportedDuplicateSourceA.ImportedSourceId} cross-project={source.SameContentExistsInAnotherProject}");
  }

  Console.WriteLine();
  Console.WriteLine("Processing:");
  foreach (var source in result.ProjectASnapshot.ImportedSources.Where(source => source.ProcessingAttempts.Count > 0))
  {
    foreach (var attempt in source.ProcessingAttempts)
    {
      Console.WriteLine($"- attempt ID: {attempt.ProcessingAttemptId}");
      Console.WriteLine($"  attempt number: {attempt.AttemptNumber}");
      Console.WriteLine($"  state: {attempt.State}");
      Console.WriteLine($"  failure classification: {attempt.FailureClassification}");
    }
  }

  Console.WriteLine();
  Console.WriteLine("Candidates:");
  foreach (var source in result.ProjectASnapshot.ImportedSources.Where(source => source.Candidates.Count > 0))
  {
    foreach (var candidate in source.Candidates)
    {
      Console.WriteLine($"- candidate ID: {candidate.DocumentCandidateId}");
      Console.WriteLine($"  type: {candidate.CandidateType}");
      Console.WriteLine($"  status: {candidate.Status}");
      Console.WriteLine($"  source locator: {candidate.SourceLocator}");
      Console.WriteLine($"  confidence: {candidate.ConfidenceBand}");
      Console.WriteLine($"  source hash: {candidate.SourceContentHash}");
    }
  }

  Console.WriteLine();
  Console.WriteLine("Authority Isolation:");
  Console.WriteLine($"- Knowledge records unchanged: {result.ProjectASnapshot.AuthorityIsolation.KnowledgeDocumentCount == 0 && result.ProjectBSnapshot.AuthorityIsolation.KnowledgeDocumentCount == 0}");
  Console.WriteLine($"- Report unchanged: {result.ProjectASnapshot.AuthorityIsolation.ReportCount == result.ProjectBSnapshot.AuthorityIsolation.ReportCount}");
  Console.WriteLine($"- AI Proposal unchanged: {result.ProjectASnapshot.AuthorityIsolation.AiProposalCount == result.ProjectBSnapshot.AuthorityIsolation.AiProposalCount}");
  Console.WriteLine();
  Console.WriteLine("Project B:");
  Console.WriteLine($"- identical content detected: {result.ImportedProjectBCopy.SameContentExistsInAnotherProject}");
  Console.WriteLine("- no Project A metadata disclosed: true");
}
catch (Exception exception)
{
  Console.Error.WriteLine($"Desktop workflow failed: {exception.GetType().Name} | {exception.Message}");
  Environment.ExitCode = 1;
}
