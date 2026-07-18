using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentProcessingHistory;

public sealed record LoadDocumentProcessingHistoryResult(ImportedSourceId ImportedSourceId, IReadOnlyList<DocumentProcessingAttemptSnapshot> Attempts);

public sealed record DocumentProcessingAttemptSnapshot(
  DocumentProcessingAttemptId ProcessingAttemptId,
  int AttemptNumber,
  string ProcessorRole,
  string ProcessorIdentity,
  string ProcessorVersion,
  DocumentProcessingAttemptState State,
  DocumentProcessingFailureClassification FailureClassification,
  string? FailureDetails,
  string InputContentHash,
  string? OutputHash,
  DateTimeOffset RequestedAtUtc,
  DateTimeOffset? StartedAtUtc,
  DateTimeOffset? CompletedAtUtc);
