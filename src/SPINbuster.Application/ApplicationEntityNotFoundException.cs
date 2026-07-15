namespace SPINbuster.Application;

public sealed class ApplicationEntityNotFoundException : InvalidOperationException
{
  public ApplicationEntityNotFoundException(string entityName, string entityId)
    : base($"{entityName} '{entityId}' was not found.")
  {
  }
}
