using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class EvidenceAttachmentRecord
{
  public EvidenceAttachmentId Id { get; set; }

  public InspectionSessionId InspectionSessionId { get; set; }

  public string CapturedBy { get; set; } = string.Empty;

  public DateTimeOffset CapturedAtUtc { get; set; }

  public string FileName { get; set; } = string.Empty;

  public string MediaType { get; set; } = string.Empty;

  public string StorageKey { get; set; } = string.Empty;

  public string Checksum { get; set; } = string.Empty;

  public string? InterpretationSummary { get; set; }

  public string? InterpretedBy { get; set; }

  public DateTimeOffset? InterpretedAtUtc { get; set; }
}
