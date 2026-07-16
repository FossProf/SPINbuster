namespace SPINbuster.Application.Abstractions;

public interface ICurrentUser
{
  ApplicationUserId UserId { get; }
}
