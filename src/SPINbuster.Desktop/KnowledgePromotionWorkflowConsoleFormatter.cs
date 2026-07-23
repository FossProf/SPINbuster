using System.Globalization;
using System.Text;

namespace SPINbuster.Desktop;

public static class KnowledgePromotionWorkflowConsoleFormatter
{
  public static string Format(KnowledgePromotionWorkflowResult result)
  {
    var sb = new StringBuilder();

    sb.AppendLine("======================================================");
    sb.AppendLine("  Knowledge Promotion Vertical Slice - Desktop Proof");
    sb.AppendLine("======================================================");
    sb.AppendLine();

    sb.AppendLine("--- 1. Project Setup ---");
    AppendLineInvariant(sb, $"  Project:            {result.CreatedProject.ProjectId}");
    AppendLineInvariant(sb, $"  Import Session:     {result.ImportSession.ImportSessionId}");
    AppendLineInvariant(sb, $"  Source A:           {result.ImportedSourceA.ImportedSourceId}");
    AppendLineInvariant(sb, $"  Source B:           {result.ImportedSourceB.ImportedSourceId}");
    AppendLineInvariant(sb, $"  Import Complete:    {result.CompletedImportSession.State}");
    sb.AppendLine();

    sb.AppendLine("--- 2. Parsing (Idempotent Replay) ---");
    AppendLineInvariant(sb, $"  First Parse:        {result.FirstParseResult.State} ({result.FirstParseResult.FragmentCandidateIds.Count} candidates)");
    AppendLineInvariant(sb, $"  Replay Parse:       {result.ReplayParseResult.State} ({result.ReplayParseResult.FragmentCandidateIds.Count} candidates)");
    AppendLineInvariant(sb, $"  Same ParserRunId:   {result.FirstParseResult.ParserRunId == result.ReplayParseResult.ParserRunId}");
    AppendLineInvariant(sb, $"  Same Candidates:    {SequenceEqual(result.FirstParseResult.FragmentCandidateIds, result.ReplayParseResult.FragmentCandidateIds)}");
    AppendLineInvariant(sb, $"  Source B Parse:     {result.SourceBParseResult.State} ({result.SourceBParseResult.FragmentCandidateIds.Count} candidates)");
    sb.AppendLine();

    sb.AppendLine("--- 3. Fragment Review ---");
    AppendLineInvariant(sb, $"  Accepted Candidate: {result.AcceptedCandidateA.ReviewState}");
    AppendLineInvariant(sb, $"  Rejected Candidate: {result.RejectedCandidateA.ReviewState}");
    AppendLineInvariant(sb, $"  Review Snapshot (Accepted): {result.ReviewSnapshotAfterAccept.TotalMatchingCount} entry(ies)");
    AppendLineInvariant(sb, $"  Review Snapshot (Rejected): {result.ReviewSnapshotAfterReject.TotalMatchingCount} entry(ies)");
    sb.AppendLine();

    sb.AppendLine("--- 4. Promotion (Idempotent) ---");
    AppendLineInvariant(sb, $"  First Promotion:    {result.FirstPromotion.Status}");
    AppendLineInvariant(sb, $"    Document:         {result.FirstPromotion.KnowledgeDocumentId}");
    AppendLineInvariant(sb, $"    Revision:         {result.FirstPromotion.KnowledgeDocumentRevisionId}");
    AppendLineInvariant(sb, $"    Citation:         {result.FirstPromotion.KnowledgeCitationId}");
    AppendLineInvariant(sb, $"    Superseded Prior: {result.FirstPromotion.SupersededExistingRevision}");
    AppendLineInvariant(sb, $"  Idempotent Replay:  {result.IdempotentReplay.Status}");
    AppendLineInvariant(sb, $"    Same Doc:         {result.FirstPromotion.KnowledgeDocumentId == result.IdempotentReplay.KnowledgeDocumentId}");
    AppendLineInvariant(sb, $"    Same Revision:    {result.FirstPromotion.KnowledgeDocumentRevisionId == result.IdempotentReplay.KnowledgeDocumentRevisionId}");
    AppendLineInvariant(sb, $"    Same Citation:    {result.FirstPromotion.KnowledgeCitationId == result.IdempotentReplay.KnowledgeCitationId}");
    AppendLineInvariant(sb, $"  First Diagnostic:   {result.FirstDiagnostic.Status}");
    AppendLineInvariant(sb, $"  Replay Diagnostic:  {result.ReplayDiagnostic.Status}");
    sb.AppendLine();

    sb.AppendLine("--- 5. Supersession (Second Source) ---");
    AppendLineInvariant(sb, $"  Accepted Candidate B: {result.SupersedingPromotion.Status}");
    AppendLineInvariant(sb, $"    Document:           {result.SupersedingPromotion.KnowledgeDocumentId}");
    AppendLineInvariant(sb, $"    Revision:           {result.SupersedingPromotion.KnowledgeDocumentRevisionId}");
    AppendLineInvariant(sb, $"    Superseded Prior:   {result.SupersedingPromotion.SupersededExistingRevision}");
    AppendLineInvariant(sb, $"    Superseded Rev Id:  {result.SupersedingPromotion.SupersededRevisionId}");
    AppendLineInvariant(sb, $"  Supersession Replay:  {result.SupersessionIdempotentReplay.Status}");
    AppendLineInvariant(sb, $"    Same Doc:           {result.SupersedingPromotion.KnowledgeDocumentId == result.SupersessionIdempotentReplay.KnowledgeDocumentId}");
    AppendLineInvariant(sb, $"    Same Revision:      {result.SupersedingPromotion.KnowledgeDocumentRevisionId == result.SupersessionIdempotentReplay.KnowledgeDocumentRevisionId}");
    sb.AppendLine();

    sb.AppendLine("--- 6. Knowledge Snapshot (Provenance) ---");
    AppendLineInvariant(sb, $"  Documents:          {result.KnowledgeSnapshot.Documents.Count}");
    foreach (var doc in result.KnowledgeSnapshot.Documents)
    {
      AppendLineInvariant(sb, $"    [{doc.DocumentType}] {doc.CanonicalTitle}");
      AppendLineInvariant(sb, $"      Lifecycle:        {doc.Lifecycle}");
      AppendLineInvariant(sb, $"      Authoritative:    {doc.CurrentAuthoritativeRevisionId}");
      AppendLineInvariant(sb, $"      Revisions:        {doc.Revisions.Count}");
      foreach (var rev in doc.Revisions)
      {
        AppendLineInvariant(sb, $"        {rev.RevisionLabel} ({rev.Lifecycle}) - {rev.ContentHash[..12]}...");
        AppendLineInvariant(sb, $"          Citations:    {rev.Citations.Count}");
        foreach (var citation in rev.Citations)
        {
          AppendLineInvariant(sb, $"            {citation.LocatorType}:{citation.LocatorValue}");
        }
      }
    }
    AppendLineInvariant(sb, $"  Relationships:      {result.KnowledgeSnapshot.Relationships.Count}");
    foreach (var rel in result.KnowledgeSnapshot.Relationships)
    {
      AppendLineInvariant(sb, $"    {rel.RelationshipType}: {rel.Source.StableKey} -> {rel.Target.StableKey}");
    }
    sb.AppendLine();

    sb.AppendLine("--- 7. Authority Isolation ---");
    sb.AppendLine("  AI does not participate in promotion decisions.");
    sb.AppendLine("  Promotion is strictly deterministic: human review + deterministic rules only.");
    AppendLineInvariant(sb, $"  Total Promotion Diagnostics: {result.PromotionDiagnostics.Count}");
    foreach (var diag in result.PromotionDiagnostics)
    {
      AppendLineInvariant(sb, $"    {diag.PromotionDiagnosticId}: {diag.Status} (superseded={diag.SupersededExistingRevision})");
    }
    sb.AppendLine();

    sb.AppendLine("--- 8. Failure Scenarios ---");
    foreach (var failure in result.FailurePresentations)
    {
      AppendLineInvariant(sb, $"  [{failure.Scenario}] {failure.ErrorType}: {failure.Message}");
    }
    sb.AppendLine();

    sb.AppendLine("======================================================");
    sb.AppendLine("  Workflow completed successfully.");
    sb.AppendLine("======================================================");

    return sb.ToString();
  }

  private static void AppendLineInvariant(StringBuilder builder, FormattableString value)
  {
    builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
  }

  private static bool SequenceEqual<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
  {
    if (a.Count != b.Count)
    {
      return false;
    }

    for (var i = 0; i < a.Count; i++)
    {
      if (!EqualityComparer<T>.Default.Equals(a[i], b[i]))
      {
        return false;
      }
    }

    return true;
  }
}
