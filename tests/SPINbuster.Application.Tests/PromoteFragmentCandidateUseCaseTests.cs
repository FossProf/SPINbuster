using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.PromoteFragmentCandidate;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class PromoteFragmentCandidateUseCaseTests
{
  private static readonly DateTimeOffset TestTime = new(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task SuccessfulPromotionRecordsRecordAndAttempt()
  {
    var fixture = CreateFixture();
    var (candidateId, sourceContentHash, _) = await SeedReadyCandidateAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, result.Status);
    Assert.NotNull(result.KnowledgeDocumentId);
    Assert.NotNull(result.KnowledgeDocumentRevisionId);
    Assert.NotNull(result.KnowledgeCitationId);

    Assert.Single(fixture.PromotionRecordRepository.AddedRecords);
    Assert.Single(fixture.PromotionAttemptRepository.AddedAttempts);

    var record = fixture.PromotionRecordRepository.AddedRecords[0];
    var attempt = fixture.PromotionAttemptRepository.AddedAttempts[0];

    Assert.Equal(PromotionAttemptOutcome.Promoted, attempt.Outcome);
    Assert.Equal(result.PromotionDiagnosticId, attempt.DiagnosticId);
    Assert.Equal(candidateId, attempt.FragmentCandidateId);
    Assert.Equal(sourceContentHash, attempt.ContentHash);
    Assert.Equal(record.Id, attempt.RecordId);
    Assert.Equal(record.LatestAttemptId, attempt.Id);
  }

  [Fact]
  public async Task ExactSuccessfulReplayReturnsCachedResultBeforeEligibilityChecks()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, firstResult.Status);

    var secondResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(firstResult.PromotionDiagnosticId, secondResult.PromotionDiagnosticId);
    Assert.Equal(firstResult.KnowledgeDocumentId, secondResult.KnowledgeDocumentId);
    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task DifferentTargetMetadataProducesDifferentIdentityNoReplay()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, firstResult.Status);

    var secondResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Structural Spec",
      "EXT-002",
      "Structural"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, secondResult.Status);
    Assert.NotEqual(firstResult.PromotionDiagnosticId, secondResult.PromotionDiagnosticId);
    Assert.NotEqual(firstResult.KnowledgeDocumentId, secondResult.KnowledgeDocumentId);

    Assert.Equal(2, fixture.PromotionRecordRepository.AddedRecords.Count);
    Assert.Equal(2, fixture.PromotionAttemptRepository.AddedAttempts.Count);
    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task FailedAttemptDoesNotBlockSubsequentRetry()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var sourceId = ImportedSourceId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    await fixture.ProjectRepository.AddAsync(project);
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      "source-hash",
      "SHA-256",
      1,
      "system",
      TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      projectId,
      sourceId,
      "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      1,
      ContentKind.PlainText,
      "Some text",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var failResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      null,
      null));

    Assert.Equal(PromotionDiagnosticStatus.Failed, failResult.Status);
    Assert.Null(failResult.KnowledgeDocumentId);
    Assert.Single(fixture.PromotionAttemptRepository.AddedAttempts);

    var failAttempt = fixture.PromotionAttemptRepository.AddedAttempts[0];
    Assert.Equal(PromotionAttemptOutcome.RetryablePreconditionFailure, failAttempt.Outcome);

    var activeProject = new Project(projectId, "Test", "system", TestTime);
    activeProject.Activate("system", TestTime);
    await fixture.ProjectRepository.UpdateAsync(activeProject);

    fixture.Clock.UtcNow = TestTime.AddMinutes(1);

    var retryResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      null,
      null));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, retryResult.Status);
    Assert.NotNull(retryResult.KnowledgeDocumentId);
    Assert.Equal(2, fixture.PromotionAttemptRepository.AddedAttempts.Count);

    var retryAttempt = fixture.PromotionAttemptRepository.AddedAttempts[1];
    Assert.Equal(PromotionAttemptOutcome.Promoted, retryAttempt.Outcome);
  }

  [Fact]
  public async Task FailedAttemptClassifiedAsPermanentInvariantViolationForLifecycleTransition()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(
      fixture,
      reviewState: FragmentCandidateReviewState.Generated);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      null,
      null));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Single(fixture.PromotionAttemptRepository.AddedAttempts);

    var attempt = fixture.PromotionAttemptRepository.AddedAttempts[0];
    Assert.Equal(PromotionAttemptOutcome.PermanentInvariantViolation, attempt.Outcome);
    Assert.NotNull(attempt.FailureReason);
  }

  [Fact]
  public async Task SameCandidateSameTargetFailureThenFailureThenSuccess()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var sourceId = ImportedSourceId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    await fixture.ProjectRepository.AddAsync(project);
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(), projectId, sourceId, "test-parser", "1.0.0", "1.0.0",
      "contract-hash", "source-hash", "SHA-256", 1, "system", TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(), parserRun.Id, projectId, sourceId, "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "Some text", ConfidenceBand.High,
      "test-parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var failResult1 = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id, KnowledgeDocumentType.Specification, "Concrete Spec", null, null));
    Assert.Equal(PromotionDiagnosticStatus.Failed, failResult1.Status);
    Assert.Single(fixture.PromotionAttemptRepository.AddedAttempts);

    var activeProject = new Project(projectId, "Test", "system", TestTime);
    activeProject.Activate("system", TestTime);
    await fixture.ProjectRepository.UpdateAsync(activeProject);
    fixture.Clock.UtcNow = TestTime.AddMinutes(1);

    var failResult2 = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id, KnowledgeDocumentType.Specification, "Concrete Spec", null, null));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, failResult2.Status);
    Assert.NotNull(failResult2.KnowledgeDocumentId);
    Assert.Equal(2, fixture.PromotionAttemptRepository.AddedAttempts.Count);

    var attempt1 = fixture.PromotionAttemptRepository.AddedAttempts[0];
    var attempt2 = fixture.PromotionAttemptRepository.AddedAttempts[1];
    Assert.NotEqual(attempt1.Id.Value, attempt2.Id.Value);
    Assert.NotEqual(attempt1.DiagnosticId, attempt2.DiagnosticId);
    Assert.Equal(PromotionAttemptOutcome.RetryablePreconditionFailure, attempt1.Outcome);
    Assert.Equal(PromotionAttemptOutcome.Promoted, attempt2.Outcome);
  }

  [Fact]
  public async Task ThreeDistinctAttemptIdsWithTwoFailuresThenPromoted()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var sourceId = ImportedSourceId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    await fixture.ProjectRepository.AddAsync(project);
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(), projectId, sourceId, "test-parser", "1.0.0", "1.0.0",
      "contract-hash", "source-hash", "SHA-256", 1, "system", TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(), parserRun.Id, projectId, sourceId, "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "Some text", ConfidenceBand.High,
      "test-parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var r1 = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id, KnowledgeDocumentType.Specification, "Concrete Spec", null, null));
    Assert.Equal(PromotionDiagnosticStatus.Failed, r1.Status);

    var r2 = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id, KnowledgeDocumentType.Specification, "Concrete Spec", null, null));
    Assert.Equal(PromotionDiagnosticStatus.Failed, r2.Status);

    var activeProject = new Project(projectId, "Test", "system", TestTime);
    activeProject.Activate("system", TestTime);
    await fixture.ProjectRepository.UpdateAsync(activeProject);
    fixture.Clock.UtcNow = TestTime.AddMinutes(1);

    var r3 = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id, KnowledgeDocumentType.Specification, "Concrete Spec", null, null));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, r3.Status);

    Assert.Equal(3, fixture.PromotionAttemptRepository.AddedAttempts.Count);
    var ids = fixture.PromotionAttemptRepository.AddedAttempts.Select(a => a.Id.Value).Distinct().ToList();
    Assert.Equal(3, ids.Count);
  }

  [Fact]
  public async Task FailedDiagnosticNeverReturnedAsReplay()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var sourceId = ImportedSourceId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    await fixture.ProjectRepository.AddAsync(project);
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(), projectId, sourceId, "test-parser", "1.0.0", "1.0.0",
      "contract-hash", "source-hash", "SHA-256", 1, "system", TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(), parserRun.Id, projectId, sourceId, "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"), 1,
      ContentKind.PlainText, "Some text", ConfidenceBand.High,
      "test-parser", "1.0.0", TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var failResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id, KnowledgeDocumentType.Specification, "Concrete Spec", null, null));
    Assert.Equal(PromotionDiagnosticStatus.Failed, failResult.Status);

    var replayResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id, KnowledgeDocumentType.Specification, "Concrete Spec", null, null));
    Assert.Equal(PromotionDiagnosticStatus.Failed, replayResult.Status);
    Assert.NotEqual(failResult.PromotionDiagnosticId, replayResult.PromotionDiagnosticId);
    Assert.Equal(2, fixture.PromotionAttemptRepository.AddedAttempts.Count);
    Assert.Empty(fixture.PromotionRecordRepository.AddedRecords);
  }

  [Fact]
  public async Task DifferentCandidateDifferentTargetRemainsIndependent()
  {
    var fixture = CreateFixture();
    var (candidateAId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    var resultA = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateAId, KnowledgeDocumentType.Specification, "Spec A", "EXT-A", "Civil"));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, resultA.Status);

    var candidateBId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);
    fixture.Clock.UtcNow = TestTime.AddMinutes(5);

    var resultB = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateBId, KnowledgeDocumentType.Specification, "Spec B", "EXT-B", "Structural"));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, resultB.Status);

    Assert.NotEqual(resultA.KnowledgeDocumentId, resultB.KnowledgeDocumentId);
    Assert.Equal(2, fixture.PromotionAttemptRepository.AddedAttempts.Count);
    Assert.Equal(2, fixture.PromotionRecordRepository.AddedRecords.Count);
  }

  [Fact]
  public void IdentityHashIsDeterministicAcrossNormalization()
  {
    var projectId = ProjectId.New();
    var identityA = new PromotionIdentity(
      projectId,
      KnowledgeDocumentType.Specification,
      "  Concrete Spec  ",
      " EXT-001 ",
      " Civil ",
      "imported-source:parser@1.0.0:WholeDocument:");

    var identityB = new PromotionIdentity(
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil",
      "imported-source:parser@1.0.0:WholeDocument:");

    Assert.Equal(identityA.Hash, identityB.Hash);
  }

  [Fact]
  public void IdentityHashDiffersWhenSourceFragmentDiffers()
  {
    var projectId = ProjectId.New();
    var identityA = new PromotionIdentity(
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      null,
      null,
      "source-aaa:parser@1.0.0:WholeDocument:");

    var identityB = new PromotionIdentity(
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      null,
      null,
      "source-bbb:parser@1.0.0:WholeDocument:");

    Assert.NotEqual(identityA.Hash, identityB.Hash);
  }

  [Fact]
  public async Task ReplayDoesNotMutateKnowledge()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var documentsAfterFirst = fixture.KnowledgeDocumentRepository.AddedDocuments.Count;
    var revisionsAfterFirst = fixture.KnowledgeRevisionRepository.AddedRevisions.Count;
    var citationsAfterFirst = fixture.KnowledgeCitationRepository.AddedCitations.Count;

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(documentsAfterFirst, fixture.KnowledgeDocumentRepository.AddedDocuments.Count);
    Assert.Equal(revisionsAfterFirst, fixture.KnowledgeRevisionRepository.AddedRevisions.Count);
    Assert.Equal(citationsAfterFirst, fixture.KnowledgeCitationRepository.AddedCitations.Count);
  }

  [Fact]
  public async Task SupersedingPromotionUsesSingleAtomicCommit()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(1, fixture.UnitOfWork.CommitCount);

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task SupersedingPromotionUpdatesOldRevisionInSameCommit()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Empty(fixture.KnowledgeRevisionRepository.UpdatedRevisions);
    Assert.Single(fixture.KnowledgeRevisionRepository.AddedRevisions);
  }

  [Fact]
  public async Task ChainOfThreePromotionsUsesOneCommitEach()
  {
    var fixture = CreateFixture();
    var (firstCandidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      firstCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);
    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var thirdCandidateId = await SeedThirdCandidateInSameDocumentAsync(fixture, projectId);
    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      thirdCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(3, fixture.UnitOfWork.CommitCount);
    Assert.Single(fixture.KnowledgeRevisionRepository.AddedRevisions);
    Assert.Empty(fixture.KnowledgeRevisionRepository.UpdatedRevisions);
    Assert.Single(fixture.KnowledgeCitationRepository.AddedCitations);
  }

  [Fact]
  public async Task CommitFailureDuringSupersessionDoesNotPersistPartialState()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(1, fixture.UnitOfWork.CommitCount);

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);
    fixture.UnitOfWork.ThrowOnCommit = true;

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
        secondCandidateId,
        KnowledgeDocumentType.Specification,
        "Concrete Spec",
        "EXT-001",
        "Civil")));

    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task SupersessionAuditEventsStagedBeforeSingleCommit()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var stagedCountBefore = fixture.AuditRecorder.StagedEvents.Count;

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.True(fixture.AuditRecorder.StagedEvents.Count > stagedCountBefore);
    Assert.Contains(fixture.AuditRecorder.StagedEvents, e => e.EventType == "KnowledgeRevisionCreated");
  }

  [Fact]
  public async Task DerivedFromRelationshipCreatedOnlyOncePerRevision()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Single(fixture.KnowledgeRelationshipRepository.AddedRelationships);
  }

  [Fact]
  public async Task PromotionSuccessPersistsProvenanceInSameUoWCommit()
  {
    var fixture = CreateFixture();
    var (candidateId, sourceContentHash, projectId) = await SeedReadyCandidateAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, result.Status);
    Assert.Single(fixture.PromotionProvenanceRepository.AddedProvenances);

    var provenance = fixture.PromotionProvenanceRepository.AddedProvenances[0];
    Assert.Equal(projectId, provenance.ProjectId);
    Assert.Equal(result.KnowledgeDocumentRevisionId, provenance.PromotedRevisionId);
    Assert.Equal(result.PromotionDiagnosticId, provenance.DiagnosticId);
    Assert.Equal(candidateId, provenance.FragmentCandidateId);
    Assert.Equal(sourceContentHash, provenance.FragmentSourceContentHash);
    Assert.Equal(fixture.CurrentUser.UserId.Value, provenance.PromotedBy);
  }

  [Fact]
  public async Task CommitFailureReportsNoPromotionSuccessAndLeavesNoPartialProvenance()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);
    fixture.UnitOfWork.ThrowOnCommit = true;

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
        candidateId,
        KnowledgeDocumentType.Specification,
        "Concrete Spec",
        "EXT-001",
        "Civil")));

    Assert.Equal(0, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task AmbiguousDocumentMatchReturnsFailedWithConflictType()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    project.Activate("system", TestTime);
    await fixture.ProjectRepository.AddAsync(project);

    var documentA = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil",
      "system",
      TestTime);

    var documentB = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil",
      "system",
      TestTime);

    await fixture.KnowledgeDocumentRepository.AddAsync(documentA);
    await fixture.KnowledgeDocumentRepository.AddAsync(documentB);

    var sourceId = ImportedSourceId.New();
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      "source-hash",
      "SHA-256",
      1,
      "system",
      TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      projectId,
      sourceId,
      "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      1,
      ContentKind.PlainText,
      "Some text",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Equal(PromotionConflictType.AmbiguousDocumentMatch, result.ConflictType);
    Assert.Null(result.KnowledgeDocumentId);
    Assert.Null(result.KnowledgeDocumentRevisionId);
  }

  [Fact]
  public async Task AmbiguousDocumentMatchCreatesNoRecords()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    project.Activate("system", TestTime);
    await fixture.ProjectRepository.AddAsync(project);

    var documentA = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil",
      "system",
      TestTime);

    var documentB = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil",
      "system",
      TestTime);

    await fixture.KnowledgeDocumentRepository.AddAsync(documentA);
    await fixture.KnowledgeDocumentRepository.AddAsync(documentB);

    var sourceId = ImportedSourceId.New();
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      "source-hash",
      "SHA-256",
      1,
      "system",
      TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      projectId,
      sourceId,
      "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      1,
      ContentKind.PlainText,
      "Some text",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var revisionsBefore = fixture.KnowledgeRevisionRepository.AddedRevisions.Count;

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(revisionsBefore, fixture.KnowledgeRevisionRepository.AddedRevisions.Count);
  }

  [Fact]
  public async Task HigherAuthorityExistsReturnsFailedWithConflictType()
  {
    var fixture = CreateFixture();
    var projectId = ProjectId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    project.Activate("system", TestTime);
    await fixture.ProjectRepository.AddAsync(project);

    var sourceId = ImportedSourceId.New();
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      "source-hash",
      "SHA-256",
      1,
      "system",
      TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      projectId,
      sourceId,
      "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      1,
      ContentKind.PlainText,
      "Some text",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);

    var document = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      projectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil",
      "system",
      TestTime);

    var existingRevision = new KnowledgeDocumentRevision(
      KnowledgeDocumentRevisionId.New(),
      document.Id,
      KnowledgeSourceId.New(),
      "v1-initial",
      null,
      TestTime,
      KnowledgeSourceAuthorityLevel.EngineerIssued,
      "content-hash",
      "metadata-hash",
      null,
      null,
      null,
      TestTime);

    document.AddInitialRevision(existingRevision, "system", TestTime);
    await fixture.KnowledgeDocumentRepository.AddAsync(document);
    await fixture.KnowledgeRevisionRepository.AddAsync(existingRevision);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidate.Id,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Equal(PromotionConflictType.HigherAuthorityExists, result.ConflictType);
    Assert.Null(result.KnowledgeDocumentId);
  }

  [Fact]
  public async Task EqualAuthorityBlocksSupersession()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);

    fixture.Clock.UtcNow = TestTime.AddMinutes(5);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Equal(PromotionConflictType.HigherAuthorityExists, result.ConflictType);
    Assert.Null(result.KnowledgeDocumentId);
  }

  [Fact]
  public async Task EqualAuthorityBlocksSupersessionEvenWithLaterTimestamp()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);

    fixture.Clock.UtcNow = TestTime.AddMinutes(5);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Equal(PromotionConflictType.HigherAuthorityExists, result.ConflictType);
  }

  [Fact]
  public async Task TemporalOrderViolationReturnsFailed()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);

    fixture.Clock.UtcNow = TestTime.AddMinutes(-10);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Equal(PromotionConflictType.HigherAuthorityExists, result.ConflictType);
    Assert.Null(result.KnowledgeDocumentId);
  }

  [Fact]
  public async Task EqualAuthorityBlocksSupersessionWithSameTimestamp()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      secondCandidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Equal(PromotionConflictType.HigherAuthorityExists, result.ConflictType);
  }

  [Fact]
  public async Task SingleMatchFoundReturnsExistingDocument()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, result.Status);
    Assert.NotNull(result.KnowledgeDocumentId);
    Assert.Single(fixture.KnowledgeDocumentRepository.AddedDocuments);
  }

  [Fact]
  public async Task ZeroMatchesCreatesNewDocument()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, result.Status);
    Assert.NotNull(result.KnowledgeDocumentId);
    Assert.Single(fixture.KnowledgeDocumentRepository.AddedDocuments);
  }

  [Fact]
  public async Task ConcurrencyConflictReturnsFailedWithConflictType()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);
    fixture.KnowledgeDocumentRepository.SimulateConcurrencyConflict = true;

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Failed, result.Status);
    Assert.Equal(PromotionConflictType.ConcurrentPromotion, result.ConflictType);
    Assert.Null(result.KnowledgeDocumentId);
  }

  [Fact]
  public async Task SupersedingPromotionWithHigherAuthorityCreatesSupersedesRelationship()
  {
    var fixture = CreateFixture();
    var (candidateId, _, projectId) = await SeedReadyCandidateAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, firstResult.Status);

    var secondCandidateId = await SeedSecondCandidateInSameDocumentAsync(fixture, projectId);
    fixture.Clock.UtcNow = TestTime.AddMinutes(5);

    var result = await CreateUseCaseWithPolicy(fixture, new HigherAuthorityPolicy()).HandleAsync(
      new PromoteFragmentCandidateCommand(
        secondCandidateId,
        KnowledgeDocumentType.Specification,
        "Concrete Spec",
        "EXT-001",
        "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, result.Status);
    Assert.NotNull(result.KnowledgeDocumentId);
    Assert.True(result.SupersededExistingRevision);

    var supersedesRelationship = fixture.KnowledgeRelationshipRepository.AddedRelationships
      .FirstOrDefault(r => r.RelationshipType == KnowledgeRelationshipType.Supersedes);
    Assert.NotNull(supersedesRelationship);
    Assert.Equal(
      KnowledgeSubjectReference.ForRevision(projectId, result.KnowledgeDocumentRevisionId!.Value),
      supersedesRelationship.Source);
  }

  [Fact]
  public async Task ReplayAfterMutableSourceStateChangeDoesNotMutateKnowledge()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    var documentsAfterFirst = fixture.KnowledgeDocumentRepository.AddedDocuments.Count;
    var revisionsAfterFirst = fixture.KnowledgeRevisionRepository.AddedRevisions.Count;

    fixture.Clock.UtcNow = TestTime.AddHours(1);

    var replayResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(firstResult.PromotionDiagnosticId, replayResult.PromotionDiagnosticId);
    Assert.Equal(documentsAfterFirst, fixture.KnowledgeDocumentRepository.AddedDocuments.Count);
    Assert.Equal(revisionsAfterFirst, fixture.KnowledgeRevisionRepository.AddedRevisions.Count);
  }

  [Fact]
  public async Task WrongTargetReplayProducesDifferentIdentity()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));
    Assert.Equal(PromotionDiagnosticStatus.Promoted, firstResult.Status);

    var secondResult = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Report,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, secondResult.Status);
    Assert.NotEqual(firstResult.PromotionDiagnosticId, secondResult.PromotionDiagnosticId);
    Assert.NotEqual(firstResult.KnowledgeDocumentId, secondResult.KnowledgeDocumentId);
  }

  [Fact]
  public async Task PromotedDiagnosticExcludesSensitiveContentFromResult()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(new PromoteFragmentCandidateCommand(
      candidateId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "EXT-001",
      "Civil"));

    Assert.Equal(PromotionDiagnosticStatus.Promoted, result.Status);
    Assert.Null(result.FailureReason);
    Assert.Equal(PromotionConflictType.None, result.ConflictType);
  }

  [Fact]
  public async Task CancellationExceptionPropagatesAndRecordsNoDiagnostic()
  {
    var fixture = CreateFixture();
    var (candidateId, _, _) = await SeedReadyCandidateAsync(fixture);
    fixture.FragmentCandidateRepository.ThrowOnGetById = true;

    await Assert.ThrowsAsync<OperationCanceledException>(() =>
      CreateUseCase(fixture).HandleAsync(
        new PromoteFragmentCandidateCommand(
          candidateId,
          KnowledgeDocumentType.Specification,
          "Concrete Spec",
          "EXT-001",
          "Civil")));

    Assert.Empty(fixture.PromotionDiagnosticRepository.AddedDiagnostics);
    Assert.Empty(fixture.PromotionAttemptRepository.AddedAttempts);
  }

  private static PromoteFragmentCandidateUseCase CreateUseCaseWithPolicy(
    PromotionFixture fixture, IAuthorityPolicy policy)
  {
    return new PromoteFragmentCandidateUseCase(
      fixture.FragmentCandidateRepository,
      fixture.ParserRunRepository,
      fixture.ImportedSourceRepository,
      fixture.ProjectRepository,
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeCitationRepository,
      fixture.KnowledgeRelationshipRepository,
      fixture.PromotionDiagnosticRepository,
      fixture.PromotionRecordRepository,
      fixture.PromotionAttemptRepository,
      fixture.PromotionProvenanceRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      policy,
      NullLogger<PromoteFragmentCandidateUseCase>.Instance);
  }

  private static async Task<FragmentCandidateId> SeedSecondCandidateInSameDocumentAsync(
    PromotionFixture fixture, ProjectId projectId)
  {
    var sourceId = ImportedSourceId.New();
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      "source-hash-v2",
      "SHA-256",
      2,
      "system",
      TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      projectId,
      sourceId,
      "source-hash-v2",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      2,
      ContentKind.PlainText,
      "Some different text",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);
    return candidate.Id;
  }

  private static async Task<FragmentCandidateId> SeedThirdCandidateInSameDocumentAsync(
    PromotionFixture fixture, ProjectId projectId)
  {
    var sourceId = ImportedSourceId.New();
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      "source-hash-v3",
      "SHA-256",
      3,
      "system",
      TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      projectId,
      sourceId,
      "source-hash-v3",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      3,
      ContentKind.PlainText,
      "Yet another text",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    candidate.Accept("reviewer", TestTime, null);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);
    return candidate.Id;
  }

  private static PromoteFragmentCandidateUseCase CreateUseCase(PromotionFixture fixture)
  {
    return new PromoteFragmentCandidateUseCase(
      fixture.FragmentCandidateRepository,
      fixture.ParserRunRepository,
      fixture.ImportedSourceRepository,
      fixture.ProjectRepository,
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeCitationRepository,
      fixture.KnowledgeRelationshipRepository,
      fixture.PromotionDiagnosticRepository,
      fixture.PromotionRecordRepository,
      fixture.PromotionAttemptRepository,
      fixture.PromotionProvenanceRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      fixture.AuthorityPolicy,
      NullLogger<PromoteFragmentCandidateUseCase>.Instance);
  }

  private static async Task<(FragmentCandidateId CandidateId, string SourceContentHash, ProjectId ProjectId)> SeedReadyCandidateAsync(
    PromotionFixture fixture,
    ProjectLifecycle projectLifecycle = ProjectLifecycle.Active,
    FragmentCandidateReviewState reviewState = FragmentCandidateReviewState.HumanAccepted)
  {
    var projectId = ProjectId.New();
    var project = new Project(projectId, "Test", "system", TestTime);
    if (projectLifecycle == ProjectLifecycle.Active)
    {
      project.Activate("system", TestTime);
    }

    await fixture.ProjectRepository.AddAsync(project);

    var sourceId = ImportedSourceId.New();
    await SeedSourceAsync(fixture, projectId, sourceId);

    var parserRun = new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash",
      "source-hash",
      "SHA-256",
      1,
      "system",
      TestTime);
    parserRun.Start(TestTime);
    parserRun.Complete(TestTime, ParserExecutionStatus.Completed);
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      projectId,
      sourceId,
      "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      1,
      ContentKind.PlainText,
      "Some text",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);

    if (reviewState == FragmentCandidateReviewState.HumanAccepted)
    {
      candidate.Accept("reviewer", TestTime, null);
    }

    await fixture.FragmentCandidateRepository.AddAsync(candidate);
    return (candidate.Id, candidate.SourceContentHash, projectId);
  }

  private static async Task SeedSourceAsync(PromotionFixture fixture, ProjectId projectId, ImportedSourceId sourceId)
  {
    var source = new ImportedDocumentSource(
      sourceId,
      DocumentImportSessionId.New(),
      projectId,
      "test.pdf",
      "application/pdf",
      "application/pdf",
      100,
      "source-hash",
      "SHA-256",
      1,
      new DocumentStorageReference(
        StorageObjectId.New(), "local", "test-key",
        100, "source-hash", "SHA-256", 1, TestTime, null,
        StorageAvailabilityState.Available),
      ImportedSourceOrigin.LocalFile,
      "test@example.invalid",
      TestTime,
      ImportedDocumentSourceStatus.Available,
      null);
    await fixture.ImportedSourceRepository.AddAsync(source);
  }

  private static PromotionFixture CreateFixture()
  {
    var currentUser = new FakeCurrentUser("promoter@example.invalid");
    return new PromotionFixture(
      new FakeProjectRepository(),
      new FakeImportedDocumentSourceRepository(),
      new FakeParserRunRepository(),
      new FakeFragmentCandidateRepository(),
      new FakeKnowledgeDocumentRepository(),
      new FakeKnowledgeRevisionRepository(),
      new FakeKnowledgeCitationRepository(),
      new FakeKnowledgeRelationshipRepository(),
      new FakePromotionDiagnosticRepository(),
      new FakePromotionRecordRepository(),
      new FakePromotionAttemptRepository(),
      new FakePromotionProvenanceRepository(),
      new FakeUnitOfWork(),
      new FakeClock(TestTime),
      currentUser,
      new FakeAuditRecorder(),
      new AuthorityPolicy(currentUser));
  }

  private sealed record PromotionFixture(
    FakeProjectRepository ProjectRepository,
    FakeImportedDocumentSourceRepository ImportedSourceRepository,
    FakeParserRunRepository ParserRunRepository,
    FakeFragmentCandidateRepository FragmentCandidateRepository,
    FakeKnowledgeDocumentRepository KnowledgeDocumentRepository,
    FakeKnowledgeRevisionRepository KnowledgeRevisionRepository,
    FakeKnowledgeCitationRepository KnowledgeCitationRepository,
    FakeKnowledgeRelationshipRepository KnowledgeRelationshipRepository,
    FakePromotionDiagnosticRepository PromotionDiagnosticRepository,
    FakePromotionRecordRepository PromotionRecordRepository,
    FakePromotionAttemptRepository PromotionAttemptRepository,
    FakePromotionProvenanceRepository PromotionProvenanceRepository,
    FakeUnitOfWork UnitOfWork,
    FakeClock Clock,
    FakeCurrentUser CurrentUser,
    FakeAuditRecorder AuditRecorder,
    AuthorityPolicy AuthorityPolicy);
}

internal sealed class HigherAuthorityPolicy : IAuthorityPolicy
{
  public string PolicyVersion => "test-higher-authority";

  public AuthorityPolicyResult Classify(FragmentCandidate candidate, ProjectId projectId)
  {
    return new AuthorityPolicyResult(
      KnowledgeSourceAuthorityLevel.EngineerIssued,
      $"Test higher authority policy for candidate {candidate.Id}.");
  }
}
