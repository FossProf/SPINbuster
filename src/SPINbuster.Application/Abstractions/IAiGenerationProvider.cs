using SPINbuster.Domain;

namespace SPINbuster.Application.Abstractions;

public interface IAiGenerationProvider
{
  AiProviderDescriptor Describe();

  Task<AiGenerationResult> GenerateAsync(
    AiGenerationRequest request,
    CancellationToken cancellationToken = default);
}

public sealed record AiProviderDescriptor(
  string ProviderId,
  string ModelName,
  string ModelDigest,
  IReadOnlyCollection<AiProviderCapability> Capabilities);

public sealed record AiGenerationRequest(
  string CorrelationId,
  string PromptPackageId,
  string PromptPackageVersion,
  string OutputSchemaId,
  string OutputSchemaVersion,
  string PromptTemplate,
  string PromptContext,
  string ContextManifestHash,
  string InputHash,
  decimal? Temperature,
  TimeSpan? Timeout);

public sealed record AiGenerationResult(
  bool Succeeded,
  string? StructuredOutputJson,
  AiGenerationFailureClassification FailureClassification,
  string? FailureMessage,
  long? LatencyMilliseconds,
  int? InputTokenCount,
  int? OutputTokenCount,
  decimal? Temperature,
  DateTimeOffset StartedAtUtc,
  DateTimeOffset CompletedAtUtc);

public enum AiGenerationFailureClassification
{
  None = 0,
  ProviderUnavailable = 1,
  Timeout = 2,
  MalformedJson = 3,
  Cancelled = 4,
  Unknown = 5,
}
