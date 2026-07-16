using SPINbuster.Application;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.AcceptAiProposal;
using SPINbuster.Application.UseCases.BuildReportProposalContext;
using SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;
using SPINbuster.Application.UseCases.RejectAiProposal;
using SPINbuster.Application.UseCases.RequestReportDraftProposal;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class AiProposalUseCaseTests
{
  [Fact]
  public async Task BuildReportProposalContextAssemblesGovernedManifest()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var useCase = new BuildReportProposalContextUseCase(
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository,
      fixture.ReportRepository,
      fixture.Clock);

    var result = await useCase.HandleAsync(new BuildReportProposalContextQuery(fixture.Report.Id));

    Assert.Equal(ContextManifestStatus.Complete, result.Status);
    Assert.Equal("report-draft-context-policy/1.0", result.ContextPolicyVersion);
    Assert.Contains("AI output is advisory only.", result.GovernedPromptContext, StringComparison.Ordinal);
    Assert.Contains(result.SourceEntries, entry => entry.SourceType == ContextSourceType.Report);
    Assert.Contains(result.SourceEntries, entry => entry.SourceType == ContextSourceType.FieldNote);
    Assert.Contains(result.SourceEntries, entry => entry.SourceType == ContextSourceType.EvidenceAttachment);
    Assert.False(string.IsNullOrWhiteSpace(result.ManifestHash));
  }

  [Fact]
  public async Task RequestReportDraftProposalCreatesReviewableProposalWithoutMutatingReport()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var useCase = fixture.CreateRequestUseCase();

    var result = await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      0.2m));

    Assert.NotNull(result.ProposalId);
    Assert.Equal(ProposalStatus.ReadyForReview, result.ProposalStatus);
    Assert.Equal(ModelRunState.ReadyForHumanReview, result.ModelRunState);
    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
    Assert.Single(fixture.ContextManifestRepository.AddedManifests);
    Assert.Single(fixture.ModelRunRepository.AddedModelRuns);
    Assert.Single(fixture.AiProposalRepository.AddedProposals);
    Assert.Single(fixture.ModelRunRepository.UpdatedModelRuns);
    Assert.Equal(ReportLifecycle.Draft, fixture.Report.Lifecycle);
    Assert.Equal(1, fixture.Report.RevisionNumber);
  }

  [Fact]
  public async Task RequestReportDraftProposalReplaysExistingResultForDuplicateOperation()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var useCase = fixture.CreateRequestUseCase();
    var command = new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      0.2m);

    var firstResult = await useCase.HandleAsync(command);
    var secondResult = await useCase.HandleAsync(command);

    Assert.Equal(firstResult.ProposalId, secondResult.ProposalId);
    Assert.True(secondResult.IsIdempotentReplay);
    Assert.Single(fixture.GenerationProvider.Requests);
    Assert.Single(await fixture.ModelRunRepository.GetAttemptsAsync(firstResult.ModelRunId));
    Assert.Single(fixture.AiProposalRepository.AddedProposals);
    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
    Assert.Equal(6, fixture.AuditRecorder.StagedEvents.Count);
  }

  [Fact]
  public async Task RequestReportDraftProposalRejectsChangedPayloadForDuplicateOperation()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var useCase = fixture.CreateRequestUseCase();
    var operationId = OperationId.New();

    await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      operationId,
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      0.2m));

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      useCase.HandleAsync(new RequestReportDraftProposalCommand(
        operationId,
        fixture.Report.Id,
        "report-draft-proposal-default",
        "0.1.0",
        0.8m)));

    Assert.Contains("already used for a different AI draft-proposal request", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public async Task ProviderFailureCreatesNoReviewableProposal()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.GenerationProvider.GenerateAsyncImpl = (_, _) => Task.FromResult(new AiGenerationResult(
      false,
      null,
      AiGenerationFailureClassification.ProviderUnavailable,
      "Provider unavailable.",
      null,
      null,
      null,
      null,
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));
    var useCase = fixture.CreateRequestUseCase();

    var result = await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));

    Assert.Null(result.ProposalId);
    Assert.Equal(ModelRunState.Failed, result.ModelRunState);
    Assert.Equal(ModelRunFailureClassification.ProviderUnavailable, result.FailureClassification);
    Assert.Empty(fixture.AiProposalRepository.AddedProposals);
    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task MalformedJsonIsRetainedAsNonReviewableFailure()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.GenerationProvider.GenerateAsyncImpl = (_, _) => Task.FromResult(new AiGenerationResult(
      true,
      "{",
      AiGenerationFailureClassification.None,
      null,
      10,
      1,
      1,
      null,
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));
    var useCase = fixture.CreateRequestUseCase();

    var result = await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));

    Assert.NotNull(result.ProposalId);
    Assert.Equal(ProposalStatus.SchemaRejected, result.ProposalStatus);
    Assert.Equal(ModelRunState.Failed, result.ModelRunState);
    Assert.Contains("invalid-json", fixture.AiProposalRepository.AddedProposals.Single().ValidationFailures);
  }

  [Fact]
  public async Task FabricatedSourceReferenceIsRejected()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.GenerationProvider.GenerateAsyncImpl = (_, _) => Task.FromResult(new AiGenerationResult(
      true,
      """
{
  "sections": [
    { "heading": "Summary", "content": "Deterministic advisory summary." }
  ],
  "reasoningSummary": "Grounded in governed sources only.",
  "confidenceBand": "Medium",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "fabricated-field-note" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": []
}
""",
      AiGenerationFailureClassification.None,
      null,
      10,
      1,
      1,
      null,
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));
    var useCase = fixture.CreateRequestUseCase();

    var result = await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));

    Assert.Equal(ProposalStatus.SchemaRejected, result.ProposalStatus);
    Assert.Contains(
      "fabricated-or-out-of-scope-source-reference",
      fixture.AiProposalRepository.AddedProposals.Single().ValidationFailures);
  }

  [Fact]
  public async Task ProhibitedAuthorityLanguageIsRejected()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.GenerationProvider.GenerateAsyncImpl = (_, _) => Task.FromResult(new AiGenerationResult(
      true,
      """
{
  "sections": [
    { "heading": "Summary", "content": "This final report is approved." }
  ],
  "reasoningSummary": "Authority claimed.",
  "confidenceBand": "Medium",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "FIELD_NOTE_ID" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": []
}
""".Replace("FIELD_NOTE_ID", fixture.FieldNote.Id.ToString(), StringComparison.Ordinal),
      AiGenerationFailureClassification.None,
      null,
      10,
      1,
      1,
      null,
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));
    var useCase = fixture.CreateRequestUseCase();

    var result = await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));

    Assert.Equal(ProposalStatus.PolicyRejected, result.ProposalStatus);
    Assert.Equal(ModelRunFailureClassification.PolicyValidationFailed, result.FailureClassification);
  }

  [Fact]
  public async Task CancellationTokenFlowsToProvider()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.GenerationProvider.GenerateAsyncImpl = (_, cancellationToken) =>
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(new AiGenerationResult(
        true,
        "{}",
        AiGenerationFailureClassification.None,
        null,
        1,
        1,
        1,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));
    };
    var useCase = fixture.CreateRequestUseCase();
    using var cancellationSource = new CancellationTokenSource();
    cancellationSource.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(() =>
      useCase.HandleAsync(new RequestReportDraftProposalCommand(
        OperationId.New(),
        fixture.Report.Id,
        "report-draft-proposal-default",
        "0.1.0",
        null),
      cancellationSource.Token));

    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
    Assert.Equal(ModelRunState.Failed, fixture.ModelRunRepository.UpdatedModelRuns.Single().State);
    Assert.Equal(ModelRunFailureClassification.Cancelled, fixture.ModelRunRepository.UpdatedModelRuns.Single().FailureClassification);
  }

  [Fact]
  public async Task RejectAiProposalStagesAuditAndClosesModelRun()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));
    var rejectUseCase = new RejectAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var rejectResult = await rejectUseCase.HandleAsync(new RejectAiProposalCommand(
      requestResult.ProposalId!.Value,
      "Human reviewer rejected the advisory proposal."));

    Assert.Equal(ProposalStatus.Rejected, rejectResult.ProposalStatus);
    Assert.Equal(ModelRunState.Closed, rejectResult.ModelRunState);
    Assert.Equal(3, fixture.UnitOfWork.CommitCount);
    var auditEvent = Assert.Single(fixture.AuditRecorder.StagedEvents, audit => audit.EventType == "AiProposalRejected");
    Assert.Equal(fixture.CurrentUser.UserId.Value, auditEvent.Actor);
    Assert.Equal(fixture.Clock.UtcNow, auditEvent.OccurredAtUtc);
  }

  [Fact]
  public async Task AcceptAiProposalStagesAuditAndClosesModelRunWithoutMutatingReport()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));
    var acceptUseCase = new AcceptAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var acceptResult = await acceptUseCase.HandleAsync(new AcceptAiProposalCommand(
      requestResult.ProposalId!.Value,
      "Human accepted the advisory proposal for later deterministic processing."));

    Assert.Equal(ProposalStatus.HumanAccepted, acceptResult.ProposalStatus);
    Assert.Equal(ModelRunState.Closed, acceptResult.ModelRunState);
    Assert.Equal(3, fixture.UnitOfWork.CommitCount);
    Assert.Equal(ReportLifecycle.Draft, fixture.Report.Lifecycle);
    Assert.Equal(1, fixture.Report.RevisionNumber);
    var auditEvent = Assert.Single(fixture.AuditRecorder.StagedEvents, audit => audit.EventType == "AiProposalAccepted");
    Assert.Equal(fixture.CurrentUser.UserId.Value, auditEvent.Actor);
    Assert.Equal(fixture.Clock.UtcNow, auditEvent.OccurredAtUtc);
  }

  [Fact]
  public async Task RepeatingAcceptDoesNotAppendDuplicateAuditEvents()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));
    var acceptUseCase = new AcceptAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    await acceptUseCase.HandleAsync(new AcceptAiProposalCommand(
      requestResult.ProposalId!.Value,
      "Accepted once."));
    var auditCountAfterFirstAccept = fixture.AuditRecorder.StagedEvents.Count(auditEvent => auditEvent.EventType == "AiProposalAccepted");

    await Assert.ThrowsAsync<LifecycleTransitionException>(() => acceptUseCase.HandleAsync(new AcceptAiProposalCommand(
      requestResult.ProposalId.Value,
      "Accepted twice.")));

    Assert.Equal(auditCountAfterFirstAccept, fixture.AuditRecorder.StagedEvents.Count(auditEvent => auditEvent.EventType == "AiProposalAccepted"));
    Assert.Equal(3, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task RepeatingRejectDoesNotAppendDuplicateAuditEvents()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));
    var rejectUseCase = new RejectAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    await rejectUseCase.HandleAsync(new RejectAiProposalCommand(
      requestResult.ProposalId!.Value,
      "Rejected once."));
    var auditCountAfterFirstReject = fixture.AuditRecorder.StagedEvents.Count(auditEvent => auditEvent.EventType == "AiProposalRejected");

    await Assert.ThrowsAsync<LifecycleTransitionException>(() => rejectUseCase.HandleAsync(new RejectAiProposalCommand(
      requestResult.ProposalId.Value,
      "Rejected twice.")));

    Assert.Equal(auditCountAfterFirstReject, fixture.AuditRecorder.StagedEvents.Count(auditEvent => auditEvent.EventType == "AiProposalRejected"));
    Assert.Equal(3, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task HumanAcceptedProposalCannotLaterBeRejected()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));
    var acceptUseCase = new AcceptAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    var rejectUseCase = new RejectAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    await acceptUseCase.HandleAsync(new AcceptAiProposalCommand(
      requestResult.ProposalId!.Value,
      "Accepted once."));

    await Assert.ThrowsAsync<LifecycleTransitionException>(() => rejectUseCase.HandleAsync(new RejectAiProposalCommand(
      requestResult.ProposalId.Value,
      "Reject after accept should fail.")));

    Assert.DoesNotContain(fixture.AuditRecorder.StagedEvents, auditEvent => auditEvent.EventType == "AiProposalRejected");
  }

  [Fact]
  public async Task RejectedProposalCannotLaterBeHumanAccepted()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));
    var rejectUseCase = new RejectAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    var acceptUseCase = new AcceptAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    await rejectUseCase.HandleAsync(new RejectAiProposalCommand(
      requestResult.ProposalId!.Value,
      "Rejected once."));

    await Assert.ThrowsAsync<LifecycleTransitionException>(() => acceptUseCase.HandleAsync(new AcceptAiProposalCommand(
      requestResult.ProposalId.Value,
      "Accept after reject should fail.")));

    Assert.DoesNotContain(fixture.AuditRecorder.StagedEvents, auditEvent => auditEvent.EventType == "AiProposalAccepted");
  }

  [Fact]
  public async Task LoadAiProposalWorkflowSnapshotReturnsAttemptsProposalAndAuditHistory()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      0.2m));
    var acceptUseCase = new AcceptAiProposalUseCase(
      fixture.AiProposalRepository,
      fixture.ModelRunRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    await acceptUseCase.HandleAsync(new AcceptAiProposalCommand(
      requestResult.ProposalId!.Value,
      "Accepted for deterministic downstream processing."));
    var loadUseCase = new LoadAiProposalWorkflowSnapshotUseCase(
      fixture.ModelRunRepository,
      fixture.AiProposalRepository,
      fixture.AuditEventQueryRepository);

    var result = await loadUseCase.HandleAsync(new LoadAiProposalWorkflowSnapshotQuery(
      requestResult.ModelRunId,
      requestResult.ProposalId));

    Assert.Equal(ModelRunState.Closed, result.ModelRunState);
    Assert.Single(result.Attempts);
    Assert.NotNull(result.Proposal);
    Assert.Equal(ProposalStatus.HumanAccepted, result.Proposal!.Status);
    Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiModelRunRequested");
    Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiProviderAttemptRecorded");
    Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiValidationCompleted");
    Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiModelRunCompleted");
    Assert.Contains(result.Proposal.AuditHistory, auditEntry => auditEntry.EventType == "AiProposalAccepted");
    Assert.False(string.IsNullOrWhiteSpace(result.Proposal.StructuredPayloadHash));
  }

  [Fact]
  public async Task RequestReportDraftProposalCommitsRequestedRunBeforeProviderInvocation()
  {
    var operationLog = new List<string>();
    var fixture = await AiProposalFixture.CreateAsync(operationLog);
    fixture.GenerationProvider.GenerateAsyncImpl = (request, cancellationToken) =>
    {
      operationLog.Add("provider-generate");
      return Task.FromResult(new AiGenerationResult(
        true,
        """
{
  "sections": [
    { "heading": "Summary", "content": "Deterministic advisory summary." }
  ],
  "reasoningSummary": "Grounded in governed sources only.",
  "confidenceBand": "Medium",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "FIELD_NOTE_ID" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": []
}
""".Replace("FIELD_NOTE_ID", fixture.FieldNote.Id.ToString(), StringComparison.Ordinal),
        AiGenerationFailureClassification.None,
        null,
        10,
        1,
        1,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));
    };
    var useCase = fixture.CreateRequestUseCase();

    await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));

    Assert.Equal(
      ["audit-stage", "audit-stage", "commit", "provider-generate", "audit-stage", "audit-stage", "audit-stage", "audit-stage", "commit"],
      operationLog);
  }

  [Theory]
  [InlineData(DeterministicAiFailure.ProviderUnavailable)]
  [InlineData(DeterministicAiFailure.Timeout)]
  [InlineData(DeterministicAiFailure.Cancelled)]
  [InlineData(DeterministicAiFailure.MalformedJson)]
  [InlineData(DeterministicAiFailure.SchemaRejected)]
  [InlineData(DeterministicAiFailure.PolicyRejected)]
  [InlineData(DeterministicAiFailure.Abstained)]
  public async Task LoadAiProposalWorkflowSnapshotRepresentsFailureAndAbstentionOutcomes(DeterministicAiFailure failure)
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.GenerationProvider.GenerateAsyncImpl = (_, cancellationToken) => Task.FromResult(CreateGenerationResult(fixture, failure, cancellationToken));
    var requestUseCase = fixture.CreateRequestUseCase();
    var requestResult = await requestUseCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));
    var loadUseCase = new LoadAiProposalWorkflowSnapshotUseCase(
      fixture.ModelRunRepository,
      fixture.AiProposalRepository,
      fixture.AuditEventQueryRepository);

    var result = await loadUseCase.HandleAsync(new LoadAiProposalWorkflowSnapshotQuery(
      requestResult.ModelRunId,
      requestResult.ProposalId));

    Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiModelRunRequested");

    if (failure == DeterministicAiFailure.Abstained)
    {
      Assert.Equal(ModelRunState.Abstained, result.ModelRunState);
      Assert.NotNull(result.Proposal);
      Assert.Equal(ProposalStatus.Abstained, result.Proposal!.Status);
      Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiValidationCompleted");
      return;
    }

    Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiProviderAttemptRecorded");
    Assert.Contains(result.ModelRunAuditHistory, auditEntry => auditEntry.EventType == "AiValidationCompleted");

    switch (failure)
    {
      case DeterministicAiFailure.MalformedJson:
      case DeterministicAiFailure.SchemaRejected:
      case DeterministicAiFailure.PolicyRejected:
        Assert.NotNull(result.Proposal);
        break;
      default:
        Assert.Null(result.Proposal);
        break;
    }
  }

  [Fact]
  public async Task RequestReportDraftProposalRejectsPromptPackageWithMismatchedContextContract()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.PromptPackageRegistry.PromptPackage = new PromptPackageDefinition(
      "report-draft-proposal-default",
      "0.1.0",
      "report-draft-proposer",
      "report-draft-context-policy/9.9",
      "report-draft-proposal",
      "1.0.0",
      [AiProviderCapability.StructuredOutput, AiProviderCapability.DeterministicFixtures],
      PromptPackageStatus.Approved,
      "Deterministic prompt package.");
    var useCase = fixture.CreateRequestUseCase();

    await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null)));
  }

  [Fact]
  public async Task RequestReportDraftProposalStoresCanonicalPayloadForReview()
  {
    var fixture = await AiProposalFixture.CreateAsync();
    fixture.GenerationProvider.GenerateAsyncImpl = (_, _) => Task.FromResult(new AiGenerationResult(
      true,
      """
{
  "warnings": [],
  "sections": [
    { "content": "Deterministic advisory summary.", "heading": "Summary" }
  ],
  "uncertaintyCodes": [],
  "confidenceBand": "Medium",
  "reasoningSummary": "Grounded in governed sources only.",
  "sourceReferences": [
    { "sourceId": "FIELD_NOTE_ID", "sourceType": "FieldNote" }
  ],
  "openQuestions": [],
  "missingInformation": []
}
""".Replace("FIELD_NOTE_ID", fixture.FieldNote.Id.ToString(), StringComparison.Ordinal),
      AiGenerationFailureClassification.None,
      null,
      10,
      1,
      1,
      null,
      new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
      new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)));
    var useCase = fixture.CreateRequestUseCase();

    var result = await useCase.HandleAsync(new RequestReportDraftProposalCommand(
      OperationId.New(),
      fixture.Report.Id,
      "report-draft-proposal-default",
      "0.1.0",
      null));

    var storedProposal = await fixture.AiProposalRepository.GetByIdAsync(result.ProposalId!.Value);
    Assert.NotNull(storedProposal);
    Assert.StartsWith("{\"confidenceBand\":\"Medium\"", storedProposal!.StructuredPayloadJson, StringComparison.Ordinal);
    Assert.False(string.IsNullOrWhiteSpace(storedProposal.StructuredPayloadHash));
  }

  private sealed class AiProposalFixture
  {
    private AiProposalFixture(
      FakeProjectRepository projectRepository,
      FakeInspectionSessionRepository inspectionSessionRepository,
      FakeReportRepository reportRepository,
      FakeContextManifestRepository contextManifestRepository,
      FakeModelRunRepository modelRunRepository,
      FakeAiProposalRepository aiProposalRepository,
      FakeAuditEventQueryRepository auditEventQueryRepository,
      FakeUnitOfWork unitOfWork,
      FakeClock clock,
      FakeCurrentUser currentUser,
      FakeAuditRecorder auditRecorder,
      FakeAiGenerationProvider generationProvider,
      FakePromptPackageRegistry promptPackageRegistry,
      List<string> operationLog,
      Project project,
      InspectionSession inspectionSession,
      FieldNote fieldNote,
      EvidenceAttachment evidenceAttachment,
      Report report)
    {
      ProjectRepository = projectRepository;
      InspectionSessionRepository = inspectionSessionRepository;
      ReportRepository = reportRepository;
      ContextManifestRepository = contextManifestRepository;
      ModelRunRepository = modelRunRepository;
      AiProposalRepository = aiProposalRepository;
      AuditEventQueryRepository = auditEventQueryRepository;
      UnitOfWork = unitOfWork;
      Clock = clock;
      CurrentUser = currentUser;
      AuditRecorder = auditRecorder;
      GenerationProvider = generationProvider;
      PromptPackageRegistry = promptPackageRegistry;
      OperationLog = operationLog;
      Project = project;
      InspectionSession = inspectionSession;
      FieldNote = fieldNote;
      EvidenceAttachment = evidenceAttachment;
      Report = report;
    }

    public FakeProjectRepository ProjectRepository { get; }

    public FakeInspectionSessionRepository InspectionSessionRepository { get; }

    public FakeReportRepository ReportRepository { get; }

    public FakeContextManifestRepository ContextManifestRepository { get; }

    public FakeModelRunRepository ModelRunRepository { get; }

    public FakeAiProposalRepository AiProposalRepository { get; }

    public FakeUnitOfWork UnitOfWork { get; }

    public FakeAuditEventQueryRepository AuditEventQueryRepository { get; }

    public FakeClock Clock { get; }

    public FakeCurrentUser CurrentUser { get; }

    public FakeAuditRecorder AuditRecorder { get; }

    public FakeAiGenerationProvider GenerationProvider { get; }

    public FakePromptPackageRegistry PromptPackageRegistry { get; }

    public List<string> OperationLog { get; }

    public Project Project { get; }

    public InspectionSession InspectionSession { get; }

    public FieldNote FieldNote { get; }

    public EvidenceAttachment EvidenceAttachment { get; }

    public Report Report { get; }

    public RequestReportDraftProposalUseCase CreateRequestUseCase()
    {
      return new RequestReportDraftProposalUseCase(
        ProjectRepository,
        InspectionSessionRepository,
        ReportRepository,
        ContextManifestRepository,
        ModelRunRepository,
        AiProposalRepository,
        GenerationProvider,
        PromptPackageRegistry,
        new JsonAiProposalPayloadValidator(),
        UnitOfWork,
        Clock,
        CurrentUser,
        AuditRecorder);
    }

    public static async Task<AiProposalFixture> CreateAsync(List<string>? operationLog = null)
    {
      operationLog ??= [];
      var projectRepository = new FakeProjectRepository();
      var inspectionSessionRepository = new FakeInspectionSessionRepository();
      var reportRepository = new FakeReportRepository();
      var contextManifestRepository = new FakeContextManifestRepository();
      var modelRunRepository = new FakeModelRunRepository();
      var aiProposalRepository = new FakeAiProposalRepository();
      var unitOfWork = new FakeUnitOfWork(operationLog);
      var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
      var currentUser = new FakeCurrentUser("inspector@example.invalid");
      var auditRecorder = new FakeAuditRecorder(operationLog);
      var auditEventQueryRepository = new FakeAuditEventQueryRepository(auditRecorder);
      var generationProvider = new FakeAiGenerationProvider();
      var promptPackageRegistry = new FakePromptPackageRegistry();

      var project = new Project(
        ProjectId.New(),
        "Project Falcon",
        "owner@example.invalid",
        new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
      project.Activate(currentUser.UserId.Value, clock.UtcNow);
      var inspectionSession = new InspectionSession(
        InspectionSessionId.New(),
        project.Id,
        "Initial Walkdown",
        currentUser.UserId.Value,
        clock.UtcNow);
      inspectionSession.Start(currentUser.UserId.Value, clock.UtcNow.AddMinutes(1));
      var fieldNote = inspectionSession.RecordFieldNote(
        FieldNoteId.New(),
        currentUser.UserId.Value,
        clock.UtcNow.AddMinutes(2),
        new FieldNoteRawText("Observed corrosion at lower seam."));
      var evidenceAttachment = inspectionSession.AttachEvidence(
        EvidenceAttachmentId.New(),
        currentUser.UserId.Value,
        clock.UtcNow.AddMinutes(3),
        new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));
      inspectionSession.InterpretEvidence(
        evidenceAttachment.Id,
        new EvidenceInterpretation(
          "Corrosion visible near lower seam.",
          currentUser.UserId.Value,
          clock.UtcNow.AddMinutes(4)));
      var report = new Report(
        ReportId.New(),
        project.Id,
        inspectionSession.Id,
        new ReportTitle("Initial Draft Report"),
        [new ReportDraftSection("Summary", "Existing authoritative draft.")],
        [fieldNote.Id],
        [evidenceAttachment.Id],
        currentUser.UserId.Value,
        clock.UtcNow.AddMinutes(5));

      await projectRepository.AddAsync(project);
      await inspectionSessionRepository.AddAsync(inspectionSession);
      await reportRepository.AddAsync(report, OperationId.New());

      return new AiProposalFixture(
        projectRepository,
        inspectionSessionRepository,
        reportRepository,
        contextManifestRepository,
        modelRunRepository,
        aiProposalRepository,
        auditEventQueryRepository,
        unitOfWork,
        clock,
        currentUser,
        auditRecorder,
        generationProvider,
        promptPackageRegistry,
        operationLog,
        project,
        inspectionSession,
        fieldNote,
        evidenceAttachment,
        report);
    }
  }

  public enum DeterministicAiFailure
  {
    ProviderUnavailable,
    Timeout,
    Cancelled,
    MalformedJson,
    SchemaRejected,
    PolicyRejected,
    Abstained,
  }

  private static AiGenerationResult CreateGenerationResult(
    AiProposalFixture fixture,
    DeterministicAiFailure failure,
    CancellationToken cancellationToken)
  {
    return failure switch
    {
      DeterministicAiFailure.ProviderUnavailable => new AiGenerationResult(
        false,
        null,
        AiGenerationFailureClassification.ProviderUnavailable,
        "Provider unavailable.",
        null,
        null,
        null,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)),
      DeterministicAiFailure.Timeout => new AiGenerationResult(
        false,
        null,
        AiGenerationFailureClassification.Timeout,
        "Provider timed out.",
        null,
        null,
        null,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 30, TimeSpan.Zero)),
      DeterministicAiFailure.Cancelled => new AiGenerationResult(
        false,
        null,
        AiGenerationFailureClassification.Cancelled,
        "Provider invocation cancelled.",
        null,
        null,
        null,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 5, TimeSpan.Zero)),
      DeterministicAiFailure.MalformedJson => new AiGenerationResult(
        true,
        "{",
        AiGenerationFailureClassification.None,
        null,
        5,
        1,
        1,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)),
      DeterministicAiFailure.SchemaRejected => new AiGenerationResult(
        true,
        """
{
  "sections": [
    { "heading": "Summary", "content": "Deterministic advisory summary." }
  ],
  "reasoningSummary": "Grounded in governed sources only.",
  "confidenceBand": "Medium",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "fabricated-field-note" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": []
}
""",
        AiGenerationFailureClassification.None,
        null,
        5,
        1,
        1,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)),
      DeterministicAiFailure.PolicyRejected => new AiGenerationResult(
        true,
        """
{
  "sections": [
    { "heading": "Summary", "content": "This final report is approved." }
  ],
  "reasoningSummary": "Authority claimed.",
  "confidenceBand": "Medium",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "FIELD_NOTE_ID" }
  ],
  "missingInformation": [],
  "openQuestions": [],
  "warnings": [],
  "uncertaintyCodes": []
}
""".Replace("FIELD_NOTE_ID", fixture.FieldNote.Id.ToString(), StringComparison.Ordinal),
        AiGenerationFailureClassification.None,
        null,
        5,
        1,
        1,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)),
      DeterministicAiFailure.Abstained => new AiGenerationResult(
        true,
        """
{
  "sections": [],
  "reasoningSummary": "The governed context is insufficient to produce a safe grounded proposal.",
  "confidenceBand": "None",
  "sourceReferences": [
    { "sourceType": "FieldNote", "sourceId": "FIELD_NOTE_ID" }
  ],
  "missingInformation": ["insufficient-source-grounding"],
  "openQuestions": [],
  "warnings": ["context-insufficient"],
  "uncertaintyCodes": [],
  "abstentionReason": "Insufficient grounded evidence for a proposal."
}
""".Replace("FIELD_NOTE_ID", fixture.FieldNote.Id.ToString(), StringComparison.Ordinal),
        AiGenerationFailureClassification.None,
        null,
        5,
        1,
        1,
        null,
        new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 7, 15, 16, 0, 1, TimeSpan.Zero)),
      _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
    };
  }
}
