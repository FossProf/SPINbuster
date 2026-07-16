using SPINbuster.AI;

namespace SPINbuster.Desktop;

public sealed record DesktopWorkflowSettings(
  string CurrentUserId,
  string ProjectName,
  string SessionName,
  string FieldNoteText,
  string EvidenceFileName,
  string EvidenceMediaType,
  string EvidenceStorageKey,
  string EvidenceChecksum,
  string InterpretationSummary,
  string DraftTitle,
  string DraftSummaryHeading,
  string DraftSummaryContent,
  string DraftObservationHeading,
  string DraftObservationContent,
  Guid ReportOperationId,
  Guid ProposalOperationId,
  string ProposalPromptPackageId,
  string ProposalPromptPackageVersion,
  decimal? ProposalTemperature,
  DeterministicAiScenario AiScenario,
  DesktopAiReviewAction ProposalReviewAction,
  string ProposalReviewNotes,
  DateTimeOffset InitialTimestampUtc);
