namespace SPINbuster.Application;

public readonly record struct ApplicationUserId
{
  public ApplicationUserId(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException("Application user ID must be provided.", nameof(value));
    }

    Value = value;
  }

  public string Value { get; }

  public override string ToString() => Value;
}

public readonly record struct OperationId
{
  public OperationId(Guid value)
  {
    if (value == Guid.Empty)
    {
      throw new ArgumentException("Operation ID must be a non-empty GUID.", nameof(value));
    }

    Value = value;
  }

  public Guid Value { get; }

  public static OperationId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
