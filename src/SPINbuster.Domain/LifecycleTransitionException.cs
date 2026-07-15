namespace SPINbuster.Domain;

public sealed class LifecycleTransitionException : DomainInvariantException
{
  public LifecycleTransitionException(string aggregateName, string currentState, string attemptedTransition)
    : base($"{aggregateName} cannot transition from {currentState} using {attemptedTransition}.")
  {
  }
}
