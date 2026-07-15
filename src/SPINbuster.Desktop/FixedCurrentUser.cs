using SPINbuster.Application.Abstractions;

namespace SPINbuster.Desktop;

internal sealed class FixedCurrentUser : ICurrentUser
{
  public FixedCurrentUser(string userId)
  {
    UserId = userId;
  }

  public string UserId { get; }
}
