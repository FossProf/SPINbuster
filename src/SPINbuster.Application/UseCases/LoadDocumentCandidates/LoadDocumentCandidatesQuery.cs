using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentCandidates;

public sealed record LoadDocumentCandidatesQuery(
  ImportedSourceId? ImportedSourceId,
  DocumentProcessingAttemptId? ProcessingAttemptId,
  int MaxResults) : IQuery<LoadDocumentCandidatesResult>;
