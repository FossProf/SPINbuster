using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.BuildReportProposalContext;

public sealed record BuildReportProposalContextResult(
  ContextManifestId ContextManifestId,
  ProjectId ProjectId,
  InspectionSessionId InspectionSessionId,
  string ContextPolicyVersion,
  string ManifestHash,
  ContextManifestStatus Status,
  IReadOnlyCollection<string> IncompleteReasons,
  IReadOnlyCollection<BuildReportProposalContextSourceEntry> SourceEntries,
  string GovernedPromptContext);

public sealed record BuildReportProposalContextSourceEntry(
  int Order,
  ContextSourceType SourceType,
  string SourceId,
  string SourceVersion,
  string ContentHash,
  AuthorityClassification AuthorityClassification,
  string InclusionReason,
  string? LimitationNotes,
  bool IsSuperseded,
  IReadOnlyCollection<string> ConflictCodes);
