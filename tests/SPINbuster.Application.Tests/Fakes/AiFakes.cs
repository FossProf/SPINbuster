using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeModelRunRepository : IModelRunRepository
{
  private readonly Dictionary<ModelRunId, ModelRun> _modelRuns = [];
  private readonly Dictionary<string, ModelRunId> _correlationIds = new(StringComparer.Ordinal);
  private readonly Dictionary<ModelRunId, List<ModelRunAttempt>> _attempts = [];

  public List<ModelRun> AddedModelRuns { get; } = [];

  public List<ModelRun> UpdatedModelRuns { get; } = [];

  public Task<ModelRun?> GetByIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default)
  {
    _modelRuns.TryGetValue(modelRunId, out var modelRun);
    return Task.FromResult(modelRun);
  }

  public Task<ModelRun?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _correlationIds.TryGetValue(correlationId, out var modelRunId) && _modelRuns.TryGetValue(modelRunId, out var modelRun)
        ? modelRun
        : null);
  }

  public Task<IReadOnlyCollection<ModelRunAttempt>> GetAttemptsAsync(
    ModelRunId modelRunId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<ModelRunAttempt>>(
      _attempts.TryGetValue(modelRunId, out var attempts) ? attempts.ToArray() : []);
  }

  public Task AddAsync(ModelRun modelRun, CancellationToken cancellationToken = default)
  {
    _modelRuns[modelRun.Id] = modelRun;
    _correlationIds[modelRun.CorrelationId] = modelRun.Id;
    AddedModelRuns.Add(modelRun);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(ModelRun modelRun, CancellationToken cancellationToken = default)
  {
    _modelRuns[modelRun.Id] = modelRun;
    UpdatedModelRuns.Add(modelRun);
    return Task.CompletedTask;
  }

  public Task AddAttemptAsync(ModelRunAttempt attempt, CancellationToken cancellationToken = default)
  {
    if (!_attempts.TryGetValue(attempt.ModelRunId, out var attempts))
    {
      attempts = [];
      _attempts[attempt.ModelRunId] = attempts;
    }

    attempts.Add(attempt);
    return Task.CompletedTask;
  }
}

internal sealed class FakeAiProposalRepository : IAiProposalRepository
{
  private readonly Dictionary<ProposalId, AiProposal> _proposals = [];
  private readonly Dictionary<ModelRunId, ProposalId> _proposalIdsByModelRun = [];

  public List<AiProposal> AddedProposals { get; } = [];

  public List<AiProposal> UpdatedProposals { get; } = [];

  public Task<AiProposal?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken = default)
  {
    _proposals.TryGetValue(proposalId, out var proposal);
    return Task.FromResult(proposal);
  }

  public Task<AiProposal?> GetByModelRunIdAsync(ModelRunId modelRunId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(
      _proposalIdsByModelRun.TryGetValue(modelRunId, out var proposalId) && _proposals.TryGetValue(proposalId, out var proposal)
        ? proposal
        : null);
  }

  public Task AddAsync(AiProposal proposal, CancellationToken cancellationToken = default)
  {
    _proposals[proposal.Id] = proposal;
    _proposalIdsByModelRun[proposal.ModelRunId] = proposal.Id;
    AddedProposals.Add(proposal);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(AiProposal proposal, CancellationToken cancellationToken = default)
  {
    _proposals[proposal.Id] = proposal;
    UpdatedProposals.Add(proposal);
    return Task.CompletedTask;
  }

  public Task<int> CountAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_proposals.Count);
  }
}

internal sealed class FakePromptPackageRegistry : IAiPromptPackageRegistry
{
  public PromptPackageDefinition? PromptPackage { get; set; } = new(
    "report-draft-proposal-default",
    "0.1.0",
    "report-draft-proposer",
    "report-draft-context-policy/1.0",
    "report-draft-proposal",
    "1.0.0",
    [
      AiProviderCapability.StructuredOutput,
      AiProviderCapability.DeterministicFixtures,
    ],
    PromptPackageStatus.Approved,
    "Deterministic prompt package.");

  public Task<PromptPackageDefinition?> GetByIdAsync(
    string packageId,
    string semanticVersion,
    CancellationToken cancellationToken = default)
  {
    var matches = PromptPackage is not null
      && string.Equals(PromptPackage.PackageId, packageId, StringComparison.Ordinal)
      && string.Equals(PromptPackage.SemanticVersion, semanticVersion, StringComparison.Ordinal);
    return Task.FromResult(matches ? PromptPackage : null);
  }
}

internal sealed class FakeAiGenerationProvider : IAiGenerationProvider
{
  public AiProviderDescriptor Descriptor { get; set; } = new(
    "tier0-deterministic",
    "deterministic-fixture",
    "sha256:deterministic-fixture-v1",
    [
      AiProviderCapability.StructuredOutput,
      AiProviderCapability.DeterministicFixtures,
    ]);

  public Func<AiGenerationRequest, CancellationToken, Task<AiGenerationResult>> GenerateAsyncImpl { get; set; } =
    (request, _) => Task.FromResult(new AiGenerationResult(
      true,
      """
{
  "sections": [
    { "heading": "Summary", "content": "Deterministic advisory summary." }
  ],
  "reasoningSummary": "Grounded in governed sources only.",
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
"""
        .Replace("FIELD_NOTE_ID", request.PromptContext.Contains("FieldNote ", StringComparison.Ordinal) ? request.PromptContext.Split("FieldNote ", StringSplitOptions.None)[1].Split(':')[0].Trim() : "missing-id", StringComparison.Ordinal)
        .Replace("EVIDENCE_ATTACHMENT_ID", request.PromptContext.Contains("Evidence ", StringComparison.Ordinal) ? request.PromptContext.Split("Evidence ", StringSplitOptions.None)[1].Split(':')[0].Trim() : "missing-id", StringComparison.Ordinal),
      AiGenerationFailureClassification.None,
      null,
      42,
      128,
      96,
      request.Temperature,
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));

  public List<AiGenerationRequest> Requests { get; } = [];

  public AiProviderDescriptor Describe() => Descriptor;

  public Task<AiGenerationResult> GenerateAsync(
    AiGenerationRequest request,
    CancellationToken cancellationToken = default)
  {
    Requests.Add(request);
    return GenerateAsyncImpl(request, cancellationToken);
  }
}
