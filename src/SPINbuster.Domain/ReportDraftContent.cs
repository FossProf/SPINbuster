namespace SPINbuster.Domain;

public sealed record ReportTitle
{
  public ReportTitle(string value)
  {
    Value = DomainGuards.NotNullOrWhiteSpace(value, nameof(value));
  }

  public string Value { get; }

  public override string ToString() => Value;
}

public sealed record ReportDraftSection
{
  public ReportDraftSection(string heading, string content)
  {
    Heading = DomainGuards.NotNullOrWhiteSpace(heading, nameof(heading));
    Content = DomainGuards.NotNullOrWhiteSpace(content, nameof(content));
  }

  public string Heading { get; }

  public string Content { get; }
}
