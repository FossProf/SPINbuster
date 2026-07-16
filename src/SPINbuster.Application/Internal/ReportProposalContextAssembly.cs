using System.Security.Cryptography;
using System.Text;
using SPINbuster.Domain;

namespace SPINbuster.Application.Internal;

internal sealed class ReportProposalContextAssembly
{
  public ReportProposalContextAssembly(
    ContextManifest contextManifest,
    string governedPromptContext,
    string inputHash)
  {
    ContextManifest = contextManifest;
    GovernedPromptContext = governedPromptContext;
    InputHash = inputHash;
  }

  public ContextManifest ContextManifest { get; }

  public string GovernedPromptContext { get; }

  public string InputHash { get; }

  public static ReportProposalContextAssembly Create(
    Project project,
    InspectionSession inspectionSession,
    Report report,
    string contextPolicyVersion,
    DateTimeOffset createdAtUtc)
  {
    var sourceEntries = new List<ContextManifestSourceEntry>();
    var order = 0;

    static string ComputeHash(string value)
    {
      var bytes = Encoding.UTF8.GetBytes(value);
      return Convert.ToHexString(SHA256.HashData(bytes));
    }

    sourceEntries.Add(new ContextManifestSourceEntry(
      order++,
      project.Id,
      ContextSourceType.Report,
      report.Id.ToString(),
      $"revision-{report.RevisionNumber}",
      ComputeHash($"{report.Title.Value}|{report.RevisionNumber}|{report.Lifecycle}"),
      AuthorityClassification.Authoritative,
      "Current authoritative report draft boundary.",
      null,
      false,
      []));

    foreach (var section in report.Sections.Select((value, index) => (value, index)))
    {
      sourceEntries.Add(new ContextManifestSourceEntry(
        order++,
        project.Id,
        ContextSourceType.ReportSection,
        $"{report.Id}:{section.index}",
        $"revision-{report.RevisionNumber}",
        ComputeHash($"{section.index}|{section.value.Heading}|{section.value.Content}"),
        AuthorityClassification.Authoritative,
        "Current report draft section content.",
        null,
        false,
        []));
    }

    foreach (var fieldNoteId in report.SourceFieldNoteIds)
    {
      var fieldNote = inspectionSession.FieldNotes.Single(fieldNote => fieldNote.Id == fieldNoteId);
      sourceEntries.Add(new ContextManifestSourceEntry(
        order++,
        project.Id,
        ContextSourceType.FieldNote,
        fieldNote.Id.ToString(),
        "raw-v1",
        ComputeHash(fieldNote.RawText.Value),
        AuthorityClassification.Authoritative,
        "Field note explicitly referenced by the authoritative report draft.",
        null,
        false,
        []));
    }

    foreach (var evidenceAttachmentId in report.SourceEvidenceAttachmentIds)
    {
      var evidenceAttachment = inspectionSession.EvidenceAttachments.Single(evidence => evidence.Id == evidenceAttachmentId);
      sourceEntries.Add(new ContextManifestSourceEntry(
        order++,
        project.Id,
        ContextSourceType.EvidenceAttachment,
        evidenceAttachment.Id.ToString(),
        "raw-v1",
        ComputeHash($"{evidenceAttachment.RawEvidence.FileName}|{evidenceAttachment.RawEvidence.MediaType}|{evidenceAttachment.RawEvidence.StorageKey}|{evidenceAttachment.RawEvidence.Checksum}"),
        AuthorityClassification.Authoritative,
        "Raw evidence explicitly referenced by the authoritative report draft.",
        evidenceAttachment.Interpretation is null ? "No interpretation is currently available." : null,
        false,
        evidenceAttachment.Interpretation is null ? ["missing-interpretation"] : []));

      if (evidenceAttachment.Interpretation is not null)
      {
        sourceEntries.Add(new ContextManifestSourceEntry(
          order++,
          project.Id,
          ContextSourceType.EvidenceInterpretation,
          evidenceAttachment.Id.ToString(),
          "interpretation-v1",
          ComputeHash($"{evidenceAttachment.Interpretation.Summary}|{evidenceAttachment.Interpretation.InterpretedBy}|{evidenceAttachment.Interpretation.InterpretedAtUtc:O}"),
          AuthorityClassification.Derived,
          "Single permitted interpretation attached to the evidence.",
          null,
          false,
          []));
      }
    }

    var incompleteReasons = new List<string>();
    if (report.SourceFieldNoteIds.Count == 0 && report.SourceEvidenceAttachmentIds.Count == 0)
    {
      incompleteReasons.Add("report-has-no-authoritative-sources");
    }

    var manifest = new ContextManifest(
      ContextManifestId.New(),
      project.Id,
      inspectionSession.Id,
      contextPolicyVersion,
      sourceEntries,
      incompleteReasons,
      createdAtUtc);

    var governedPromptContext = BuildGovernedPromptContext(project, inspectionSession, report);
    return new ReportProposalContextAssembly(manifest, governedPromptContext, ComputeHash(governedPromptContext));
  }

  private static string BuildGovernedPromptContext(
    Project project,
    InspectionSession inspectionSession,
    Report report)
  {
    var builder = new StringBuilder();
    builder.AppendLine("SPINbuster governed report-draft proposal context");
    builder.AppendLine(FormattableString.Invariant($"Project: {project.Name} ({project.Id})"));
    builder.AppendLine(FormattableString.Invariant($"Inspection Session: {inspectionSession.Name} ({inspectionSession.Id})"));
    builder.AppendLine(FormattableString.Invariant($"Report: {report.Title.Value} ({report.Id}) revision {report.RevisionNumber} state {report.Lifecycle}"));
    builder.AppendLine("Authoritative draft sections:");

    foreach (var section in report.Sections.Select((value, index) => (value, index)))
    {
      builder.AppendLine(FormattableString.Invariant($"- Section {section.index + 1}: {section.value.Heading}"));
      builder.AppendLine(section.value.Content);
    }

    builder.AppendLine("Authoritative field notes:");
    foreach (var fieldNoteId in report.SourceFieldNoteIds)
    {
      var fieldNote = inspectionSession.FieldNotes.Single(note => note.Id == fieldNoteId);
      builder.AppendLine(FormattableString.Invariant($"- FieldNote {fieldNote.Id}: {fieldNote.RawText.Value}"));
    }

    builder.AppendLine("Authoritative evidence:");
    foreach (var evidenceAttachmentId in report.SourceEvidenceAttachmentIds)
    {
      var evidence = inspectionSession.EvidenceAttachments.Single(attachment => attachment.Id == evidenceAttachmentId);
      builder.AppendLine(FormattableString.Invariant($"- Evidence {evidence.Id}: {evidence.RawEvidence.FileName} [{evidence.RawEvidence.MediaType}]"));
      builder.AppendLine(FormattableString.Invariant($"  StorageKey: {evidence.RawEvidence.StorageKey}"));
      builder.AppendLine(FormattableString.Invariant($"  Checksum: {evidence.RawEvidence.Checksum}"));
      if (evidence.Interpretation is not null)
      {
        builder.AppendLine(FormattableString.Invariant($"  Interpretation: {evidence.Interpretation.Summary}"));
      }
      else
      {
        builder.AppendLine("  Interpretation: none");
      }
    }

    builder.AppendLine("Operating rules:");
    builder.AppendLine("- AI output is advisory only.");
    builder.AppendLine("- Do not approve or issue reports.");
    builder.AppendLine("- Do not fabricate sources or facts.");
    builder.AppendLine("- Do not modify authoritative records.");
    return builder.ToString().TrimEnd();
  }
}
