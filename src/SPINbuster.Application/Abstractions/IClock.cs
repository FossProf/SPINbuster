namespace SPINbuster.Application.Abstractions;

public interface IClock
{
  DateTimeOffset UtcNow { get; }
}
