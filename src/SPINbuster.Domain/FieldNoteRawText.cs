namespace SPINbuster.Domain;

public sealed record FieldNoteRawText
{
  public FieldNoteRawText(string value)
  {
    Value = DomainGuards.NotNullOrWhiteSpace(value, nameof(value));
  }

  public string Value { get; }
}
