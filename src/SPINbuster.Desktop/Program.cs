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
  var result = await DesktopWorkflowBootstrapper.RunAsync(host.Services);

  Console.WriteLine($"Project created: {result.CreatedProject.ProjectId} ({result.PersistedInspectionSnapshot.Project.Lifecycle})");
  Console.WriteLine($"Inspection session started: {result.StartedInspectionSession.InspectionSessionId} ({result.PersistedInspectionSnapshot.InspectionSession.Lifecycle})");
  Console.WriteLine($"Field note captured: {result.CapturedFieldNote.FieldNoteId}");
  Console.WriteLine($"Evidence attached: {result.AttachedEvidence.EvidenceAttachmentId}");
  Console.WriteLine($"Evidence interpreted: {result.AddedInterpretation.EvidenceAttachmentId}");
  Console.WriteLine($"Report draft created: {result.CreatedReportDraft.ReportId} ({result.PersistedReportSnapshot.Lifecycle})");
  Console.WriteLine($"AI proposal replay: idempotent={result.ReplayedAiProposalRequest.IsIdempotentReplay} proposal={result.ReplayedAiProposalRequest.ProposalId}");
  Console.WriteLine($"Specification registered: {result.RegisteredSpecificationDocument.KnowledgeDocumentId}");
  Console.WriteLine($"Specification revision chain: {result.AddedSpecificationInitialRevision.KnowledgeDocumentRevisionId} -> {result.SupersededSpecificationRevision.SuccessorRevisionId}");
  Console.WriteLine($"RFI registered: {result.RegisteredRfiDocument.KnowledgeDocumentId}");
  Console.WriteLine($"Knowledge relationship created: {result.CreatedKnowledgeRelationship.KnowledgeRelationshipId}");
  Console.WriteLine($"Knowledge citation added: {result.AddedKnowledgeCitation.KnowledgeCitationId}");
  Console.WriteLine();
  Console.WriteLine("Reloaded Knowledge Snapshot");
  Console.WriteLine($"  Project: {result.ReloadedKnowledgeSnapshot.ProjectId}");

  foreach (var document in result.ReloadedKnowledgeSnapshot.Documents)
  {
    Console.WriteLine($"  Document {document.KnowledgeDocumentId}");
    Console.WriteLine($"    Type: {document.DocumentType}");
    Console.WriteLine($"    Title: {document.CanonicalTitle}");
    Console.WriteLine($"    ExternalRef: {document.ExternalReferenceNumber}");
    Console.WriteLine($"    Lifecycle: {document.Lifecycle}");
    Console.WriteLine($"    CurrentRevision: {document.CurrentAuthoritativeRevisionId}");

    foreach (var revision in document.Revisions)
    {
      Console.WriteLine($"    Revision {revision.KnowledgeDocumentRevisionId}");
      Console.WriteLine($"      Label: {revision.RevisionLabel}");
      Console.WriteLine($"      Lifecycle: {revision.Lifecycle}");
      Console.WriteLine($"      Verification: {revision.VerificationStatus}");
      Console.WriteLine($"      Supersedes: {revision.SupersedesRevisionId}");
      Console.WriteLine($"      SupersededBy: {revision.SupersededByRevisionId}");

      foreach (var citation in revision.Citations)
      {
        Console.WriteLine($"      Citation: {citation.LocatorType} {citation.LocatorValue} | hash={citation.RevisionContentHash}");
      }

      foreach (var auditEntry in revision.AuditHistory)
      {
        Console.WriteLine($"      Audit: {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor}");
      }
    }

    foreach (var auditEntry in document.AuditHistory)
    {
      Console.WriteLine($"    DocumentAudit: {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor}");
    }
  }

  Console.WriteLine();
  Console.WriteLine("Relationship Graph");
  foreach (var relationship in result.ReloadedKnowledgeSnapshot.Relationships)
  {
    Console.WriteLine(
      $"  {relationship.KnowledgeRelationshipId}: {relationship.Source.StableKey} -[{relationship.RelationshipType}]-> {relationship.Target.StableKey}");
    Console.WriteLine($"    Rationale: {relationship.EvidenceOrRationale}");
    foreach (var auditEntry in relationship.AuditHistory)
    {
      Console.WriteLine($"    Audit: {auditEntry.OccurredAtUtc:O} | {auditEntry.EventType} | {auditEntry.Actor}");
    }
  }

  Console.WriteLine();
  Console.WriteLine("Expected Failure Presentations");
  foreach (var failure in result.FailurePresentations)
  {
    Console.WriteLine($"  {failure.Scenario}: {failure.ErrorType} | {failure.Message}");
  }

  Console.WriteLine();
  Console.WriteLine("Authoritative State After Knowledge Workflow");
  Console.WriteLine($"  Report revision: {result.PersistedReportSnapshot.RevisionNumber}");
  Console.WriteLine($"  Report lifecycle: {result.PersistedReportSnapshot.Lifecycle}");
  Console.WriteLine($"  AI proposal state: {result.ReviewedAiProposalSnapshot.ModelRunState}");
  Console.WriteLine($"  AI proposal status: {result.ReviewedAiProposalSnapshot.Proposal?.Status}");
}
catch (Exception exception)
{
  Console.Error.WriteLine($"Desktop workflow failed: {exception.GetType().Name} | {exception.Message}");
  Environment.ExitCode = 1;
}
