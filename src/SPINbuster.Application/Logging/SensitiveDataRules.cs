namespace SPINbuster.Application.Logging;

/// <summary>
/// Documents the sensitive-data exclusion rules for operational logging.
/// These rules are enforced by convention in all ILogger calls throughout
/// the Application, Infrastructure, AI, and Documents layers.
/// </summary>
/// <remarks>
/// <para>The following data MUST NEVER appear in operational log messages:</para>
/// <list type="bullet">
///   <item>Raw evidence bytes or imported file content</item>
///   <item>Full AI prompt templates or prompt context strings</item>
///   <item>Full AI model output or structured proposal payloads</item>
///   <item>Absolute storage filesystem paths (log relative path segments or storage object IDs only)</item>
///   <item>Secrets, API keys, connection strings, or credentials</item>
///   <item>Cross-project metadata not relevant to the current operation scope</item>
/// </list>
/// <para>The following data SHOULD be logged as structured properties:</para>
/// <list type="bullet">
///   <item>ProjectId, OperationId, ApplicationUserId for correlation</item>
///   <item>Use-case or workflow name for operation classification</item>
///   <item>AttemptId, ProviderKey, ProcessorKey for execution tracking</item>
///   <item>FailureClassification for error categorization</item>
///   <item>DurationMs for performance monitoring</item>
///   <item>ContentHash (truncated) for identity verification without content exposure</item>
/// </list>
/// <para>Deferred logging adoption (not yet instrumented):</para>
/// <list type="bullet">
///   <item>ImportDocumentSourceUseCase: logs start/success/duplicate but lacks try/catch failure logging.
///     DocumentImportFailed (EventId 2003) is defined but unused.</item>
///   <item>RequestReportDraftProposalUseCase: logs AI-provider sub-lifecycle (invoke/complete/cancel/fail)
///     but lacks outer try/catch for use-case-level cancellation and failure. UseCaseFailed and
///     UseCaseCancelled are not emitted by this class.</item>
/// </list>
/// </remarks>
public static class SensitiveDataRules
{
  public const string RedactedMarker = "[REDACTED]";

  public static string TruncateHash(string? contentHash)
  {
    if (string.IsNullOrWhiteSpace(contentHash))
    {
      return "(none)";
    }

    return contentHash.Length <= 16
      ? contentHash
      : string.Concat(contentHash.AsSpan(0, 16), "...");
  }
}
