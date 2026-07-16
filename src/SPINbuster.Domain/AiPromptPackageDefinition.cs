namespace SPINbuster.Domain;

public enum PromptPackageStatus
{
  Draft = 0,
  Evaluating = 1,
  Approved = 2,
  Retired = 3,
}

public sealed class PromptPackageDefinition
{
  public PromptPackageDefinition(
    string packageId,
    string semanticVersion,
    string assignedModelRole,
    string requiredContextPolicyVersion,
    string requiredOutputSchemaId,
    string requiredOutputSchemaVersion,
    IEnumerable<AiProviderCapability> allowedProviderCapabilities,
    PromptPackageStatus status,
    string promptTemplate)
  {
    PackageId = DomainGuards.NotNullOrWhiteSpace(packageId, nameof(packageId));
    SemanticVersion = DomainGuards.NotNullOrWhiteSpace(semanticVersion, nameof(semanticVersion));
    AssignedModelRole = DomainGuards.NotNullOrWhiteSpace(assignedModelRole, nameof(assignedModelRole));
    RequiredContextPolicyVersion = DomainGuards.NotNullOrWhiteSpace(requiredContextPolicyVersion, nameof(requiredContextPolicyVersion));
    RequiredOutputSchemaId = DomainGuards.NotNullOrWhiteSpace(requiredOutputSchemaId, nameof(requiredOutputSchemaId));
    RequiredOutputSchemaVersion = DomainGuards.NotNullOrWhiteSpace(requiredOutputSchemaVersion, nameof(requiredOutputSchemaVersion));
    AllowedProviderCapabilities = (allowedProviderCapabilities ?? [])
      .Distinct()
      .OrderBy(capability => capability)
      .ToArray();
    Status = status;
    PromptTemplate = DomainGuards.NotNullOrWhiteSpace(promptTemplate, nameof(promptTemplate));
  }

  public string PackageId { get; }

  public string SemanticVersion { get; }

  public string AssignedModelRole { get; }

  public string RequiredContextPolicyVersion { get; }

  public string RequiredOutputSchemaId { get; }

  public string RequiredOutputSchemaVersion { get; }

  public IReadOnlyList<AiProviderCapability> AllowedProviderCapabilities { get; }

  public PromptPackageStatus Status { get; }

  public string PromptTemplate { get; }
}
