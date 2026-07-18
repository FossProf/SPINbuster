namespace SPINbuster.Domain;

public readonly record struct ReportId
{
  public ReportId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ReportId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct ContextManifestId
{
  public ContextManifestId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ContextManifestId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
