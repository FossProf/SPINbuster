using System.Globalization;
using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;

namespace SPINbuster.AI;

public sealed class DeterministicAiProvider : IAiGenerationProvider
{
  private readonly DeterministicAiProviderOptions _options;

  public DeterministicAiProvider(DeterministicAiProviderOptions options)
  {
    _options = options;
  }

  public AiProviderDescriptor Describe()
  {
    return new AiProviderDescriptor(
      "tier0-deterministic",
      "deterministic-fixture",
      "sha256:deterministic-fixture-v1",
      [
        AiProviderCapability.StructuredOutput,
        AiProviderCapability.DeterministicFixtures,
        AiProviderCapability.TokenUsageMetadata,
        AiProviderCapability.LatencyMetadata,
        AiProviderCapability.ConfidenceMetadata,
        AiProviderCapability.TimeoutClassification,
      ]);
  }

  public Task<AiGenerationResult> GenerateAsync(
    AiGenerationRequest request,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var startedAtUtc = DateTimeOffset.Parse("2026-07-15T16:00:00Z", CultureInfo.InvariantCulture);
    var completedAtUtc = startedAtUtc.AddMilliseconds(42);

    AiGenerationResult result = _options.Scenario switch
    {
      DeterministicAiScenario.Success => new AiGenerationResult(
        true,
        """
{
  "sections": [
    { "heading": "Summary", "content": "This proposal summarizes the authoritative field material." },
    { "heading": "Observations", "content": "Observed conditions are derived only from cited sources." }
  ],
  "reasoningSummary": "The proposal preserves the report draft boundary and references only governed sources.",
  "confidenceBand": "Medium",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "FIELD_NOTE_ID" },
    { "sourceType": "EvidenceAttachment", "sourceId": "EVIDENCE_ATTACHMENT_ID" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": []
}
""",
        AiGenerationFailureClassification.None,
        null,
        42,
        128,
        96,
        request.Temperature,
        startedAtUtc,
        completedAtUtc),
      DeterministicAiScenario.Abstention => new AiGenerationResult(
        true,
        """
{
  "sections": [],
  "reasoningSummary": "The governed context is insufficient to produce a safe grounded proposal.",
  "confidenceBand": "None",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "FIELD_NOTE_ID" }
  ],
  "missingInformation": ["additional-governed-context-required"],
  "openQuestions": [],
  "warnings": ["insufficient-context"],
  "uncertaintyCodes": [],
  "abstentionReason": "The governed context is insufficient for a safe proposal."
}
""",
        AiGenerationFailureClassification.None,
        null,
        42,
        64,
        44,
        request.Temperature,
        startedAtUtc,
        completedAtUtc),
      DeterministicAiScenario.Timeout => new AiGenerationResult(
        false,
        null,
        AiGenerationFailureClassification.Timeout,
        "Deterministic timeout classification.",
        request.Timeout.HasValue ? (long)request.Timeout.Value.TotalMilliseconds : 30000L,
        128,
        null,
        request.Temperature,
        startedAtUtc,
        completedAtUtc),
      DeterministicAiScenario.ProviderUnavailable => new AiGenerationResult(
        false,
        null,
        AiGenerationFailureClassification.ProviderUnavailable,
        "Deterministic provider unavailable classification.",
        null,
        null,
        null,
        request.Temperature,
        startedAtUtc,
        completedAtUtc),
      DeterministicAiScenario.MalformedJson => new AiGenerationResult(
        true,
        "{ \"sections\": [",
        AiGenerationFailureClassification.None,
        null,
        42,
        96,
        12,
        request.Temperature,
        startedAtUtc,
        completedAtUtc),
      DeterministicAiScenario.SchemaInvalid => new AiGenerationResult(
        true,
        """
{
  "sections": [
    { "heading": "", "content": "" }
  ],
  "reasoningSummary": "",
  "confidenceBand": "High",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "fabricated-field-note" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": ["scope-mismatch"]
}
""",
        AiGenerationFailureClassification.None,
        null,
        42,
        96,
        28,
        request.Temperature,
        startedAtUtc,
        completedAtUtc),
      _ => new AiGenerationResult(
        false,
        null,
        AiGenerationFailureClassification.Unknown,
        "Unrecognized deterministic scenario.",
        null,
        null,
        null,
        request.Temperature,
        startedAtUtc,
        completedAtUtc),
    };

    if (result.StructuredOutputJson is not null)
    {
      result = result with
      {
        StructuredOutputJson = result.StructuredOutputJson
          .Replace("FIELD_NOTE_ID", ExtractFirstDelimitedId(request.PromptContext, "FieldNote "), StringComparison.Ordinal)
          .Replace("EVIDENCE_ATTACHMENT_ID", ExtractFirstDelimitedId(request.PromptContext, "Evidence "), StringComparison.Ordinal),
      };
    }

    return Task.FromResult(result);
  }

  private static string ExtractFirstDelimitedId(string context, string marker)
  {
    var markerIndex = context.IndexOf(marker, StringComparison.Ordinal);
    if (markerIndex < 0)
    {
      return "missing-id";
    }

    var startIndex = markerIndex + marker.Length;
    var endIndex = context.IndexOf(':', startIndex);
    return endIndex < 0
      ? "missing-id"
      : context[startIndex..endIndex].Trim();
  }
}
