using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestDocumentProcessing;

public sealed record RequestDocumentProcessingResult(
  DocumentProcessingAttemptId ProcessingAttemptId,
  ImportedSourceId ImportedSourceId,
  DocumentProcessingAttemptState State,
  DocumentProcessingFailureClassification FailureClassification,
  int CandidateCount,
  IReadOnlyList<DocumentCandidateId> CandidateIds);
