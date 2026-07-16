using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ModelRunRecord
{
  public ModelRunId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public InspectionSessionId? InspectionSessionId { get; set; }

  public ReportId ReportId { get; set; }

  public string InitiatedBy { get; set; } = string.Empty;

  public ContextManifestId ContextManifestId { get; set; }

  public string ContextManifestHash { get; set; } = string.Empty;

  public string ProviderId { get; set; } = string.Empty;

  public string ModelName { get; set; } = string.Empty;

  public string ModelDigest { get; set; } = string.Empty;

  public string PromptPackageId { get; set; } = string.Empty;

  public string PromptPackageVersion { get; set; } = string.Empty;

  public string OutputSchemaId { get; set; } = string.Empty;

  public string OutputSchemaVersion { get; set; } = string.Empty;

  public string CorrelationId { get; set; } = string.Empty;

  public string RequestFingerprintHash { get; set; } = string.Empty;

  public DateTimeOffset RequestedAtUtc { get; set; }

  public ModelRunState State { get; set; }

  public ModelRunFailureClassification FailureClassification { get; set; }

  public string? FailureMessage { get; set; }

  public List<ModelRunAttemptRecord> Attempts { get; } = [];
}
