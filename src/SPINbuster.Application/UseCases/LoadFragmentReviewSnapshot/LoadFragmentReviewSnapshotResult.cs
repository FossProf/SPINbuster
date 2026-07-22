using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadFragmentReviewSnapshot;

public sealed record LoadFragmentReviewSnapshotResult(
  IReadOnlyList<FragmentReviewSnapshotEntry> Entries,
  int TotalMatchingCount);

public sealed record FragmentReviewSnapshotEntry(
  FragmentCandidateId FragmentCandidateId,
  ParserRunId ParserRunId,
  ImportedSourceId ImportedSourceId,
  string ParserKey,
  string ParserVersion,
  string ParserContractVersion,
  FragmentLocatorType LocatorType,
  string LocatorValue,
  string NormalizedLocator,
  int Ordinal,
  ContentKind ContentKind,
  int TextLength,
  ConfidenceBand ConfidenceBand,
  string IdentityKeyHash,
  FragmentCandidateReviewState ReviewState,
  string? ReviewedBy,
  DateTimeOffset? ReviewedAtUtc,
  string? ReviewNotes,
  string TextPreview,
  DateTimeOffset CreatedAtUtc);

public sealed record FragmentReviewAuditSnapshot(
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
