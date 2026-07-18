using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadImportedDocumentSource;

public sealed class LoadImportedDocumentSourceUseCase : IQueryHandler<LoadImportedDocumentSourceQuery, LoadImportedDocumentSourceResult>
{
  private readonly IImportedDocumentSourceRepository _importedSourceRepository;

  public LoadImportedDocumentSourceUseCase(IImportedDocumentSourceRepository importedSourceRepository)
  {
    _importedSourceRepository = importedSourceRepository;
  }

  public async Task<LoadImportedDocumentSourceResult> HandleAsync(LoadImportedDocumentSourceQuery query, CancellationToken cancellationToken = default)
  {
    var source = await _importedSourceRepository.GetByIdAsync(query.ImportedSourceId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(ImportedDocumentSource), query.ImportedSourceId.ToString());

    return new LoadImportedDocumentSourceResult(
      source.Id,
      source.ImportSessionId,
      source.ProjectId,
      source.OriginalFileName,
      source.DeclaredMediaType,
      source.DetectedMediaType,
      source.ContentLength,
      source.ContentHash,
      source.HashAlgorithm,
      source.HashAlgorithmVersion,
      source.StorageReference.StorageObjectId,
      source.StorageReference.StorageProviderKey,
      source.StorageReference.ImmutableObjectKey,
      source.SourceOrigin,
      source.Status);
  }
}
