using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentCandidates;

public sealed record LoadDocumentCandidatesResult(IReadOnlyList<DocumentCandidateSnapshot> Candidates);

public sealed record DocumentCandidateSnapshot(
  DocumentCandidateId DocumentCandidateId,
  ProjectId ProjectId,
  ImportedSourceId ImportedSourceId,
  DocumentProcessingAttemptId ProcessingAttemptId,
  DocumentCandidateType CandidateType,
  string SchemaId,
  string SchemaVersion,
  string PayloadHash,
  string CanonicalPayload,
  string SourceContentHash,
  string? SourceLocator,
  ConfidenceBand ConfidenceBand,
  IReadOnlyList<string> UncertaintyCodes,
  DocumentCandidateStatus Status,
  DateTimeOffset CreatedAtUtc,
  string? ReviewedBy,
  DateTimeOffset? ReviewedAtUtc,
  string? ReviewNotes);
