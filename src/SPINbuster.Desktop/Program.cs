using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
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
  var projectAPrimarySource = RequireSingleMatch(
    result.ProjectASnapshot.ImportedSources,
    source => source.ImportedSourceId == result.ImportedSourceA.ImportedSourceId,
    "current-run Project A primary source");
  var projectASecondarySource = RequireSingleMatch(
    result.ProjectASnapshot.ImportedSources,
    source => source.ImportedSourceId == result.ImportedSourceB.ImportedSourceId,
    "current-run Project A secondary source");
  var projectAProcessingAttempt = RequireSingleMatch(
    projectAPrimarySource.ProcessingAttempts,
    attempt => attempt.ProcessingAttemptId == result.RequestedSourceAProcessing.ProcessingAttemptId,
    "current-run Project A processing attempt");
  var acceptedCandidate = RequireSingleMatch(
    projectAPrimarySource.Candidates,
    candidate => candidate.DocumentCandidateId == result.HumanAcceptedCandidate.DocumentCandidateId,
    "current-run accepted candidate");
  var rejectedCandidate = RequireSingleMatch(
    projectAPrimarySource.Candidates,
    candidate => candidate.DocumentCandidateId == result.RejectedCandidate.DocumentCandidateId,
    "current-run rejected candidate");
  var projectBCopySource = RequireSingleMatch(
    result.ProjectBSnapshot.ImportedSources,
    source => source.ImportedSourceId == result.ImportedProjectBCopy.ImportedSourceId,
    "current-run Project B duplicate copy source");

  Console.WriteLine($"Project A: {result.CreatedProjectA.Name}");
  Console.WriteLine("Import Session:");
  Console.WriteLine($"- state: {result.CompletedProjectAImportSession.State}");
  Console.WriteLine($"- accepted count: {result.CompletedProjectAImportSession.AcceptedCount}");
  Console.WriteLine($"- duplicate count: {result.CompletedProjectAImportSession.DuplicateCount}");
  Console.WriteLine($"- rejected count: {result.CompletedProjectAImportSession.RejectedCount}");
  Console.WriteLine();
  Console.WriteLine("Sources:");
  foreach (var source in new[] { projectAPrimarySource, projectASecondarySource }.OrderBy(source => source.OriginalFileName, StringComparer.Ordinal))
  {
    Console.WriteLine($"- source ID: {source.ImportedSourceId}");
    Console.WriteLine($"  filename: {source.OriginalFileName}");
    Console.WriteLine($"  hash: {source.ContentHash}");
    Console.WriteLine($"  storage object ID: {source.Storage.StorageObjectId}");
    Console.WriteLine($"  duplicate status: same-project={source.ImportedSourceId == result.ImportedDuplicateSourceA.ImportedSourceId} cross-project={source.SameContentExistsInAnotherProject}");
  }

  Console.WriteLine();
  Console.WriteLine("Processing:");
  Console.WriteLine($"- attempt ID: {projectAProcessingAttempt.ProcessingAttemptId}");
  Console.WriteLine($"  attempt number: {projectAProcessingAttempt.AttemptNumber}");
  Console.WriteLine($"  state: {projectAProcessingAttempt.State}");
  Console.WriteLine($"  failure classification: {projectAProcessingAttempt.FailureClassification}");

  Console.WriteLine();
  Console.WriteLine("Candidates:");
  foreach (var candidate in new[] { acceptedCandidate, rejectedCandidate }.OrderBy(candidate => candidate.CandidateType))
  {
    Console.WriteLine($"- candidate ID: {candidate.DocumentCandidateId}");
    Console.WriteLine($"  type: {candidate.CandidateType}");
    Console.WriteLine($"  status: {candidate.Status}");
    Console.WriteLine($"  source locator: {candidate.SourceLocator}");
    Console.WriteLine($"  confidence: {candidate.ConfidenceBand}");
    Console.WriteLine($"  source hash: {candidate.SourceContentHash}");
  }

  Console.WriteLine();
  Console.WriteLine("Authority Isolation:");
  Console.WriteLine($"- Knowledge records unchanged: {result.ProjectASnapshot.AuthorityIsolation.KnowledgeDocumentCount == 0 && result.ProjectBSnapshot.AuthorityIsolation.KnowledgeDocumentCount == 0}");
  Console.WriteLine($"- Report unchanged: {result.ProjectASnapshot.AuthorityIsolation.ReportCount == result.ProjectBSnapshot.AuthorityIsolation.ReportCount}");
  Console.WriteLine($"- AI Proposal unchanged: {result.ProjectASnapshot.AuthorityIsolation.AiProposalCount == result.ProjectBSnapshot.AuthorityIsolation.AiProposalCount}");
  Console.WriteLine();
  Console.WriteLine($"Project B: {result.CreatedProjectB.Name}");
  Console.WriteLine($"- identical content detected: {projectBCopySource.SameContentExistsInAnotherProject}");
  Console.WriteLine("- no Project A metadata disclosed: true");
}
catch (Exception exception)
{
  Console.Error.WriteLine($"Desktop workflow failed:{Environment.NewLine}{exception}");
  Environment.ExitCode = 1;
}

static T RequireSingleMatch<T>(
  IEnumerable<T> values,
  Func<T, bool> predicate,
  string description)
{
  var matches = values.Where(predicate).Take(2).ToArray();
  return matches.Length switch
  {
    1 => matches[0],
    0 => throw new InvalidOperationException($"Expected exactly one {description}, but none were found."),
    _ => throw new InvalidOperationException($"Expected exactly one {description}, but multiple matches were found."),
  };
}
