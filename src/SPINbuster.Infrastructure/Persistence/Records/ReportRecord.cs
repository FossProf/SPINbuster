using SPINbuster.Domain;

namespace SPINbuster.Infrastructure.Persistence.Records;

internal sealed class ReportRecord
{
  public List<ReportSectionRecord> Sections { get; } = [];

  public List<ReportFieldNoteSourceRecord> FieldNoteSources { get; } = [];

  public List<ReportEvidenceSourceRecord> EvidenceSources { get; } = [];

  public List<ReportDraftOperationRecord> Operations { get; } = [];

  public ReportId Id { get; set; }

  public ProjectId ProjectId { get; set; }

  public InspectionSessionId InspectionSessionId { get; set; }

  public string Title { get; set; } = string.Empty;

  public int RevisionNumber { get; set; }

  public string CreatedBy { get; set; } = string.Empty;

  public DateTimeOffset CreatedAtUtc { get; set; }

  public ReportLifecycle Lifecycle { get; set; }

  public string? ApprovedBy { get; set; }

  public DateTimeOffset? ApprovedAtUtc { get; set; }
}
