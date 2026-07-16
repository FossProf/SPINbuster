using SPINbuster.Domain;

namespace SPINbuster.Application.Abstractions;

public interface IAiProposalPayloadValidator
{
  AiProposalValidationResult Validate(AiProposalValidationRequest request);
}

public sealed record AiProposalValidationRequest(
  string OutputSchemaId,
  string OutputSchemaVersion,
  string StructuredOutputJson,
  ContextManifest ContextManifest);

public sealed record AiProposalValidationResult(
  AiProposalValidationOutcome Outcome,
  AiProposalPayload? Payload,
  string? NormalizedPayloadJson,
  ConfidenceBand ConfidenceBand,
  IReadOnlyCollection<string> Warnings,
  IReadOnlyCollection<string> UncertaintyCodes,
  IReadOnlyCollection<string> ValidationFailures);

public enum AiProposalValidationOutcome
{
  ReadyForReview = 0,
  Abstained = 1,
  SchemaRejected = 2,
  PolicyRejected = 3,
}
