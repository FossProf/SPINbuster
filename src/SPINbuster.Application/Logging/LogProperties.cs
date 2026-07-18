namespace SPINbuster.Application.Logging;

/// <summary>
/// Structured property names used across all operational log messages.
/// Consumer code and log sinks should reference these constants to ensure
/// consistent property naming and avoid silent schema drift.
/// </summary>
public static class LogProperties
{
  public const string ProjectId = "ProjectId";
  public const string OperationId = "OperationId";
  public const string ApplicationUserId = "ApplicationUserId";
  public const string UseCase = "UseCase";
  public const string AttemptId = "AttemptId";
  public const string ProviderKey = "ProviderKey";
  public const string FailureClassification = "FailureClassification";
  public const string DurationMs = "DurationMs";
  public const string ImportSessionId = "ImportSessionId";
  public const string ImportedSourceId = "ImportedSourceId";
  public const string ModelRunId = "ModelRunId";
  public const string ProposalId = "ProposalId";
  public const string ReportId = "ReportId";
  public const string InspectionSessionId = "InspectionSessionId";
  public const string CorrelationId = "CorrelationId";
  public const string ContentHash = "ContentHash";
  public const string FileName = "FileName";
  public const string DeclaredMediaType = "DeclaredMediaType";
  public const string ProcessorKey = "ProcessorKey";
  public const string CandidateCount = "CandidateCount";
}
