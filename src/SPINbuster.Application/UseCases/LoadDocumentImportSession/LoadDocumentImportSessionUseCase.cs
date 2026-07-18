using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadDocumentImportSession;

public sealed class LoadDocumentImportSessionUseCase : IQueryHandler<LoadDocumentImportSessionQuery, LoadDocumentImportSessionResult>
{
  private readonly IDocumentImportSessionRepository _importSessionRepository;

  public LoadDocumentImportSessionUseCase(IDocumentImportSessionRepository importSessionRepository)
  {
    _importSessionRepository = importSessionRepository;
  }

  public async Task<LoadDocumentImportSessionResult> HandleAsync(LoadDocumentImportSessionQuery query, CancellationToken cancellationToken = default)
  {
    var session = await _importSessionRepository.GetByIdAsync(query.ImportSessionId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(DocumentImportSession), query.ImportSessionId.ToString());

    return new LoadDocumentImportSessionResult(
      session.Id,
      session.ProjectId,
      session.InitiatedBy,
      session.StartedAtUtc,
      session.CompletedAtUtc,
      session.State,
      session.SourceCount,
      session.AcceptedCount,
      session.DuplicateCount,
      session.RejectedCount,
      session.FailureSummary);
  }
}
