using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadParsingSnapshot;

public sealed record LoadParsingSnapshotResult(
  ImportedSourceId ImportedSourceId,
  ProjectId ProjectId,
  string ContentHash,
  string HashAlgorithm,
  int HashAlgorithmVersion,
  long ContentLength,
  IReadOnlyList<ParserRunSnapshot> ParserRuns);

public sealed record ParserRunSnapshot(
  ParserRunId ParserRunId,
  string ParserKey,
  string ParserVersion,
  string ParserContractVersion,
  string ParserContractHash,
  ParserRunState State,
  string? FailureReason,
  DateTimeOffset CreatedAtUtc,
  DateTimeOffset? StartedAtUtc,
  DateTimeOffset? CompletedAtUtc,
  IReadOnlyList<FragmentCandidateSnapshot> FragmentCandidates,
  IReadOnlyList<AuditEventSnapshot> AuditHistory);

public sealed record FragmentCandidateSnapshot(
  FragmentCandidateId FragmentCandidateId,
  FragmentLocatorType LocatorType,
  string LocatorValue,
  string NormalizedLocatorValue,
  int Ordinal,
  ContentKind ContentKind,
  int TextLength,
  ConfidenceBand ConfidenceBand,
  string IdentityKeyHash,
  DateTimeOffset CreatedAtUtc);

public sealed record AuditEventSnapshot(
  string EventType,
  string Actor,
  DateTimeOffset OccurredAtUtc,
  string Description);
