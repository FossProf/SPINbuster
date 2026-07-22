using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.AcceptFragmentCandidate;
using SPINbuster.Application.UseCases.LoadFragmentReviewSnapshot;
using SPINbuster.Application.UseCases.RejectFragmentCandidate;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class FragmentReviewUseCaseTests
{
  private static readonly DateTimeOffset TestTime = new(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task AcceptHappyPathTransitionsReviewStateAndStagesAudit()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);

    var result = await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, "Looks good."));

    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, result.ReviewState);
    Assert.Equal(candidateId, result.FragmentCandidateId);
    Assert.Equal("reviewer@example.invalid", result.Reviewer);
    Assert.Equal(TestTime, result.ReviewedAtUtc);
    Assert.Single(fixture.FragmentCandidateRepository.UpdatedCandidates);
    Assert.Single(fixture.AuditRecorder.StagedEvents);
    Assert.Contains(fixture.AuditRecorder.StagedEvents, e => e.EventType == "FragmentCandidateHumanAccepted");
    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task RejectHappyPathTransitionsReviewStateAndStagesAudit()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);

    var result = await CreateRejectUseCase(fixture).HandleAsync(
      new RejectFragmentCandidateCommand(candidateId, "Not relevant."));

    Assert.Equal(FragmentCandidateReviewState.Rejected, result.ReviewState);
    Assert.Equal(candidateId, result.FragmentCandidateId);
    Assert.Equal("reviewer@example.invalid", result.Reviewer);
    Assert.Equal(TestTime, result.ReviewedAtUtc);
    Assert.Single(fixture.FragmentCandidateRepository.UpdatedCandidates);
    Assert.Single(fixture.AuditRecorder.StagedEvents);
    Assert.Contains(fixture.AuditRecorder.StagedEvents, e => e.EventType == "FragmentCandidateRejected");
    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task AcceptDoesNotMutateIdentityOrProvenance()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);
    var candidateBefore = await fixture.FragmentCandidateRepository.GetByIdAsync(candidateId);

    await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, null));

    var candidateAfter = await fixture.FragmentCandidateRepository.GetByIdAsync(candidateId);
    Assert.NotNull(candidateAfter);
    Assert.Equal(candidateBefore!.IdentityKey, candidateAfter.IdentityKey);
    Assert.Equal(candidateBefore.IdentityKeyHash, candidateAfter.IdentityKeyHash);
    Assert.Equal(candidateBefore.SourceContentHash, candidateAfter.SourceContentHash);
    Assert.Equal(candidateBefore.Locator, candidateAfter.Locator);
    Assert.Equal(candidateBefore.ExtractedText, candidateAfter.ExtractedText);
    Assert.Equal(candidateBefore.ParserRunId, candidateAfter.ParserRunId);
    Assert.Equal(candidateBefore.ImportedSourceId, candidateAfter.ImportedSourceId);
  }

  [Fact]
  public async Task RejectDoesNotMutateIdentityOrProvenance()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);
    var candidateBefore = await fixture.FragmentCandidateRepository.GetByIdAsync(candidateId);

    await CreateRejectUseCase(fixture).HandleAsync(
      new RejectFragmentCandidateCommand(candidateId, null));

    var candidateAfter = await fixture.FragmentCandidateRepository.GetByIdAsync(candidateId);
    Assert.NotNull(candidateAfter);
    Assert.Equal(candidateBefore!.IdentityKey, candidateAfter.IdentityKey);
    Assert.Equal(candidateBefore.IdentityKeyHash, candidateAfter.IdentityKeyHash);
    Assert.Equal(candidateBefore.SourceContentHash, candidateAfter.SourceContentHash);
    Assert.Equal(candidateBefore.Locator, candidateAfter.Locator);
    Assert.Equal(candidateBefore.ExtractedText, candidateAfter.ExtractedText);
  }

  [Fact]
  public async Task AcceptFromHumanAcceptedThrowsLifecycleTransitionException()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);
    await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, null));

    await Assert.ThrowsAsync<LifecycleTransitionException>(async () =>
      await CreateAcceptUseCase(fixture).HandleAsync(
        new AcceptFragmentCandidateCommand(candidateId, null)));
  }

  [Fact]
  public async Task RejectFromHumanAcceptedThrowsLifecycleTransitionException()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);
    await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, null));

    await Assert.ThrowsAsync<LifecycleTransitionException>(async () =>
      await CreateRejectUseCase(fixture).HandleAsync(
        new RejectFragmentCandidateCommand(candidateId, null)));
  }

  [Fact]
  public async Task AcceptFromRejectedThrowsLifecycleTransitionException()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);
    await CreateRejectUseCase(fixture).HandleAsync(
      new RejectFragmentCandidateCommand(candidateId, null));

    await Assert.ThrowsAsync<LifecycleTransitionException>(async () =>
      await CreateAcceptUseCase(fixture).HandleAsync(
        new AcceptFragmentCandidateCommand(candidateId, null)));
  }

  [Fact]
  public async Task RejectFromRejectedThrowsLifecycleTransitionException()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);
    await CreateRejectUseCase(fixture).HandleAsync(
      new RejectFragmentCandidateCommand(candidateId, null));

    await Assert.ThrowsAsync<LifecycleTransitionException>(async () =>
      await CreateRejectUseCase(fixture).HandleAsync(
        new RejectFragmentCandidateCommand(candidateId, null)));
  }

  [Fact]
  public async Task AcceptCandidateNotFoundThrowsApplicationEntityNotFoundException()
  {
    var fixture = CreateFixture();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateAcceptUseCase(fixture).HandleAsync(
        new AcceptFragmentCandidateCommand(FragmentCandidateId.New(), null)));
  }

  [Fact]
  public async Task RejectCandidateNotFoundThrowsApplicationEntityNotFoundException()
  {
    var fixture = CreateFixture();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateRejectUseCase(fixture).HandleAsync(
        new RejectFragmentCandidateCommand(FragmentCandidateId.New(), null)));
  }

  [Fact]
  public async Task AcceptCommitFailureDoesNotReportSuccess()
  {
    var fixture = CreateFixture();
    fixture = fixture with { UnitOfWork = new FakeUnitOfWork { ThrowOnCommit = true } };
    var candidateId = await SeedCandidateAsync(fixture);

    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await CreateAcceptUseCase(fixture).HandleAsync(
        new AcceptFragmentCandidateCommand(candidateId, null)));
  }

  [Fact]
  public async Task RejectCommitFailureDoesNotReportSuccess()
  {
    var fixture = CreateFixture();
    fixture = fixture with { UnitOfWork = new FakeUnitOfWork { ThrowOnCommit = true } };
    var candidateId = await SeedCandidateAsync(fixture);

    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await CreateRejectUseCase(fixture).HandleAsync(
        new RejectFragmentCandidateCommand(candidateId, null)));
  }

  [Fact]
  public async Task AcceptAuditStagingOccursBeforeCommit()
  {
    var fixture = CreateFixture();
    var operationLog = new List<string>();
    var auditRecorder = new FakeAuditRecorder(operationLog);
    var unitOfWork = new FakeUnitOfWork(operationLog);
    fixture = fixture with { AuditRecorder = auditRecorder, UnitOfWork = unitOfWork };
    var candidateId = await SeedCandidateAsync(fixture);

    await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, null));

    var auditIndex = operationLog.IndexOf("audit-stage");
    var commitIndex = operationLog.IndexOf("commit");
    Assert.True(auditIndex >= 0, "Audit staging should occur.");
    Assert.True(commitIndex > auditIndex, "Audit staging must occur before commit.");
  }

  [Fact]
  public async Task AcceptNoKnowledgeReportRuleOrAiMutation()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);

    var result = await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, "Accepted."));

    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, result.ReviewState);
    Assert.DoesNotContain(fixture.AuditRecorder.StagedEvents,
      e => e.EventType.Contains("Knowledge") || e.EventType.Contains("Report") || e.EventType.Contains("Rule") || e.EventType.Contains("Ai"));
  }

  [Fact]
  public async Task RejectNoKnowledgeReportRuleOrAiMutation()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);

    var result = await CreateRejectUseCase(fixture).HandleAsync(
      new RejectFragmentCandidateCommand(candidateId, "Rejected."));

    Assert.Equal(FragmentCandidateReviewState.Rejected, result.ReviewState);
    Assert.DoesNotContain(fixture.AuditRecorder.StagedEvents,
      e => e.EventType.Contains("Knowledge") || e.EventType.Contains("Report") || e.EventType.Contains("Rule") || e.EventType.Contains("Ai"));
  }

  [Fact]
  public async Task LoggingVerifiesEventIdsAndScopes()
  {
    var fixture = CreateFixture();
    var logSpy = new LogSpy<AcceptFragmentCandidateUseCase>();
    var candidateId = await SeedCandidateAsync(fixture);

    var useCase = new AcceptFragmentCandidateUseCase(
      fixture.FragmentCandidateRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      logSpy);

    await useCase.HandleAsync(new AcceptFragmentCandidateCommand(candidateId, null));

    Assert.Contains(logSpy.Entries, e => e.EventId == LogEvents.FragmentReviewStarting);
    Assert.Contains(logSpy.Entries, e => e.EventId == LogEvents.FragmentReviewCompleted);
    Assert.DoesNotContain(logSpy.Entries, e => e.Message.Contains("Parsed text content"));
  }

  [Fact]
  public async Task LoggingVerifiesFailedEventIdOnUnexpectedError()
  {
    var fixture = CreateFixture();
    var logSpy = new LogSpy<AcceptFragmentCandidateUseCase>();

    var useCase = new AcceptFragmentCandidateUseCase(
      fixture.FragmentCandidateRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      logSpy);

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await useCase.HandleAsync(new AcceptFragmentCandidateCommand(FragmentCandidateId.New(), null)));

    Assert.Contains(logSpy.Entries, e => e.EventId == LogEvents.FragmentReviewStarting);
    Assert.Contains(logSpy.Entries, e => e.EventId == LogEvents.FragmentReviewFailed);
  }

  [Fact]
  public async Task SnapshotReturnsBoundedEntries()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 100));

    Assert.Single(result.Entries);
    Assert.Equal(1, result.TotalMatchingCount);
  }

  [Fact]
  public async Task SnapshotFiltersByReviewState()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);

    var generated = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, FragmentCandidateReviewState.Generated, null, null, null, 100));
    Assert.Single(generated.Entries);

    var accepted = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, FragmentCandidateReviewState.HumanAccepted, null, null, null, 100));
    Assert.Empty(accepted.Entries);

    await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, null));

    var acceptedAfter = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, FragmentCandidateReviewState.HumanAccepted, null, null, null, 100));
    Assert.Single(acceptedAfter.Entries);
  }

  [Fact]
  public async Task SnapshotFiltersByContentKind()
  {
    var fixture = CreateFixture();
    await SeedCandidateAsync(fixture);

    var plainText = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, ContentKind.PlainText, 100));
    Assert.Single(plainText.Entries);

    var table = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, ContentKind.Table, 100));
    Assert.Empty(table.Entries);
  }

  [Fact]
  public async Task SnapshotReturnsTextPreviewBoundedTo200Characters()
  {
    var fixture = CreateFixture();
    await SeedLongTextCandidateAsync(fixture);

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 100));

    var entry = result.Entries.Single();
    Assert.True(entry.TextPreview.Length <= 203, "Text preview should be at most 200 characters plus '...'");
    Assert.EndsWith("...", entry.TextPreview);
  }

  [Fact]
  public async Task SnapshotReturnsShortTextWithoutEllipsis()
  {
    var fixture = CreateFixture();
    await SeedCandidateAsync(fixture);

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 100));

    var entry = result.Entries.Single();
    Assert.DoesNotContain("...", entry.TextPreview);
    Assert.Equal("Parsed text content.", entry.TextPreview);
  }

  [Fact]
  public async Task SnapshotReturnsCorrectReviewMetadata()
  {
    var fixture = CreateFixture();
    var candidateId = await SeedCandidateAsync(fixture);

    await CreateAcceptUseCase(fixture).HandleAsync(
      new AcceptFragmentCandidateCommand(candidateId, "Accepted."));

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 100));

    var entry = result.Entries.Single();
    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, entry.ReviewState);
    Assert.Equal("reviewer@example.invalid", entry.ReviewedBy);
    Assert.Equal(TestTime, entry.ReviewedAtUtc);
    Assert.Equal("Accepted.", entry.ReviewNotes);
  }

  [Fact]
  public async Task SnapshotDoesNotReturnPhysicalPaths()
  {
    var fixture = CreateFixture();
    await SeedCandidateAsync(fixture);

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 100));

    Assert.All(result.Entries, entry =>
    {
      Assert.DoesNotContain("\\", entry.LocatorValue);
      Assert.DoesNotContain("/", entry.NormalizedLocator);
    });
  }

  [Fact]
  public async Task SnapshotReturnsParserRunScope()
  {
    var fixture = CreateFixture();
    await SeedCandidateAsync(fixture);

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 100));

    var entry = result.Entries.Single();
    Assert.NotEqual(ParserRunId.New(), entry.ParserRunId);
  }

  [Fact]
  public async Task SnapshotReturnsIdentityKeyHash()
  {
    var fixture = CreateFixture();
    await SeedCandidateAsync(fixture);

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 100));

    var entry = result.Entries.Single();
    Assert.False(string.IsNullOrWhiteSpace(entry.IdentityKeyHash));
    Assert.Equal(64, entry.IdentityKeyHash.Length);
  }

  [Fact]
  public async Task SnapshotMaxResultsBoundsOutput()
  {
    var fixture = CreateFixture();
    await SeedCandidateAsync(fixture);

    var result = await CreateSnapshotUseCase(fixture).HandleAsync(
      new LoadFragmentReviewSnapshotQuery(fixture.ProjectId, null, null, null, null, 0));

    Assert.Empty(result.Entries);
  }

  private static async Task<FragmentCandidateId> SeedCandidateAsync(FragmentReviewFixture fixture)
  {
    var sourceId = await SeedSourceAsync(fixture);
    var parserRun = new ParserRun(
      ParserRunId.New(),
      fixture.ProjectId,
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
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      fixture.ProjectId,
      sourceId,
      "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      1,
      ContentKind.PlainText,
      "Parsed text content.",
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);
    return candidate.Id;
  }

  private static async Task<FragmentCandidateId> SeedLongTextCandidateAsync(FragmentReviewFixture fixture)
  {
    var sourceId = await SeedSourceAsync(fixture);
    var parserRun = new ParserRun(
      ParserRunId.New(),
      fixture.ProjectId,
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
    await fixture.ParserRunRepository.AddAsync(parserRun);

    var longText = new string('x', 500);
    var candidate = new FragmentCandidate(
      FragmentCandidateId.New(),
      parserRun.Id,
      fixture.ProjectId,
      sourceId,
      "source-hash",
      new FragmentLocator(FragmentLocatorType.WholeDocument, "*"),
      1,
      ContentKind.PlainText,
      longText,
      ConfidenceBand.High,
      "test-parser",
      "1.0.0",
      TestTime);
    await fixture.FragmentCandidateRepository.AddAsync(candidate);
    return candidate.Id;
  }

  private static async Task<ImportedSourceId> SeedSourceAsync(FragmentReviewFixture fixture)
  {
    var sourceId = ImportedSourceId.New();
    var source = new ImportedDocumentSource(
      sourceId,
      DocumentImportSessionId.New(),
      fixture.ProjectId,
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
    return sourceId;
  }

  private static AcceptFragmentCandidateUseCase CreateAcceptUseCase(FragmentReviewFixture fixture)
  {
    return new AcceptFragmentCandidateUseCase(
      fixture.FragmentCandidateRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      NullLogger<AcceptFragmentCandidateUseCase>.Instance);
  }

  private static RejectFragmentCandidateUseCase CreateRejectUseCase(FragmentReviewFixture fixture)
  {
    return new RejectFragmentCandidateUseCase(
      fixture.FragmentCandidateRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      NullLogger<RejectFragmentCandidateUseCase>.Instance);
  }

  private static LoadFragmentReviewSnapshotUseCase CreateSnapshotUseCase(FragmentReviewFixture fixture)
  {
    return new LoadFragmentReviewSnapshotUseCase(
      fixture.FragmentCandidateRepository,
      fixture.ParserRunRepository,
      fixture.ParserDiagnosticRepository,
      NullLogger<LoadFragmentReviewSnapshotUseCase>.Instance);
  }

  private static FragmentReviewFixture CreateFixture(List<string>? operationLog = null)
  {
    var projectId = ProjectId.New();
    var project = new Project(projectId, "Test Project", "test@example.invalid", TestTime);
    var projectRepository = new FakeProjectRepository();
    projectRepository.AddAsync(project).GetAwaiter().GetResult();

    return new FragmentReviewFixture(
      projectId,
      projectRepository,
      new FakeImportedDocumentSourceRepository(),
      new FakeParserRunRepository(),
      new FakeFragmentCandidateRepository(),
      new FakeParserDiagnosticRepository(),
      new FakeUnitOfWork(operationLog),
      new FakeClock(TestTime),
      new FakeCurrentUser("reviewer@example.invalid"),
      new FakeAuditRecorder(operationLog));
  }

  private sealed record FragmentReviewFixture(
    ProjectId ProjectId,
    FakeProjectRepository ProjectRepository,
    FakeImportedDocumentSourceRepository ImportedSourceRepository,
    FakeParserRunRepository ParserRunRepository,
    FakeFragmentCandidateRepository FragmentCandidateRepository,
    FakeParserDiagnosticRepository ParserDiagnosticRepository,
    FakeUnitOfWork UnitOfWork,
    FakeClock Clock,
    FakeCurrentUser CurrentUser,
    FakeAuditRecorder AuditRecorder);
}
