using Microsoft.Extensions.Logging;

namespace SPINbuster.Desktop;

internal static partial class WorkflowLogger
{
  private static readonly Action<ILogger, string, Exception?> WorkflowStartingDelegate =
    LoggerMessage.Define<string>(
      LogLevel.Information,
      new EventId(1, nameof(WorkflowStarting)),
      "Desktop workflow starting, operation {OperationId}");

  private static readonly Action<ILogger, string, Exception?> WorkflowCompletedDelegate =
    LoggerMessage.Define<string>(
      LogLevel.Information,
      new EventId(2, nameof(WorkflowCompleted)),
      "Desktop workflow completed, operation {OperationId}");

  public static void WorkflowStarting(ILogger logger, string operationId)
    => WorkflowStartingDelegate(logger, operationId, null);

  public static void WorkflowCompleted(ILogger logger, string operationId)
    => WorkflowCompletedDelegate(logger, operationId, null);
}
