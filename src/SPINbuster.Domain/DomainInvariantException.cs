namespace SPINbuster.Domain;

public class DomainInvariantException : InvalidOperationException
{
  public DomainInvariantException(string message)
    : base(message)
  {
  }

  public DomainInvariantException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
