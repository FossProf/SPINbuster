namespace SPINbuster.Domain;

public sealed class EvidenceAttachment
{
  public EvidenceAttachment(
    EvidenceAttachmentId id,
    InspectionSessionId inspectionSessionId,
    string capturedBy,
    DateTimeOffset capturedAtUtc,
    RawEvidenceReference rawEvidence)
  {
    Id = id;
    InspectionSessionId = inspectionSessionId;
    CapturedBy = DomainGuards.NotNullOrWhiteSpace(capturedBy, nameof(capturedBy));
    CapturedAtUtc = DomainGuards.NotDefault(capturedAtUtc, nameof(capturedAtUtc));
    RawEvidence = rawEvidence ?? throw new DomainInvariantException($"{nameof(rawEvidence)} must be provided.");
  }

  public EvidenceAttachmentId Id { get; }

  public InspectionSessionId InspectionSessionId { get; }

  public string CapturedBy { get; }

  public DateTimeOffset CapturedAtUtc { get; }

  public RawEvidenceReference RawEvidence { get; }

  // The initial model keeps the current interpretation separate from the raw
  // evidence payload so later revisions can evolve without rewriting source
  // capture data.
  public EvidenceInterpretation? Interpretation { get; private set; }

  internal void ApplyInterpretation(EvidenceInterpretation interpretation)
  {
    if (Interpretation is not null)
    {
      throw new DomainInvariantException("Evidence interpretation is already recorded. Add an explicit revision model before allowing replacement.");
    }

    Interpretation = interpretation ?? throw new DomainInvariantException($"{nameof(interpretation)} must be provided.");
  }
}
