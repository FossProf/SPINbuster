using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class InspectionSessionRecord
{
  public InspectionSessionId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public string Name { get; set; } = string.Empty;

  public string CreatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public InspectionSessionLifecycle Lifecycle { get; set; }

  public DateTimeOffset? StartedAtUtc { get; set; }

  public DateTimeOffset? CompletedAtUtc { get; set; }

  public List<FieldNoteRecord> FieldNotes { get; } = [];

  public List<EvidenceAttachmentRecord> EvidenceAttachments { get; } = [];
}
