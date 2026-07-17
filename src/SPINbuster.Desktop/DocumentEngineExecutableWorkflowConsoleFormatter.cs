using System.Text;
using System.Globalization;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public static class DocumentEngineExecutableWorkflowConsoleFormatter
{
  public static string Format(DocumentEngineExecutableWorkflowResult result)
  {
    var builder = new StringBuilder();

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

    builder.AppendLine();
    builder.AppendLine("Document Engine Workflow");
    builder.AppendLine();
    AppendLineInvariant(builder, $"Project A: {result.CreatedProjectA.Name}");
    builder.AppendLine("Import Session:");
    AppendLineInvariant(builder, $"- state: {result.CompletedProjectAImportSession.State}");
    AppendLineInvariant(builder, $"- accepted count: {result.CompletedProjectAImportSession.AcceptedCount}");
    AppendLineInvariant(builder, $"- duplicate count: {result.CompletedProjectAImportSession.DuplicateCount}");
    AppendLineInvariant(builder, $"- rejected count: {result.CompletedProjectAImportSession.RejectedCount}");
    builder.AppendLine();
    builder.AppendLine("Sources:");
    foreach (var source in new[] { projectAPrimarySource, projectASecondarySource }.OrderBy(source => source.OriginalFileName, StringComparer.Ordinal))
    {
      AppendLineInvariant(builder, $"- source ID: {source.ImportedSourceId}");
      AppendLineInvariant(builder, $"  filename: {source.OriginalFileName}");
      AppendLineInvariant(builder, $"  hash: {source.ContentHash}");
      AppendLineInvariant(builder, $"  storage object ID: {source.Storage.StorageObjectId}");
      AppendLineInvariant(builder, $"  object key: {source.Storage.ImmutableObjectKey}");
      AppendLineInvariant(builder, $"  duplicate status: same-project={source.ImportedSourceId == result.ImportedDuplicateSourceA.ImportedSourceId} cross-project={source.SameContentExistsInAnotherProject}");
    }

    builder.AppendLine();
    builder.AppendLine("Processing:");
    AppendLineInvariant(builder, $"- attempt ID: {projectAProcessingAttempt.ProcessingAttemptId}");
    AppendLineInvariant(builder, $"  attempt number: {projectAProcessingAttempt.AttemptNumber}");
    AppendLineInvariant(builder, $"  state: {projectAProcessingAttempt.State}");
    AppendLineInvariant(builder, $"  failure classification: {projectAProcessingAttempt.FailureClassification}");

    builder.AppendLine();
    builder.AppendLine("Candidates:");
    foreach (var candidate in new[] { acceptedCandidate, rejectedCandidate }.OrderBy(candidate => candidate.CandidateType))
    {
      AppendLineInvariant(builder, $"- candidate ID: {candidate.DocumentCandidateId}");
      AppendLineInvariant(builder, $"  type: {candidate.CandidateType}");
      AppendLineInvariant(builder, $"  status: {candidate.Status}");
      AppendLineInvariant(builder, $"  source locator: {candidate.SourceLocator}");
      AppendLineInvariant(builder, $"  confidence: {candidate.ConfidenceBand}");
      AppendLineInvariant(builder, $"  source hash: {candidate.SourceContentHash}");
    }

    builder.AppendLine();
    builder.AppendLine("Authority Isolation:");
    AppendLineInvariant(builder, $"- Knowledge records unchanged: {result.ProjectASnapshot.AuthorityIsolation.KnowledgeDocumentCount == 0 && result.ProjectBSnapshot.AuthorityIsolation.KnowledgeDocumentCount == 0}");
    AppendLineInvariant(builder, $"- Report unchanged: {result.ProjectASnapshot.AuthorityIsolation.ReportCount == result.ProjectBSnapshot.AuthorityIsolation.ReportCount}");
    AppendLineInvariant(builder, $"- AI Proposal unchanged: {result.ProjectASnapshot.AuthorityIsolation.AiProposalCount == result.ProjectBSnapshot.AuthorityIsolation.AiProposalCount}");
    builder.AppendLine();
    AppendLineInvariant(builder, $"Project B: {result.CreatedProjectB.Name}");
    AppendLineInvariant(builder, $"- identical content detected: {projectBCopySource.SameContentExistsInAnotherProject}");
    builder.AppendLine("- no Project A metadata disclosed: true");

    return builder.ToString();
  }

  private static T RequireSingleMatch<T>(
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

  private static void AppendLineInvariant(StringBuilder builder, FormattableString value)
  {
    builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
  }
}
