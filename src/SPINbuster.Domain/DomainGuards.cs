namespace SPINbuster.Domain;

internal static class DomainGuards
{
  public static Guid NotEmpty(Guid value, string paramName)
  {
    if (value == Guid.Empty)
    {
      throw new DomainInvariantException($"{paramName} cannot be empty.");
    }

    return value;
  }

  public static string NotNullOrWhiteSpace(string value, string paramName)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new DomainInvariantException($"{paramName} cannot be empty.");
    }

    return value;
  }

  public static DateTimeOffset NotDefault(DateTimeOffset value, string paramName)
  {
    if (value == default)
    {
      throw new DomainInvariantException($"{paramName} must be provided.");
    }

    return value;
  }
}
