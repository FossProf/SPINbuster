using SPINbuster.Application.Abstractions;

namespace SPINbuster.Desktop;

internal sealed class DeterministicClock : IClock
{
  private DateTimeOffset _nextUtcTimestamp;

  public DeterministicClock(DateTimeOffset initialTimestampUtc)
  {
    _nextUtcTimestamp = initialTimestampUtc;
  }

  public DateTimeOffset UtcNow
  {
    get
    {
      // Each workflow step gets a stable, reviewable timestamp while keeping
      // the execution deterministic for tests and future code reviews.
      var currentTimestamp = _nextUtcTimestamp;
      _nextUtcTimestamp = _nextUtcTimestamp.AddMinutes(1);
      return currentTimestamp;
    }
  }
}
