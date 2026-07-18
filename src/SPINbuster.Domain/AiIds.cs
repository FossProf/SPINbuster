namespace SPINbuster.Domain;

public readonly record struct ModelRunId
{
  public ModelRunId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ModelRunId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct ModelRunAttemptId
{
  public ModelRunAttemptId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ModelRunAttemptId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct ProposalId
{
  public ProposalId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ProposalId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
