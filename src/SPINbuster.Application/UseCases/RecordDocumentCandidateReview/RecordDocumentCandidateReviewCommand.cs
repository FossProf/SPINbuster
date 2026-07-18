using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RecordDocumentCandidateReview;

public sealed record RecordDocumentCandidateReviewCommand(
  DocumentCandidateId DocumentCandidateId,
  DocumentCandidateReviewDisposition Disposition,
  string? ReviewNotes) : ICommand<RecordDocumentCandidateReviewResult>;

public enum DocumentCandidateReviewDisposition
{
  HumanAccepted = 0,
  Rejected = 1,
}
