using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class AiProposalRecord
{
  public ProposalId Id { get; set; }

  public ModelRunId ModelRunId { get; set; }

  public ProjectId ProjectId { get; set; }

  public InspectionSessionId? InspectionSessionId { get; set; }

  public ReportId ReportId { get; set; }

  public string ProviderId { get; set; } = string.Empty;

  public string ModelName { get; set; } = string.Empty;

  public string ModelDigest { get; set; } = string.Empty;

  public string PromptPackageId { get; set; } = string.Empty;

  public string PromptPackageVersion { get; set; } = string.Empty;

  public string OutputSchemaId { get; set; } = string.Empty;

  public string OutputSchemaVersion { get; set; } = string.Empty;

  public ContextManifestId ContextManifestId { get; set; }

  public string ContextManifestHash { get; set; } = string.Empty;

  public DateTimeOffset GeneratedAtUtc { get; set; }

  public long? LatencyMilliseconds { get; set; }

  public int? InputTokenCount { get; set; }

  public int? OutputTokenCount { get; set; }

  public decimal? Temperature { get; set; }

  public string ReferencedSourceIdsJson { get; set; } = "[]";

  public string StructuredPayloadJson { get; set; } = string.Empty;

  public ProposalStatus Status { get; set; }

  public ConfidenceBand ConfidenceBand { get; set; }

  public string? AbstentionReason { get; set; }

  public string? ReviewDispositionNotes { get; set; }

  public string UncertaintyCodesJson { get; set; } = "[]";

  public string WarningsJson { get; set; } = "[]";

  public string ValidationFailuresJson { get; set; } = "[]";
}
