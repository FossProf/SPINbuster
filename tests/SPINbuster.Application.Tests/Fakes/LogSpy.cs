using Microsoft.Extensions.Logging;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class LogEntry
{
  public required EventId EventId { get; init; }
  public required LogLevel LogLevel { get; init; }
  public required string Message { get; init; }
  public Exception? Exception { get; init; }
  public IReadOnlyList<KeyValuePair<string, object?>> State { get; init; } = [];
}

internal sealed class LogSpy<T> : ILogger<T>, ILogger where T : class
{
  private readonly List<LogEntry> _entries = [];
  private readonly List<Dictionary<string, object?>> _scopes = [];

  public IReadOnlyList<LogEntry> Entries => _entries;

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter)
  {
    var message = formatter(state, exception);

    _entries.Add(new LogEntry
    {
      EventId = eventId,
      LogLevel = logLevel,
      Message = message,
      Exception = exception,
      State = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [],
    });
  }

  public bool IsEnabled(LogLevel logLevel) => true;

  public IDisposable? BeginScope<TState>(TState state) where TState : notnull
  {
    if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
    {
      var scopeDict = new Dictionary<string, object?>(StringComparer.Ordinal);
      foreach (var kvp in kvps)
      {
        scopeDict[kvp.Key] = kvp.Value;
      }

      _scopes.Add(scopeDict);
    }

    return new NoOpDisposable();
  }

  public IReadOnlyDictionary<string, object?> GetLastScope() =>
    _scopes.Count > 0 ? _scopes[^1] : new Dictionary<string, object?>();

  public IReadOnlyList<Dictionary<string, object?>> GetAllScopes() =>
    _scopes.ToArray();

  private sealed class NoOpDisposable : IDisposable
  {
    public void Dispose() { }
  }
}

internal sealed class LogSpy : ILogger
{
  private readonly List<LogEntry> _entries = [];
  private readonly List<Dictionary<string, object?>> _scopes = [];

  public IReadOnlyList<LogEntry> Entries => _entries;

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter)
  {
    var message = formatter(state, exception);

    _entries.Add(new LogEntry
    {
      EventId = eventId,
      LogLevel = logLevel,
      Message = message,
      Exception = exception,
      State = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [],
    });
  }

  public bool IsEnabled(LogLevel logLevel) => true;

  public IDisposable? BeginScope<TState>(TState state) where TState : notnull
  {
    if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
    {
      var scopeDict = new Dictionary<string, object?>(StringComparer.Ordinal);
      foreach (var kvp in kvps)
      {
        scopeDict[kvp.Key] = kvp.Value;
      }

      _scopes.Add(scopeDict);
    }

    return new NoOpDisposable();
  }

  public IReadOnlyDictionary<string, object?> GetLastScope() =>
    _scopes.Count > 0 ? _scopes[^1] : new Dictionary<string, object?>();

  public IReadOnlyList<Dictionary<string, object?>> GetAllScopes() =>
    _scopes.ToArray();

  private sealed class NoOpDisposable : IDisposable
  {
    public void Dispose() { }
  }
}
