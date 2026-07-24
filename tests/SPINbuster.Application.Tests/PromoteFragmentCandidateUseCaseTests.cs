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
    var (candidateId, sourceContentHash) = await SeedReadyCandidateAsync(fixture);

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
    var (candidateId, _) = await SeedReadyCandidateAsync(fixture);

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
    var (candidateId, _) = await SeedReadyCandidateAsync(fixture);

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
    var (candidateId, _) = await SeedReadyCandidateAsync(
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
    var (candidateId, _) = await SeedReadyCandidateAsync(fixture);

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
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      NullLogger<PromoteFragmentCandidateUseCase>.Instance);
  }

  private static async Task<(FragmentCandidateId CandidateId, string SourceContentHash)> SeedReadyCandidateAsync(
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
    return (candidate.Id, candidate.SourceContentHash);
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
      new FakeUnitOfWork(),
      new FakeClock(TestTime),
      new FakeCurrentUser("promoter@example.invalid"),
      new FakeAuditRecorder());
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
    FakeUnitOfWork UnitOfWork,
    FakeClock Clock,
    FakeCurrentUser CurrentUser,
    FakeAuditRecorder AuditRecorder);
}
