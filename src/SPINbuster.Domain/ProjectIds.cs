namespace SPINbuster.Domain;

public readonly record struct ProjectId
{
  public ProjectId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ProjectId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
