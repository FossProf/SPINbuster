using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectDocumentCandidate;

public sealed record RejectDocumentCandidateCommand(DocumentCandidateId DocumentCandidateId, string? ReviewNotes) : ICommand<RejectDocumentCandidateResult>;
