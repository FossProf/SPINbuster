using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadAiProposal;

public sealed record LoadAiProposalResult(
  ProposalId ProposalId,
  ProposalStatus Status,
  ReportId ReportId,
  ProjectId ProjectId,
  InspectionSessionId? InspectionSessionId,
  string ProviderId,
  string ModelName,
  string ModelDigest,
  string PromptPackageId,
  string PromptPackageVersion,
  string OutputSchemaId,
  string OutputSchemaVersion,
  ContextManifestId ContextManifestId,
  string ContextManifestHash,
  ConfidenceBand ConfidenceBand,
  DateTimeOffset GeneratedAtUtc,
  IReadOnlyCollection<string> Warnings,
  IReadOnlyCollection<string> UncertaintyCodes,
  IReadOnlyCollection<string> ValidationFailures,
  IReadOnlyCollection<string> ReferencedSourceIds,
  string StructuredPayloadJson,
  string? AbstentionReason,
  string? ReviewDispositionNotes);
