using SPINbuster.Domain;

namespace SPINbuster.Application.Repositories;

public interface IContextManifestRepository
{
  Task<ContextManifest?> GetByIdAsync(
    ContextManifestId contextManifestId,
    CancellationToken cancellationToken = default);

  Task AddAsync(ContextManifest contextManifest, CancellationToken cancellationToken = default);
}
