namespace SPINbuster.Domain;

public enum AiProviderCapability
{
  StructuredOutput = 0,
  DeterministicFixtures = 1,
  TokenUsageMetadata = 2,
  LatencyMetadata = 3,
  ConfidenceMetadata = 4,
  TimeoutClassification = 5,
}

public enum ModelRunState
{
  Requested = 0,
  ContextBuilding = 1,
  ContextValidated = 2,
  Queued = 3,
  Running = 4,
  OutputReceived = 5,
  SchemaValidating = 6,
  PolicyValidating = 7,
  ReadyForHumanReview = 8,
  ReviewCompleted = 9,
  Abstained = 10,
  Failed = 11,
  Closed = 12,
}

public enum ModelRunFailureClassification
{
  None = 0,
  ProviderUnavailable = 1,
  Timeout = 2,
  MalformedJson = 3,
  SchemaValidationFailed = 4,
  PolicyValidationFailed = 5,
  Cancelled = 6,
  Unknown = 7,
}

public sealed class ModelRun
{
  public ModelRun(
    ModelRunId id,
    ProjectId projectId,
    InspectionSessionId? inspectionSessionId,
    ReportId reportId,
    string initiatedBy,
    ContextManifestId contextManifestId,
    string contextManifestHash,
    string providerId,
    string modelName,
    string modelDigest,
    string promptPackageId,
    string promptPackageVersion,
    string outputSchemaId,
    string outputSchemaVersion,
    string correlationId,
    string requestFingerprintHash,
    DateTimeOffset requestedAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    InspectionSessionId = inspectionSessionId;
    ReportId = reportId;
    InitiatedBy = DomainGuards.NotNullOrWhiteSpace(initiatedBy, nameof(initiatedBy));
    ContextManifestId = contextManifestId;
    ContextManifestHash = DomainGuards.NotNullOrWhiteSpace(contextManifestHash, nameof(contextManifestHash));
    ProviderId = DomainGuards.NotNullOrWhiteSpace(providerId, nameof(providerId));
    ModelName = DomainGuards.NotNullOrWhiteSpace(modelName, nameof(modelName));
    ModelDigest = DomainGuards.NotNullOrWhiteSpace(modelDigest, nameof(modelDigest));
    PromptPackageId = DomainGuards.NotNullOrWhiteSpace(promptPackageId, nameof(promptPackageId));
    PromptPackageVersion = DomainGuards.NotNullOrWhiteSpace(promptPackageVersion, nameof(promptPackageVersion));
    OutputSchemaId = DomainGuards.NotNullOrWhiteSpace(outputSchemaId, nameof(outputSchemaId));
    OutputSchemaVersion = DomainGuards.NotNullOrWhiteSpace(outputSchemaVersion, nameof(outputSchemaVersion));
    CorrelationId = DomainGuards.NotNullOrWhiteSpace(correlationId, nameof(correlationId));
    RequestFingerprintHash = DomainGuards.NotNullOrWhiteSpace(requestFingerprintHash, nameof(requestFingerprintHash));
    RequestedAtUtc = DomainGuards.NotDefault(requestedAtUtc, nameof(requestedAtUtc));
    State = ModelRunState.Requested;
    FailureClassification = ModelRunFailureClassification.None;
  }

  public ModelRunId Id { get; }

  public ProjectId ProjectId { get; }

  public InspectionSessionId? InspectionSessionId { get; }

  public ReportId ReportId { get; }

  public string InitiatedBy { get; }

  public ContextManifestId ContextManifestId { get; }

  public string ContextManifestHash { get; }

  public string ProviderId { get; }

  public string ModelName { get; }

  public string ModelDigest { get; }

  public string PromptPackageId { get; }

  public string PromptPackageVersion { get; }

  public string OutputSchemaId { get; }

  public string OutputSchemaVersion { get; }

  public string CorrelationId { get; }

  public string RequestFingerprintHash { get; }

  public DateTimeOffset RequestedAtUtc { get; }

  public ModelRunState State { get; private set; }

  public ModelRunFailureClassification FailureClassification { get; private set; }

  public string? FailureMessage { get; private set; }

  internal static ModelRun Rehydrate(
    ModelRunId id,
    ProjectId projectId,
    InspectionSessionId? inspectionSessionId,
    ReportId reportId,
    string initiatedBy,
    ContextManifestId contextManifestId,
    string contextManifestHash,
    string providerId,
    string modelName,
    string modelDigest,
    string promptPackageId,
    string promptPackageVersion,
    string outputSchemaId,
    string outputSchemaVersion,
    string correlationId,
    string requestFingerprintHash,
    DateTimeOffset requestedAtUtc,
    ModelRunState state,
    ModelRunFailureClassification failureClassification,
    string? failureMessage)
  {
    var modelRun = new ModelRun(
      id,
      projectId,
      inspectionSessionId,
      reportId,
      initiatedBy,
      contextManifestId,
      contextManifestHash,
      providerId,
      modelName,
      modelDigest,
      promptPackageId,
      promptPackageVersion,
      outputSchemaId,
      outputSchemaVersion,
      correlationId,
      requestFingerprintHash,
      requestedAtUtc)
    {
      State = state,
      FailureClassification = failureClassification,
      FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim(),
    };

    return modelRun;
  }

  public void MarkContextBuilding() => TransitionFrom([ModelRunState.Requested], ModelRunState.ContextBuilding, nameof(MarkContextBuilding));

  public void MarkContextValidated() => TransitionFrom([ModelRunState.ContextBuilding], ModelRunState.ContextValidated, nameof(MarkContextValidated));

  public void Queue() => TransitionFrom([ModelRunState.ContextValidated], ModelRunState.Queued, nameof(Queue));

  public void StartRunning() => TransitionFrom([ModelRunState.Queued], ModelRunState.Running, nameof(StartRunning));

  public void MarkOutputReceived() => TransitionFrom([ModelRunState.Running], ModelRunState.OutputReceived, nameof(MarkOutputReceived));

  public void MarkSchemaValidating() => TransitionFrom([ModelRunState.OutputReceived], ModelRunState.SchemaValidating, nameof(MarkSchemaValidating));

  public void MarkPolicyValidating() => TransitionFrom([ModelRunState.SchemaValidating], ModelRunState.PolicyValidating, nameof(MarkPolicyValidating));

  public void MarkReadyForHumanReview() => TransitionFrom([ModelRunState.PolicyValidating], ModelRunState.ReadyForHumanReview, nameof(MarkReadyForHumanReview));

  // ModelRun tracks technical execution only. Human review disposition belongs to AiProposal.
  public void MarkReviewCompleted() => TransitionFrom([ModelRunState.ReadyForHumanReview], ModelRunState.ReviewCompleted, nameof(MarkReviewCompleted));

  public void MarkAbstained() => TransitionFrom([ModelRunState.ContextValidated, ModelRunState.PolicyValidating], ModelRunState.Abstained, nameof(MarkAbstained));

  public void MarkFailed(ModelRunFailureClassification failureClassification, string failureMessage)
  {
    var allowedStates = new[]
    {
      ModelRunState.ContextValidated,
      ModelRunState.Queued,
      ModelRunState.Running,
      ModelRunState.OutputReceived,
      ModelRunState.SchemaValidating,
      ModelRunState.PolicyValidating,
    };

    TransitionFrom(allowedStates, ModelRunState.Failed, nameof(MarkFailed));
    FailureClassification = failureClassification;
    FailureMessage = DomainGuards.NotNullOrWhiteSpace(failureMessage, nameof(failureMessage));
  }

  public void Close() => TransitionFrom([ModelRunState.ReviewCompleted, ModelRunState.Abstained, ModelRunState.Failed], ModelRunState.Closed, nameof(Close));

  private void TransitionFrom(
    IReadOnlyCollection<ModelRunState> allowedStates,
    ModelRunState nextState,
    string transitionName)
  {
    if (!allowedStates.Contains(State))
    {
      throw new LifecycleTransitionException(nameof(ModelRun), State.ToString(), transitionName);
    }

    State = nextState;
  }
}

public sealed class ModelRunAttempt
{
  public ModelRunAttempt(
    ModelRunAttemptId id,
    ModelRunId modelRunId,
    int attemptNumber,
    string inputHash,
    DateTimeOffset startedAtUtc,
    DateTimeOffset? completedAtUtc,
    long? latencyMilliseconds,
    int? inputTokenCount,
    int? outputTokenCount,
    string? rawOutput,
    string? rawOutputHash,
    ModelRunFailureClassification outcomeClassification,
    string? failureMessage)
  {
    if (attemptNumber < 1)
    {
      throw new DomainInvariantException($"{nameof(attemptNumber)} must be at least 1.");
    }

    if (latencyMilliseconds.HasValue && latencyMilliseconds.Value < 0)
    {
      throw new DomainInvariantException($"{nameof(latencyMilliseconds)} cannot be negative.");
    }

    Id = id;
    ModelRunId = modelRunId;
    AttemptNumber = attemptNumber;
    InputHash = DomainGuards.NotNullOrWhiteSpace(inputHash, nameof(inputHash));
    StartedAtUtc = DomainGuards.NotDefault(startedAtUtc, nameof(startedAtUtc));
    CompletedAtUtc = completedAtUtc;
    LatencyMilliseconds = latencyMilliseconds;
    InputTokenCount = inputTokenCount;
    OutputTokenCount = outputTokenCount;
    RawOutput = string.IsNullOrWhiteSpace(rawOutput) ? null : rawOutput;
    RawOutputHash = string.IsNullOrWhiteSpace(rawOutputHash) ? null : rawOutputHash;
    OutcomeClassification = outcomeClassification;
    FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim();
  }

  public ModelRunAttemptId Id { get; }

  public ModelRunId ModelRunId { get; }

  public int AttemptNumber { get; }

  public string InputHash { get; }

  public DateTimeOffset StartedAtUtc { get; }

  public DateTimeOffset? CompletedAtUtc { get; }

  public long? LatencyMilliseconds { get; }

  public int? InputTokenCount { get; }

  public int? OutputTokenCount { get; }

  public string? RawOutput { get; }

  public string? RawOutputHash { get; }

  public ModelRunFailureClassification OutcomeClassification { get; }

  public string? FailureMessage { get; }
}
