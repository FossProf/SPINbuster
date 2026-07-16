using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadKnowledgeRevisionHistory;

public sealed class LoadKnowledgeRevisionHistoryUseCase
  : IQueryHandler<LoadKnowledgeRevisionHistoryQuery, LoadKnowledgeRevisionHistoryResult>
{
  private readonly IKnowledgeRevisionRepository _knowledgeRevisionRepository;

  public LoadKnowledgeRevisionHistoryUseCase(IKnowledgeRevisionRepository knowledgeRevisionRepository)
  {
    _knowledgeRevisionRepository = knowledgeRevisionRepository;
  }

  public async Task<LoadKnowledgeRevisionHistoryResult> HandleAsync(
    LoadKnowledgeRevisionHistoryQuery query,
    CancellationToken cancellationToken = default)
  {
    var revisions = await _knowledgeRevisionRepository.GetByDocumentIdAsync(query.KnowledgeDocumentId, cancellationToken);
    return new LoadKnowledgeRevisionHistoryResult(
      query.KnowledgeDocumentId,
      revisions
        .OrderBy(revision => revision.CreatedAtUtc)
        .Select(revision => new KnowledgeRevisionHistoryEntry(
          revision.Id,
          revision.RevisionLabel,
          revision.EffectiveDate,
          revision.ReceivedAtUtc,
          revision.SourceAuthority,
          revision.VerificationStatus,
          revision.Lifecycle,
          revision.ContentHash,
          revision.MetadataHash,
          revision.SupersedesRevisionId,
          revision.SupersededByRevisionId,
          revision.SourceSystemReference,
          revision.DescriptiveNotes,
          revision.CreatedAtUtc,
          revision.IngestionStatus))
        .ToArray());
  }
}
