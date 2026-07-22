namespace SPINbuster.Domain;

public readonly record struct ParserRunId
{
  public ParserRunId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ParserRunId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct FragmentCandidateId
{
  public FragmentCandidateId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static FragmentCandidateId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct ParserDiagnosticId
{
  public ParserDiagnosticId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ParserDiagnosticId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
