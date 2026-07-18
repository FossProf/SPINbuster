namespace SPINbuster.Domain;

public readonly record struct InspectionSessionId
{
  public InspectionSessionId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static InspectionSessionId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct FieldNoteId
{
  public FieldNoteId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static FieldNoteId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct EvidenceAttachmentId
{
  public EvidenceAttachmentId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static EvidenceAttachmentId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
