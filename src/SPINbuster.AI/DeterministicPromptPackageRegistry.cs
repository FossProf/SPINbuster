using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;

namespace SPINbuster.AI;

public sealed class DeterministicPromptPackageRegistry : IAiPromptPackageRegistry
{
  private static readonly PromptPackageDefinition ApprovedPackage = new(
    "report-draft-proposal-default",
    "0.1.0",
    "report-draft-proposer",
    "report-draft-context-policy/1.0",
    "report-draft-proposal",
    "1.0.0",
    [
      AiProviderCapability.StructuredOutput,
      AiProviderCapability.DeterministicFixtures,
    ],
    PromptPackageStatus.Approved,
    "Propose advisory report-draft revisions using governed SPINbuster context only.");

  public Task<PromptPackageDefinition?> GetByIdAsync(
    string packageId,
    string semanticVersion,
    CancellationToken cancellationToken = default)
  {
    PromptPackageDefinition? package = string.Equals(packageId, ApprovedPackage.PackageId, StringComparison.Ordinal)
      && string.Equals(semanticVersion, ApprovedPackage.SemanticVersion, StringComparison.Ordinal)
        ? ApprovedPackage
        : null;

    return Task.FromResult(package);
  }
}
