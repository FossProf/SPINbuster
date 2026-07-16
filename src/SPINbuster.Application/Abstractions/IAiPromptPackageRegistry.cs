using SPINbuster.Domain;

namespace SPINbuster.Application.Abstractions;

public interface IAiPromptPackageRegistry
{
  Task<PromptPackageDefinition?> GetByIdAsync(
    string packageId,
    string semanticVersion,
    CancellationToken cancellationToken = default);
}
