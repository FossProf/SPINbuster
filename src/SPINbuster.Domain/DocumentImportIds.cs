namespace SPINbuster.Domain;

public readonly record struct ImportedSourceId
{
  public ImportedSourceId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static ImportedSourceId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct DocumentImportSessionId
{
  public DocumentImportSessionId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static DocumentImportSessionId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct DocumentProcessingAttemptId
{
  public DocumentProcessingAttemptId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static DocumentProcessingAttemptId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct DocumentCandidateId
{
  public DocumentCandidateId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static DocumentCandidateId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}

public readonly record struct StorageObjectId
{
  public StorageObjectId(Guid value)
  {
    Value = DomainGuards.NotEmpty(value, nameof(value));
  }

  public Guid Value { get; }

  public static StorageObjectId New() => new(Guid.NewGuid());

  public override string ToString() => Value.ToString("D");
}
