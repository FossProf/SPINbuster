namespace SPINbuster.Domain;

public readonly record struct KnowledgeDocumentId
{
  public KnowledgeDocumentId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static KnowledgeDocumentId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct KnowledgeDocumentRevisionId
{
  public KnowledgeDocumentRevisionId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static KnowledgeDocumentRevisionId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct KnowledgeSourceId
{
  public KnowledgeSourceId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static KnowledgeSourceId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct KnowledgeRelationshipId
{
  public KnowledgeRelationshipId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static KnowledgeRelationshipId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct KnowledgeCitationId
{
  public KnowledgeCitationId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static KnowledgeCitationId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
