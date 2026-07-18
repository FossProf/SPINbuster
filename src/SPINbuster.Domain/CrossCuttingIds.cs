namespace SPINbuster.Domain;

public readonly record struct AuditEventId
{
  public AuditEventId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static AuditEventId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct SaveTransactionId
{
  public SaveTransactionId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static SaveTransactionId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
