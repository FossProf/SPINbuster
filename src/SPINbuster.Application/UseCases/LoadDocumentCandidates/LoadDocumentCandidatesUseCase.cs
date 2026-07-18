using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentCandidates;

public sealed class LoadDocumentCandidatesUseCase : IQueryHandler<LoadDocumentCandidatesQuery, LoadDocumentCandidatesResult>
{
  private readonly IDocumentCandidateRepository _documentCandidateRepository;
  private readonly IDocumentImportPolicy _documentImportPolicy;

  public LoadDocumentCandidatesUseCase(
    IDocumentCandidateRepository documentCandidateRepository,
    IDocumentImportPolicy documentImportPolicy)
  {
    _documentCandidateRepository = documentCandidateRepository;
    _documentImportPolicy = documentImportPolicy;
  }

  public async Task<LoadDocumentCandidatesResult> HandleAsync(LoadDocumentCandidatesQuery query, CancellationToken cancellationToken = default)
  {
    if (query.ImportedSourceId is null && query.ProcessingAttemptId is null)
    {
      throw new DomainInvariantException("Either an imported source ID or processing attempt ID must be provided.");
    }

    if (query.MaxResults <= 0 || query.MaxResults > _documentImportPolicy.MaxCandidateQueryResults)
    {
      throw new DomainInvariantException($"Candidate max results must be between 1 and {_documentImportPolicy.MaxCandidateQueryResults}.");
    }

    IReadOnlyCollection<DocumentCandidate> candidates = query.ProcessingAttemptId is not null
      ? await _documentCandidateRepository.GetByProcessingAttemptAsync(query.ProcessingAttemptId.Value, query.MaxResults, cancellationToken)
      : await _documentCandidateRepository.GetByImportedSourceAsync(query.ImportedSourceId!.Value, query.MaxResults, cancellationToken);

    return new LoadDocumentCandidatesResult(
      candidates.Select(candidate => new DocumentCandidateSnapshot(
        candidate.Id,
        candidate.ProjectId,
        candidate.ImportedSourceId,
        candidate.ProcessingAttemptId,
        candidate.CandidateType,
        candidate.SchemaId,
        candidate.SchemaVersion,
        candidate.PayloadHash,
        candidate.CanonicalPayload,
        candidate.SourceContentHash,
        candidate.SourceLocator,
        candidate.ConfidenceBand,
        candidate.UncertaintyCodes,
        candidate.Status,
        candidate.CreatedAtUtc,
        candidate.ReviewedBy,
        candidate.ReviewedAtUtc,
        candidate.ReviewNotes)).ToArray());
  }
}
