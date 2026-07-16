using System.Text.Json;
using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;

namespace SPINbuster.Application.Internal;

internal sealed class JsonAiProposalPayloadValidator : IAiProposalPayloadValidator
{
  private static readonly HashSet<string> ProhibitedAuthorityTerms = new(StringComparer.OrdinalIgnoreCase)
  {
    "approved",
    "approval granted",
    "issued",
    "final report",
    "authoritative record",
  };

  public AiProposalValidationResult Validate(AiProposalValidationRequest request)
  {
    var failures = new List<string>();

    JsonDocument document;
    try
    {
      document = JsonDocument.Parse(request.StructuredOutputJson);
    }
    catch (JsonException)
    {
      return new AiProposalValidationResult(
        AiProposalValidationOutcome.SchemaRejected,
        null,
        null,
        ConfidenceBand.None,
        [],
        [],
        ["invalid-json"]);
    }

    using (document)
    {
      var root = document.RootElement;
      var normalizedPayloadJson = CanonicalJson.Canonicalize(root);

      if (root.ValueKind != JsonValueKind.Object)
      {
        return Rejected("root-object-required");
      }

      if (!TryGetRequiredArray(root, "sections", out var sectionsElement)
        || !TryGetRequiredString(root, "reasoningSummary", out var reasoningSummary)
        || !TryGetRequiredString(root, "confidenceBand", out var confidenceBandText)
        || !TryGetRequiredArray(root, "sourceReferences", out var sourceReferencesElement)
        || !TryGetRequiredArray(root, "missingInformation", out var missingInformationElement)
        || !TryGetRequiredArray(root, "openQuestions", out var openQuestionsElement)
        || !TryGetRequiredArray(root, "warnings", out var warningsElement)
        || !TryGetRequiredArray(root, "uncertaintyCodes", out var uncertaintyCodesElement))
      {
        return Rejected("required-properties-missing");
      }

      if (!Enum.TryParse<ConfidenceBand>(confidenceBandText, true, out var confidenceBand))
      {
        return Rejected("invalid-confidence-band");
      }

      var warnings = ExtractStringArray(warningsElement, "warnings", failures);
      var uncertaintyCodes = ExtractStringArray(uncertaintyCodesElement, "uncertaintyCodes", failures);
      var missingInformation = ExtractStringArray(missingInformationElement, "missingInformation", failures);
      var openQuestions = ExtractStringArray(openQuestionsElement, "openQuestions", failures);
      var abstentionReason = TryGetOptionalString(root, "abstentionReason");

      if (confidenceBand == ConfidenceBand.High && uncertaintyCodes.Count > 0)
      {
        failures.Add("high-confidence-cannot-carry-uncertainty-codes");
      }

      if (!string.IsNullOrWhiteSpace(abstentionReason) && confidenceBand != ConfidenceBand.None)
      {
        failures.Add("abstention-requires-confidence-none");
      }

      var sections = new List<AiProposalSection>();
      foreach (var sectionElement in sectionsElement.EnumerateArray())
      {
        if (sectionElement.ValueKind != JsonValueKind.Object
          || !TryGetRequiredString(sectionElement, "heading", out var heading)
          || !TryGetRequiredString(sectionElement, "content", out var content))
        {
          failures.Add("invalid-section-shape");
          continue;
        }

        if (ContainsProhibitedAuthorityLanguage(heading) || ContainsProhibitedAuthorityLanguage(content))
        {
          failures.Add("prohibited-authority-language");
        }

        sections.Add(new AiProposalSection(heading, content));
      }

      var manifestSources = request.ContextManifest.Entries
        .ToDictionary(entry => $"{entry.SourceType}:{entry.SourceId}", StringComparer.Ordinal);
      var sourceReferences = new List<AiProposalSourceReference>();
      foreach (var sourceReferenceElement in sourceReferencesElement.EnumerateArray())
      {
        if (sourceReferenceElement.ValueKind != JsonValueKind.Object
          || !TryGetRequiredString(sourceReferenceElement, "sourceType", out var sourceTypeText)
          || !TryGetRequiredString(sourceReferenceElement, "sourceId", out var sourceId))
        {
          failures.Add("invalid-source-reference-shape");
          continue;
        }

        if (!Enum.TryParse<ContextSourceType>(sourceTypeText, true, out var sourceType))
        {
          failures.Add("unsupported-source-type");
          continue;
        }

        var manifestKey = $"{sourceType}:{sourceId}";
        if (!manifestSources.ContainsKey(manifestKey))
        {
          failures.Add("fabricated-or-out-of-scope-source-reference");
        }

        sourceReferences.Add(new AiProposalSourceReference(sourceType, sourceId));
      }

      if (ContainsProhibitedAuthorityLanguage(reasoningSummary))
      {
        failures.Add("prohibited-authority-language");
      }

      if (failures.Count > 0)
      {
        return Rejected(failures.ToArray());
      }

      try
      {
        var payload = new AiProposalPayload(
          sections,
          reasoningSummary,
          confidenceBand,
          sourceReferences,
          missingInformation,
          openQuestions,
          warnings,
          uncertaintyCodes,
          abstentionReason);

        var outcome = payload.AbstentionReason is null
          ? AiProposalValidationOutcome.ReadyForReview
          : AiProposalValidationOutcome.Abstained;

        return new AiProposalValidationResult(
          outcome,
          payload,
          normalizedPayloadJson,
          confidenceBand,
          warnings,
          uncertaintyCodes,
          []);
      }
      catch (DomainInvariantException exception)
      {
        return Rejected(NormalizeFailureCode(exception.Message));
      }
    }
  }

  private static AiProposalValidationResult Rejected(params string[] failures)
  {
    return new AiProposalValidationResult(
      failures.Any(failure => failure.Contains("authority", StringComparison.OrdinalIgnoreCase))
        ? AiProposalValidationOutcome.PolicyRejected
        : AiProposalValidationOutcome.SchemaRejected,
      null,
      null,
      ConfidenceBand.None,
      [],
      [],
      failures.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
  }

  private static List<string> ExtractStringArray(
    JsonElement element,
    string propertyName,
    List<string> failures)
  {
    if (element.ValueKind != JsonValueKind.Array)
    {
      failures.Add($"invalid-{propertyName}-array");
      return [];
    }

    var values = new List<string>();
    foreach (var item in element.EnumerateArray())
    {
      if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
      {
        failures.Add($"invalid-{propertyName}-value");
        continue;
      }

      values.Add(item.GetString()!.Trim());
    }

    return values.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
  }

  private static bool TryGetRequiredArray(JsonElement element, string propertyName, out JsonElement value)
  {
    if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Array)
    {
      return true;
    }

    value = default;
    return false;
  }

  private static bool TryGetRequiredString(JsonElement element, string propertyName, out string value)
  {
    if (element.TryGetProperty(propertyName, out var propertyValue)
      && propertyValue.ValueKind == JsonValueKind.String
      && !string.IsNullOrWhiteSpace(propertyValue.GetString()))
    {
      value = propertyValue.GetString()!.Trim();
      return true;
    }

    value = string.Empty;
    return false;
  }

  private static string? TryGetOptionalString(JsonElement element, string propertyName)
  {
    return element.TryGetProperty(propertyName, out var propertyValue)
      && propertyValue.ValueKind == JsonValueKind.String
      && !string.IsNullOrWhiteSpace(propertyValue.GetString())
        ? propertyValue.GetString()!.Trim()
        : null;
  }

  private static bool ContainsProhibitedAuthorityLanguage(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    return ProhibitedAuthorityTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
  }

  private static string NormalizeFailureCode(string message)
  {
    return message
      .ToLowerInvariant()
      .Replace(' ', '-')
      .Replace(".", string.Empty, StringComparison.Ordinal)
      .Replace(",", string.Empty, StringComparison.Ordinal);
  }
}
