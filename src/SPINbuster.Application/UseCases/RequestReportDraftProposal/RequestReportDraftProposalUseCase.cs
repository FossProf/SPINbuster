using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestReportDraftProposal;

public sealed class RequestReportDraftProposalUseCase
  : ICommandHandler<RequestReportDraftProposalCommand, RequestReportDraftProposalResult>
{
  private const string AssignedModelRole = "report-draft-proposer";
  private const string ContextPolicyVersion = "report-draft-context-policy/1.0";
  private const string OutputSchemaId = "report-draft-proposal";
  private const string OutputSchemaVersion = "1.0.0";

  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IAiGenerationProvider _generationProvider;
  private readonly IAiPromptPackageRegistry _promptPackageRegistry;
  private readonly IAiProposalPayloadValidator _proposalPayloadValidator;
  private readonly IAiProposalRepository _proposalRepository;
  private readonly IContextManifestRepository _contextManifestRepository;
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IModelRunRepository _modelRunRepository;
  private readonly IProjectRepository _projectRepository;
  private readonly IReportRepository _reportRepository;
  private readonly IUnitOfWork _unitOfWork;

  public RequestReportDraftProposalUseCase(
    IProjectRepository projectRepository,
    IInspectionSessionRepository inspectionSessionRepository,
    IReportRepository reportRepository,
    IContextManifestRepository contextManifestRepository,
    IModelRunRepository modelRunRepository,
    IAiProposalRepository proposalRepository,
    IAiGenerationProvider generationProvider,
    IAiPromptPackageRegistry promptPackageRegistry,
    IAiProposalPayloadValidator proposalPayloadValidator,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _projectRepository = projectRepository;
    _inspectionSessionRepository = inspectionSessionRepository;
    _reportRepository = reportRepository;
    _contextManifestRepository = contextManifestRepository;
    _modelRunRepository = modelRunRepository;
    _proposalRepository = proposalRepository;
    _generationProvider = generationProvider;
    _promptPackageRegistry = promptPackageRegistry;
    _proposalPayloadValidator = proposalPayloadValidator;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<RequestReportDraftProposalResult> HandleAsync(
    RequestReportDraftProposalCommand command,
    CancellationToken cancellationToken = default)
  {
    var correlationId = command.OperationId.ToString();
    var requestFingerprintHash = ComputeHash(
      $"{command.ReportId}|{command.PromptPackageId}|{command.PromptPackageVersion}|{(command.Temperature.HasValue ? command.Temperature.Value.ToString(CultureInfo.InvariantCulture) : "none")}");

    var existingRun = await _modelRunRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
    if (existingRun is not null)
    {
      if (!string.Equals(existingRun.RequestFingerprintHash, requestFingerprintHash, StringComparison.Ordinal))
      {
        throw new InvalidOperationException(
          $"Operation ID {command.OperationId} was already used for a different AI draft-proposal request.");
      }

      var existingProposal = await _proposalRepository.GetByModelRunIdAsync(existingRun.Id, cancellationToken);
      return new RequestReportDraftProposalResult(
        existingProposal?.Id,
        existingRun.Id,
        existingProposal?.Status,
        existingRun.State,
        existingRun.FailureClassification,
        existingRun.FailureMessage,
        true);
    }

    var report = await _reportRepository.GetByIdAsync(command.ReportId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Report), command.ReportId.ToString());
    var project = await _projectRepository.GetByIdAsync(report.ProjectId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Project), report.ProjectId.ToString());
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(report.InspectionSessionId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(InspectionSession), report.InspectionSessionId.ToString());

    var promptPackage = await _promptPackageRegistry.GetByIdAsync(
      command.PromptPackageId,
      command.PromptPackageVersion,
      cancellationToken)
      ?? throw new InvalidOperationException(
        $"Prompt package {command.PromptPackageId}@{command.PromptPackageVersion} was not found.");
    if (promptPackage.Status != PromptPackageStatus.Approved)
    {
      throw new InvalidOperationException(
        $"Prompt package {promptPackage.PackageId}@{promptPackage.SemanticVersion} is not approved.");
    }

    if (!string.Equals(promptPackage.AssignedModelRole, AssignedModelRole, StringComparison.Ordinal))
    {
      throw new InvalidOperationException(
        $"Prompt package {promptPackage.PackageId}@{promptPackage.SemanticVersion} is not approved for {AssignedModelRole}.");
    }

    if (!string.Equals(promptPackage.RequiredContextPolicyVersion, ContextPolicyVersion, StringComparison.Ordinal)
      || !string.Equals(promptPackage.RequiredOutputSchemaId, OutputSchemaId, StringComparison.Ordinal)
      || !string.Equals(promptPackage.RequiredOutputSchemaVersion, OutputSchemaVersion, StringComparison.Ordinal))
    {
      throw new InvalidOperationException(
        $"Prompt package {promptPackage.PackageId}@{promptPackage.SemanticVersion} does not match the required context-policy or schema contract.");
    }

    var providerDescriptor = _generationProvider.Describe();
    foreach (var requiredCapability in promptPackage.AllowedProviderCapabilities)
    {
      if (!providerDescriptor.Capabilities.Contains(requiredCapability))
      {
        throw new InvalidOperationException(
          $"Provider {providerDescriptor.ProviderId} does not satisfy prompt package capability {requiredCapability}.");
      }
    }

    var contextAssembly = ReportProposalContextAssembly.Create(
      project,
      inspectionSession,
      report,
      ContextPolicyVersion,
      _clock.UtcNow);

    var modelRun = new ModelRun(
      ModelRunId.New(),
      project.Id,
      inspectionSession.Id,
      report.Id,
      _currentUser.UserId.Value,
      contextAssembly.ContextManifest.Id,
      contextAssembly.ContextManifest.ManifestHash,
      providerDescriptor.ProviderId,
      providerDescriptor.ModelName,
      providerDescriptor.ModelDigest,
      promptPackage.PackageId,
      promptPackage.SemanticVersion,
      OutputSchemaId,
      OutputSchemaVersion,
      correlationId,
      requestFingerprintHash,
      _clock.UtcNow);

    modelRun.MarkContextBuilding();
    if (contextAssembly.ContextManifest.Status == ContextManifestStatus.Incomplete)
    {
      modelRun.MarkContextValidated();
      modelRun.MarkAbstained();

      var abstentionProposal = CreateProposal(
        modelRun,
        command,
        providerDescriptor,
        structuredPayloadJson: """
{
  "sections": [],
  "reasoningSummary": "",
  "confidenceBand": "None",
  "sourceReferences": [],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": ["context-incomplete"],
  "uncertaintyCodes": [],
  "abstentionReason": "Required governed context is incomplete."
}
""",
        referencedSourceIds: [],
        latencyMilliseconds: null,
        inputTokenCount: null,
        outputTokenCount: null);
      abstentionProposal.MarkAbstained(
        ConfidenceBand.None,
        ["context-incomplete"],
        contextAssembly.ContextManifest.IncompleteReasons,
        "Required governed context is incomplete.");

      await _contextManifestRepository.AddAsync(contextAssembly.ContextManifest, cancellationToken);
      await _modelRunRepository.AddAsync(modelRun, cancellationToken);
      await _proposalRepository.AddAsync(abstentionProposal, cancellationToken);
      StageAudit(AiAuditEventFactory.ContextManifestCreated(contextAssembly.ContextManifest, _currentUser.UserId.Value, _clock.UtcNow));
      StageAudit(AiAuditEventFactory.ModelRunCompleted(modelRun, "AI request abstained because governed context was incomplete.", _clock.UtcNow));
      StageAudit(AiAuditEventFactory.ProposalRecorded(abstentionProposal, _currentUser.UserId.Value, _clock.UtcNow));
      await _unitOfWork.CommitAsync(cancellationToken);

      return new RequestReportDraftProposalResult(
        abstentionProposal.Id,
        modelRun.Id,
        abstentionProposal.Status,
        modelRun.State,
        modelRun.FailureClassification,
        modelRun.FailureMessage,
        false);
    }

    modelRun.MarkContextValidated();
    modelRun.Queue();
    modelRun.StartRunning();

    await _contextManifestRepository.AddAsync(contextAssembly.ContextManifest, cancellationToken);
    await _modelRunRepository.AddAsync(modelRun, cancellationToken);
    StageAudit(AiAuditEventFactory.ContextManifestCreated(contextAssembly.ContextManifest, _currentUser.UserId.Value, _clock.UtcNow));
    await _unitOfWork.CommitAsync(cancellationToken);

    var generationRequest = new AiGenerationRequest(
      correlationId,
      promptPackage.PackageId,
      promptPackage.SemanticVersion,
      OutputSchemaId,
      OutputSchemaVersion,
      promptPackage.PromptTemplate,
      contextAssembly.GovernedPromptContext,
      contextAssembly.ContextManifest.ManifestHash,
      contextAssembly.InputHash,
      command.Temperature,
      TimeSpan.FromSeconds(30));

    AiGenerationResult generationResult;
    try
    {
      // Provider invocation must happen outside the database commit that records the requested run.
      generationResult = await _generationProvider.GenerateAsync(generationRequest, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      modelRun.MarkFailed(ModelRunFailureClassification.Cancelled, "AI generation was cancelled.");
      await _modelRunRepository.UpdateAsync(modelRun, CancellationToken.None);
      StageAudit(AiAuditEventFactory.ModelRunCompleted(modelRun, "AI generation was cancelled.", _clock.UtcNow));
      await _unitOfWork.CommitAsync(CancellationToken.None);
      throw;
    }
    catch (Exception exception)
    {
      modelRun.MarkFailed(ModelRunFailureClassification.Unknown, $"AI generation threw an exception: {exception.Message}");
      await _modelRunRepository.UpdateAsync(modelRun, CancellationToken.None);
      StageAudit(AiAuditEventFactory.ModelRunCompleted(modelRun, modelRun.FailureMessage!, _clock.UtcNow));
      await _unitOfWork.CommitAsync(CancellationToken.None);
      throw;
    }

    var attempt = new ModelRunAttempt(
      ModelRunAttemptId.New(),
      modelRun.Id,
      1,
      contextAssembly.InputHash,
      generationResult.StartedAtUtc,
      generationResult.CompletedAtUtc,
      generationResult.LatencyMilliseconds,
      generationResult.InputTokenCount,
      generationResult.OutputTokenCount,
      generationResult.StructuredOutputJson,
      generationResult.StructuredOutputJson is null ? null : ComputeHash(generationResult.StructuredOutputJson),
      MapGenerationOutcome(generationResult.FailureClassification),
      generationResult.FailureMessage);

    await _modelRunRepository.AddAttemptAsync(attempt, cancellationToken);

    if (!generationResult.Succeeded || string.IsNullOrWhiteSpace(generationResult.StructuredOutputJson))
    {
      modelRun.MarkFailed(MapGenerationOutcome(generationResult.FailureClassification), generationResult.FailureMessage ?? "AI generation failed.");
      await _modelRunRepository.UpdateAsync(modelRun, cancellationToken);
      StageAudit(AiAuditEventFactory.ModelRunCompleted(modelRun, generationResult.FailureMessage ?? "AI generation failed.", _clock.UtcNow));
      await _unitOfWork.CommitAsync(cancellationToken);

      return new RequestReportDraftProposalResult(
        null,
        modelRun.Id,
        null,
        modelRun.State,
        modelRun.FailureClassification,
        modelRun.FailureMessage,
        false);
    }

    modelRun.MarkOutputReceived();
    modelRun.MarkSchemaValidating();

    var validation = _proposalPayloadValidator.Validate(new AiProposalValidationRequest(
      OutputSchemaId,
      OutputSchemaVersion,
      generationResult.StructuredOutputJson,
      contextAssembly.ContextManifest));

    var proposal = CreateProposal(
      modelRun,
      command,
      providerDescriptor,
      validation.NormalizedPayloadJson ?? generationResult.StructuredOutputJson,
      validation.Payload?.SourceReferences.Select(reference => reference.SourceId).ToArray()
        ?? [],
      generationResult.LatencyMilliseconds,
      generationResult.InputTokenCount,
      generationResult.OutputTokenCount);

    switch (validation.Outcome)
    {
      case AiProposalValidationOutcome.ReadyForReview:
        modelRun.MarkPolicyValidating();
        modelRun.MarkReadyForHumanReview();
        proposal.MarkReadyForReview(validation.ConfidenceBand, validation.Warnings, validation.UncertaintyCodes);
        break;

      case AiProposalValidationOutcome.Abstained:
        modelRun.MarkPolicyValidating();
        modelRun.MarkAbstained();
        proposal.MarkAbstained(
          validation.ConfidenceBand,
          validation.Warnings,
          validation.UncertaintyCodes,
          validation.Payload!.AbstentionReason!);
        break;

      case AiProposalValidationOutcome.PolicyRejected:
        modelRun.MarkPolicyValidating();
        modelRun.MarkFailed(ModelRunFailureClassification.PolicyValidationFailed, string.Join(", ", validation.ValidationFailures));
        proposal.MarkPolicyRejected(validation.ConfidenceBand, validation.Warnings, validation.UncertaintyCodes, validation.ValidationFailures);
        break;

      default:
        modelRun.MarkFailed(ModelRunFailureClassification.SchemaValidationFailed, string.Join(", ", validation.ValidationFailures));
        proposal.MarkSchemaRejected(validation.ConfidenceBand, validation.Warnings, validation.UncertaintyCodes, validation.ValidationFailures);
        break;
    }

    await _modelRunRepository.UpdateAsync(modelRun, cancellationToken);
    await _proposalRepository.AddAsync(proposal, cancellationToken);
    StageAudit(AiAuditEventFactory.ModelRunCompleted(modelRun, DescribeOutcome(proposal), _clock.UtcNow));
    StageAudit(AiAuditEventFactory.ProposalRecorded(proposal, _currentUser.UserId.Value, _clock.UtcNow));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new RequestReportDraftProposalResult(
      proposal.Id,
      modelRun.Id,
      proposal.Status,
      modelRun.State,
      modelRun.FailureClassification,
      modelRun.FailureMessage,
      false);
  }

  private AiProposal CreateProposal(
    ModelRun modelRun,
    RequestReportDraftProposalCommand command,
    AiProviderDescriptor providerDescriptor,
    string structuredPayloadJson,
    IReadOnlyList<string> referencedSourceIds,
    long? latencyMilliseconds,
    int? inputTokenCount,
    int? outputTokenCount)
  {
    return new AiProposal(
      ProposalId.New(),
      modelRun.Id,
      modelRun.ProjectId,
      modelRun.InspectionSessionId,
      command.ReportId,
      providerDescriptor.ProviderId,
      providerDescriptor.ModelName,
      providerDescriptor.ModelDigest,
      modelRun.PromptPackageId,
      modelRun.PromptPackageVersion,
      modelRun.OutputSchemaId,
      modelRun.OutputSchemaVersion,
      modelRun.ContextManifestId,
      modelRun.ContextManifestHash,
      _clock.UtcNow,
      latencyMilliseconds,
      inputTokenCount,
      outputTokenCount,
      command.Temperature,
      referencedSourceIds,
      structuredPayloadJson);
  }

  private static ModelRunFailureClassification MapGenerationOutcome(AiGenerationFailureClassification failureClassification)
  {
    return failureClassification switch
    {
      AiGenerationFailureClassification.ProviderUnavailable => ModelRunFailureClassification.ProviderUnavailable,
      AiGenerationFailureClassification.Timeout => ModelRunFailureClassification.Timeout,
      AiGenerationFailureClassification.MalformedJson => ModelRunFailureClassification.MalformedJson,
      AiGenerationFailureClassification.Cancelled => ModelRunFailureClassification.Cancelled,
      AiGenerationFailureClassification.Unknown => ModelRunFailureClassification.Unknown,
      _ => ModelRunFailureClassification.None,
    };
  }

  private static string DescribeOutcome(AiProposal proposal)
  {
    return proposal.Status switch
    {
      ProposalStatus.ReadyForReview => "AI proposal is ready for human review.",
      ProposalStatus.Abstained => "AI proposal abstained.",
      ProposalStatus.PolicyRejected => "AI proposal failed policy validation.",
      ProposalStatus.SchemaRejected => "AI proposal failed schema validation.",
      _ => $"AI proposal finished with status {proposal.Status}.",
    };
  }

  private static string ComputeHash(string value)
  {
    var bytes = Encoding.UTF8.GetBytes(value);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }

  private void StageAudit(AuditEvent auditEvent)
  {
    _auditRecorder.Stage(auditEvent);
  }
}
