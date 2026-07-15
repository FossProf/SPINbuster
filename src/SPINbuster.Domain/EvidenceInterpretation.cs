namespace SPINbuster.Domain;

public sealed record EvidenceInterpretation
{
  public EvidenceInterpretation(
    string summary,
    string interpretedBy,
    DateTimeOffset interpretedAtUtc)
  {
    Summary = DomainGuards.NotNullOrWhiteSpace(summary, nameof(summary));
    InterpretedBy = DomainGuards.NotNullOrWhiteSpace(interpretedBy, nameof(interpretedBy));
    InterpretedAtUtc = DomainGuards.NotDefault(interpretedAtUtc, nameof(interpretedAtUtc));
  }

  public string Summary { get; }

  public string InterpretedBy { get; }

  public DateTimeOffset InterpretedAtUtc { get; }
}
