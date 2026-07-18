using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentProcessingHistory;

public sealed class LoadDocumentProcessingHistoryUseCase : IQueryHandler<LoadDocumentProcessingHistoryQuery, LoadDocumentProcessingHistoryResult>
{
  private readonly IDocumentImportPolicy _documentImportPolicy;
  private readonly IDocumentProcessingAttemptRepository _documentProcessingAttemptRepository;

  public LoadDocumentProcessingHistoryUseCase(
    IDocumentProcessingAttemptRepository documentProcessingAttemptRepository,
    IDocumentImportPolicy documentImportPolicy)
  {
    _documentProcessingAttemptRepository = documentProcessingAttemptRepository;
    _documentImportPolicy = documentImportPolicy;
  }

  public async Task<LoadDocumentProcessingHistoryResult> HandleAsync(LoadDocumentProcessingHistoryQuery query, CancellationToken cancellationToken = default)
  {
    if (query.MaxResults <= 0 || query.MaxResults > _documentImportPolicy.MaxProcessingHistoryResults)
    {
      throw new DomainInvariantException($"Processing history max results must be between 1 and {_documentImportPolicy.MaxProcessingHistoryResults}.");
    }

    var attempts = await _documentProcessingAttemptRepository.GetByImportedSourceAsync(query.ImportedSourceId, query.MaxResults, cancellationToken);
    return new LoadDocumentProcessingHistoryResult(
      query.ImportedSourceId,
      attempts.Select(attempt => new DocumentProcessingAttemptSnapshot(
        attempt.Id,
        attempt.AttemptNumber,
        attempt.ProcessorRole,
        attempt.ProcessorIdentity,
        attempt.ProcessorVersion,
        attempt.State,
        attempt.FailureClassification,
        attempt.FailureDetails,
        attempt.InputContentHash,
        attempt.OutputHash,
        attempt.RequestedAtUtc,
        attempt.StartedAtUtc,
        attempt.CompletedAtUtc)).ToArray());
  }
}
