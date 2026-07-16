namespace SPINbuster.Domain;

public enum ConfidenceBand
{
  None = 0,
  Low = 1,
  Medium = 2,
  High = 3,
}

public enum ProposalStatus
{
  Generated = 0,
  SchemaRejected = 1,
  PolicyRejected = 2,
  ReadyForReview = 3,
  HumanAccepted = 4,
  Rejected = 5,
  Abstained = 6,
  Failed = 7,
}

public sealed record AiProposalSection
{
  public AiProposalSection(string heading, string content)
  {
    Heading = DomainGuards.NotNullOrWhiteSpace(heading, nameof(heading));
    Content = DomainGuards.NotNullOrWhiteSpace(content, nameof(content));
  }

  public string Heading { get; }

  public string Content { get; }
}

public sealed record AiProposalSourceReference
{
  public AiProposalSourceReference(ContextSourceType sourceType, string sourceId)
  {
    SourceType = sourceType;
    SourceId = DomainGuards.NotNullOrWhiteSpace(sourceId, nameof(sourceId));
  }

  public ContextSourceType SourceType { get; }

  public string SourceId { get; }
}

public sealed class AiProposalPayload
{
  private readonly List<AiProposalSection> _sections = [];
  private readonly List<AiProposalSourceReference> _sourceReferences = [];
  private readonly List<string> _missingInformation = [];
  private readonly List<string> _openQuestions = [];
  private readonly List<string> _warnings = [];
  private readonly List<string> _uncertaintyCodes = [];

  public AiProposalPayload(
    IEnumerable<AiProposalSection> sections,
    string reasoningSummary,
    ConfidenceBand confidenceBand,
    IEnumerable<AiProposalSourceReference> sourceReferences,
    IEnumerable<string> missingInformation,
    IEnumerable<string> openQuestions,
    IEnumerable<string> warnings,
    IEnumerable<string> uncertaintyCodes,
    string? abstentionReason)
  {
    _sections.AddRange(CreateSections(sections, abstentionReason));
    ReasoningSummary = string.IsNullOrWhiteSpace(abstentionReason)
      ? DomainGuards.NotNullOrWhiteSpace(reasoningSummary, nameof(reasoningSummary))
      : reasoningSummary?.Trim() ?? string.Empty;
    ConfidenceBand = confidenceBand;
    _sourceReferences.AddRange(CreateSourceReferences(sourceReferences));
    _missingInformation.AddRange(MaterializeDistinctStrings(missingInformation, nameof(missingInformation)));
    _openQuestions.AddRange(MaterializeDistinctStrings(openQuestions, nameof(openQuestions)));
    _warnings.AddRange(MaterializeDistinctStrings(warnings, nameof(warnings)));
    _uncertaintyCodes.AddRange(MaterializeDistinctStrings(uncertaintyCodes, nameof(uncertaintyCodes)));
    AbstentionReason = string.IsNullOrWhiteSpace(abstentionReason) ? null : abstentionReason.Trim();

    if (AbstentionReason is not null && _sections.Count > 0)
    {
      throw new DomainInvariantException("Abstaining proposals cannot contain proposed sections.");
    }
  }

  public IReadOnlyList<AiProposalSection> Sections => _sections.AsReadOnly();

  public string ReasoningSummary { get; }

  public ConfidenceBand ConfidenceBand { get; }

  public IReadOnlyList<AiProposalSourceReference> SourceReferences => _sourceReferences.AsReadOnly();

  public IReadOnlyList<string> MissingInformation => _missingInformation.AsReadOnly();

  public IReadOnlyList<string> OpenQuestions => _openQuestions.AsReadOnly();

  public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

  public IReadOnlyList<string> UncertaintyCodes => _uncertaintyCodes.AsReadOnly();

  public string? AbstentionReason { get; }

  private static AiProposalSection[] CreateSections(IEnumerable<AiProposalSection> sections, string? abstentionReason)
  {
    var materializedSections = sections?.ToArray()
      ?? throw new DomainInvariantException($"{nameof(sections)} must be provided.");

    if (string.IsNullOrWhiteSpace(abstentionReason) && materializedSections.Length == 0)
    {
      throw new DomainInvariantException("Non-abstaining proposals must contain at least one proposed section.");
    }

    return materializedSections;
  }

  private static AiProposalSourceReference[] CreateSourceReferences(IEnumerable<AiProposalSourceReference> sourceReferences)
  {
    var materializedSourceReferences = sourceReferences?.ToArray()
      ?? throw new DomainInvariantException($"{nameof(sourceReferences)} must be provided.");
    var duplicateKeys = materializedSourceReferences
      .GroupBy(reference => $"{reference.SourceType}:{reference.SourceId}", StringComparer.Ordinal)
      .Where(group => group.Count() > 1)
      .Select(group => group.Key)
      .ToArray();
    if (duplicateKeys.Length > 0)
    {
      throw new DomainInvariantException($"Duplicate proposal source references are not allowed: {string.Join(", ", duplicateKeys)}.");
    }

    return materializedSourceReferences;
  }

  private static string[] MaterializeDistinctStrings(IEnumerable<string> values, string paramName)
  {
    return (values ?? [])
      .Select(value => DomainGuards.NotNullOrWhiteSpace(value, paramName))
      .Distinct(StringComparer.Ordinal)
      .OrderBy(value => value, StringComparer.Ordinal)
      .ToArray();
  }
}

public sealed class AiProposal
{
  public AiProposal(
    ProposalId id,
    ModelRunId modelRunId,
    ProjectId projectId,
    InspectionSessionId? inspectionSessionId,
    ReportId reportId,
    string providerId,
    string modelName,
    string modelDigest,
    string promptPackageId,
    string promptPackageVersion,
    string outputSchemaId,
    string outputSchemaVersion,
    ContextManifestId contextManifestId,
    string contextManifestHash,
    DateTimeOffset generatedAtUtc,
    long? latencyMilliseconds,
    int? inputTokenCount,
    int? outputTokenCount,
    decimal? temperature,
    IReadOnlyList<string> referencedSourceIds,
    string structuredPayloadJson)
  {
    Id = id;
    ModelRunId = modelRunId;
    ProjectId = projectId;
    InspectionSessionId = inspectionSessionId;
    ReportId = reportId;
    ProviderId = DomainGuards.NotNullOrWhiteSpace(providerId, nameof(providerId));
    ModelName = DomainGuards.NotNullOrWhiteSpace(modelName, nameof(modelName));
    ModelDigest = DomainGuards.NotNullOrWhiteSpace(modelDigest, nameof(modelDigest));
    PromptPackageId = DomainGuards.NotNullOrWhiteSpace(promptPackageId, nameof(promptPackageId));
    PromptPackageVersion = DomainGuards.NotNullOrWhiteSpace(promptPackageVersion, nameof(promptPackageVersion));
    OutputSchemaId = DomainGuards.NotNullOrWhiteSpace(outputSchemaId, nameof(outputSchemaId));
    OutputSchemaVersion = DomainGuards.NotNullOrWhiteSpace(outputSchemaVersion, nameof(outputSchemaVersion));
    ContextManifestId = contextManifestId;
    ContextManifestHash = DomainGuards.NotNullOrWhiteSpace(contextManifestHash, nameof(contextManifestHash));
    GeneratedAtUtc = DomainGuards.NotDefault(generatedAtUtc, nameof(generatedAtUtc));
    LatencyMilliseconds = latencyMilliseconds;
    InputTokenCount = inputTokenCount;
    OutputTokenCount = outputTokenCount;
    Temperature = temperature;
    ReferencedSourceIds = referencedSourceIds
      .Select(sourceId => DomainGuards.NotNullOrWhiteSpace(sourceId, nameof(referencedSourceIds)))
      .Distinct(StringComparer.Ordinal)
      .OrderBy(sourceId => sourceId, StringComparer.Ordinal)
      .ToArray();
    StructuredPayloadJson = DomainGuards.NotNullOrWhiteSpace(structuredPayloadJson, nameof(structuredPayloadJson));
    Status = ProposalStatus.Generated;
  }

  public ProposalId Id { get; }

  public ModelRunId ModelRunId { get; }

  public ProjectId ProjectId { get; }

  public InspectionSessionId? InspectionSessionId { get; }

  public ReportId ReportId { get; }

  public string ProviderId { get; }

  public string ModelName { get; }

  public string ModelDigest { get; }

  public string PromptPackageId { get; }

  public string PromptPackageVersion { get; }

  public string OutputSchemaId { get; }

  public string OutputSchemaVersion { get; }

  public ContextManifestId ContextManifestId { get; }

  public string ContextManifestHash { get; }

  public DateTimeOffset GeneratedAtUtc { get; }

  public long? LatencyMilliseconds { get; }

  public int? InputTokenCount { get; }

  public int? OutputTokenCount { get; }

  public decimal? Temperature { get; }

  public IReadOnlyList<string> ReferencedSourceIds { get; }

  public string StructuredPayloadJson { get; }

  // This hash covers the persisted proposal payload representation, not the raw provider response.
  public string StructuredPayloadHash => ComputeHash(StructuredPayloadJson);

  public ProposalStatus Status { get; private set; }

  public ConfidenceBand ConfidenceBand { get; private set; }

  public string? AbstentionReason { get; private set; }

  public string? ReviewDispositionNotes { get; private set; }

  public IReadOnlyList<string> UncertaintyCodes { get; private set; } = Array.Empty<string>();

  public IReadOnlyList<string> Warnings { get; private set; } = Array.Empty<string>();

  public IReadOnlyList<string> ValidationFailures { get; private set; } = Array.Empty<string>();

  internal static AiProposal Rehydrate(
    ProposalId id,
    ModelRunId modelRunId,
    ProjectId projectId,
    InspectionSessionId? inspectionSessionId,
    ReportId reportId,
    string providerId,
    string modelName,
    string modelDigest,
    string promptPackageId,
    string promptPackageVersion,
    string outputSchemaId,
    string outputSchemaVersion,
    ContextManifestId contextManifestId,
    string contextManifestHash,
    DateTimeOffset generatedAtUtc,
    long? latencyMilliseconds,
    int? inputTokenCount,
    int? outputTokenCount,
    decimal? temperature,
    IReadOnlyList<string> referencedSourceIds,
    string structuredPayloadJson,
    ProposalStatus status,
    ConfidenceBand confidenceBand,
    string? abstentionReason,
    string? reviewDispositionNotes,
    IReadOnlyList<string> uncertaintyCodes,
    IReadOnlyList<string> warnings,
    IReadOnlyList<string> validationFailures)
  {
    var proposal = new AiProposal(
      id,
      modelRunId,
      projectId,
      inspectionSessionId,
      reportId,
      providerId,
      modelName,
      modelDigest,
      promptPackageId,
      promptPackageVersion,
      outputSchemaId,
      outputSchemaVersion,
      contextManifestId,
      contextManifestHash,
      generatedAtUtc,
      latencyMilliseconds,
      inputTokenCount,
      outputTokenCount,
      temperature,
      referencedSourceIds,
      structuredPayloadJson)
    {
      Status = status,
      ConfidenceBand = confidenceBand,
      AbstentionReason = string.IsNullOrWhiteSpace(abstentionReason) ? null : abstentionReason.Trim(),
      ReviewDispositionNotes = string.IsNullOrWhiteSpace(reviewDispositionNotes) ? null : reviewDispositionNotes.Trim(),
      UncertaintyCodes = uncertaintyCodes
        .Select(code => DomainGuards.NotNullOrWhiteSpace(code, nameof(uncertaintyCodes)))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(code => code, StringComparer.Ordinal)
        .ToArray(),
      Warnings = warnings
        .Select(code => DomainGuards.NotNullOrWhiteSpace(code, nameof(warnings)))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(code => code, StringComparer.Ordinal)
        .ToArray(),
      ValidationFailures = validationFailures
        .Select(code => DomainGuards.NotNullOrWhiteSpace(code, nameof(validationFailures)))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(code => code, StringComparer.Ordinal)
        .ToArray(),
    };

    return proposal;
  }

  public void MarkSchemaRejected(
    ConfidenceBand confidenceBand,
    IEnumerable<string> warnings,
    IEnumerable<string> uncertaintyCodes,
    IEnumerable<string> validationFailures)
  {
    TransitionFrom([ProposalStatus.Generated], ProposalStatus.SchemaRejected, nameof(MarkSchemaRejected));
    ConfidenceBand = confidenceBand;
    Warnings = NormalizeStrings(warnings, nameof(warnings));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    ValidationFailures = NormalizeValidationFailures(validationFailures);
  }

  public void MarkPolicyRejected(
    ConfidenceBand confidenceBand,
    IEnumerable<string> warnings,
    IEnumerable<string> uncertaintyCodes,
    IEnumerable<string> validationFailures)
  {
    TransitionFrom([ProposalStatus.Generated], ProposalStatus.PolicyRejected, nameof(MarkPolicyRejected));
    ConfidenceBand = confidenceBand;
    Warnings = NormalizeStrings(warnings, nameof(warnings));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    ValidationFailures = NormalizeValidationFailures(validationFailures);
  }

  public void MarkReadyForReview(
    ConfidenceBand confidenceBand,
    IEnumerable<string> warnings,
    IEnumerable<string> uncertaintyCodes)
  {
    TransitionFrom([ProposalStatus.Generated], ProposalStatus.ReadyForReview, nameof(MarkReadyForReview));
    ConfidenceBand = confidenceBand;
    Warnings = NormalizeStrings(warnings, nameof(warnings));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    ValidationFailures = Array.Empty<string>();
    AbstentionReason = null;
  }

  public void MarkAbstained(
    ConfidenceBand confidenceBand,
    IEnumerable<string> warnings,
    IEnumerable<string> uncertaintyCodes,
    string abstentionReason)
  {
    TransitionFrom([ProposalStatus.Generated], ProposalStatus.Abstained, nameof(MarkAbstained));
    ConfidenceBand = confidenceBand;
    Warnings = NormalizeStrings(warnings, nameof(warnings));
    UncertaintyCodes = NormalizeStrings(uncertaintyCodes, nameof(uncertaintyCodes));
    ValidationFailures = Array.Empty<string>();
    AbstentionReason = DomainGuards.NotNullOrWhiteSpace(abstentionReason, nameof(abstentionReason));
  }

  public void MarkFailed(IEnumerable<string> validationFailures)
  {
    TransitionFrom([ProposalStatus.Generated], ProposalStatus.Failed, nameof(MarkFailed));
    ValidationFailures = NormalizeValidationFailures(validationFailures);
  }

  public void Accept(string reviewDispositionNotes)
  {
    // Human acceptance records review intent only. It does not create or mutate authoritative reports.
    TransitionFrom([ProposalStatus.ReadyForReview], ProposalStatus.HumanAccepted, nameof(Accept));
    ReviewDispositionNotes = string.IsNullOrWhiteSpace(reviewDispositionNotes) ? null : reviewDispositionNotes.Trim();
  }

  public void Reject(string reviewDispositionNotes)
  {
    TransitionFrom([ProposalStatus.ReadyForReview], ProposalStatus.Rejected, nameof(Reject));
    ReviewDispositionNotes = string.IsNullOrWhiteSpace(reviewDispositionNotes) ? null : reviewDispositionNotes.Trim();
  }

  private static string[] NormalizeStrings(IEnumerable<string> values, string paramName)
  {
    return (values ?? [])
      .Select(value => DomainGuards.NotNullOrWhiteSpace(value, paramName))
      .Distinct(StringComparer.Ordinal)
      .OrderBy(value => value, StringComparer.Ordinal)
      .ToArray();
  }

  private static string[] NormalizeValidationFailures(IEnumerable<string> validationFailures)
  {
    var normalized = NormalizeStrings(validationFailures, nameof(validationFailures));
    if (normalized.Length == 0)
    {
      throw new DomainInvariantException("At least one validation failure must be recorded.");
    }

    return normalized;
  }

  private void TransitionFrom(
    IReadOnlyCollection<ProposalStatus> allowedStatuses,
    ProposalStatus nextStatus,
    string transitionName)
  {
    if (!allowedStatuses.Contains(Status))
    {
      throw new LifecycleTransitionException(nameof(AiProposal), Status.ToString(), transitionName);
    }

    Status = nextStatus;
  }

  private static string ComputeHash(string value)
  {
    var bytes = System.Text.Encoding.UTF8.GetBytes(value);
    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
  }
}
