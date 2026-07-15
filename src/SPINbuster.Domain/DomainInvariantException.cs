namespace SPINbuster.Domain;

public class DomainInvariantException : InvalidOperationException
{
  public DomainInvariantException(string message)
    : base(message)
  {
  }
}
