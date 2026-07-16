using SPINbuster.Application;
using SPINbuster.Application.Abstractions;

namespace SPINbuster.Desktop;

internal sealed class FixedCurrentUser : ICurrentUser
{
  public FixedCurrentUser(string userId)
  {
    UserId = new ApplicationUserId(userId);
  }

  public ApplicationUserId UserId { get; }
}
