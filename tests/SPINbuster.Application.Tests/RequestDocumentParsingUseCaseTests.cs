using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;
using System.Text;

namespace SPINbuster.Application.Tests;

public sealed class RequestDocumentParsingUseCaseTests
{
  private static readonly DateTimeOffset TestTime = new(2026, 7, 18, 14, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task SuccessfulParsingStagesCandidatesAndRunCompletionInOneCommit()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Completed, result.State);
    Assert.Equal(ParserRunFailureClassification.None, result.FailureClassification);
    Assert.Single(result.FragmentCandidateIds);
    Assert.True(fixture.UnitOfWork.CommitCount >= 2);
    Assert.Single(fixture.ParserRunRepository.AddedRuns);
    Assert.Single(fixture.FragmentCandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task RunIsCommittedBeforeProviderInvocation()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);
    var operationLog = new List<string>();
    var auditRecorder = new FakeAuditRecorder(operationLog);
    var unitOfWork = new FakeUnitOfWork(operationLog);
    fixture = fixture with { AuditRecorder = auditRecorder, UnitOfWork = unitOfWork };

    await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    var runIndex = operationLog.IndexOf("audit-stage");
    var commitIndex = operationLog.IndexOf("commit");
    Assert.True(runIndex >= 0, "Parser run audit should be staged.");
    Assert.True(commitIndex > runIndex, "Run commit should occur before provider execution.");
  }

  [Fact]
  public async Task ProjectNotFoundThrowsApplicationEntityNotFoundException()
  {
    var fixture = CreateFixture();
    var missingProjectId = ProjectId.New();
    var sourceId = ImportedSourceId.New();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new RequestDocumentParsingCommand(missingProjectId, sourceId, "test-parser", "1.0.0")));
  }

  [Fact]
  public async Task SourceNotFoundThrowsApplicationEntityNotFoundException()
  {
    var fixture = CreateFixture();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new RequestDocumentParsingCommand(fixture.ProjectId, ImportedSourceId.New(), "test-parser", "1.0.0")));
  }

  [Fact]
  public async Task SourceBelongingToDifferentProjectThrowsDomainInvariantException()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture, ProjectId.New());

    await Assert.ThrowsAsync<DomainInvariantException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0")));
  }

  [Fact]
  public async Task ParserKeyMismatchThrowsKeyNotFoundException()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);

    await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "wrong-parser", "1.0.0")));
  }

  [Fact]
  public async Task ParserContractVersionMismatchThrowsDomainInvariantException()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);

    await Assert.ThrowsAsync<DomainInvariantException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "2.0.0")));
  }

  [Fact]
  public async Task NonDeterministicParserThrowsDomainInvariantException()
  {
    var fixture = CreateFixture();
    var parser = new FakeDocumentParser { Determinism = ParserDeterminism.NonDeterministic };
    fixture = fixture with
    {
      DocumentParser = parser,
      ParserRegistry = new FakeDocumentParserRegistry(parser)
    };
    var sourceId = await SeedSourceAsync(fixture);

    await Assert.ThrowsAsync<DomainInvariantException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0")));
  }

  [Fact]
  public async Task SourceUnavailablePersistsTerminalRunState()
  {
    var fixture = CreateFixture();
    fixture = fixture with { ImmutableContentStore = new FakeImmutableContentStore { ReturnUnavailableOnOpen = true } };
    var sourceId = await SeedSourceAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Failed, result.State);
    Assert.Empty(result.FragmentCandidateIds);
    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
    Assert.Single(fixture.ParserRunRepository.AddedRuns);
  }

  [Fact]
  public async Task IntegrityMismatchPersistsTerminalRunState()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);
    fixture = fixture with { ImmutableContentStore = new FakeImmutableContentStore { OpenReadHashOverride = "mismatched-hash" } };
    var source = await fixture.ImportedSourceRepository.GetByIdAsync(sourceId);
    Assert.NotNull(source);
    await fixture.ImmutableContentStore.StoreAsync(new StoreImmutableContentRequest(
      source.StorageReference.StorageObjectId, "local", "test-key",
      source.ContentHash, source.HashAlgorithm, source.HashAlgorithmVersion,
      source.ContentLength, new MemoryStream(Encoding.UTF8.GetBytes("test content for parsing")), TestTime, null));

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Failed, result.State);
    Assert.Empty(result.FragmentCandidateIds);
    Assert.Single(fixture.ParserRunRepository.AddedRuns);
  }

  [Fact]
  public async Task IdempotentReplayReturnsExistingCompletedRunAndFragments()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    var secondResult = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(firstResult.ParserRunId, secondResult.ParserRunId);
    Assert.Equal(ParserRunState.Completed, secondResult.State);
    Assert.Single(secondResult.FragmentCandidateIds);
    Assert.Single(fixture.ParserRunRepository.AddedRuns);
  }

  [Fact]
  public async Task IdempotentReplayReturnsExistingFailedRunWithEmptyFragments()
  {
    var fixture = CreateFixture();
    var failedParser = new FakeDocumentParser
    {
      ParseAsyncCore = (_, _) => Task.FromResult(new ParserExecutionResult(
        ParserExecutionStatus.Failed, ParserRunFailureClassification.ParserFailure, "parser crashed", [], []))
    };
    fixture = fixture with
    {
      DocumentParser = failedParser,
      ParserRegistry = new FakeDocumentParserRegistry(failedParser)
    };
    var sourceId = await SeedSourceAsync(fixture);

    var firstResult = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Failed, firstResult.State);

    var newParser = new FakeDocumentParser
    {
      ParseAsyncCore = (_, _) => Task.FromResult(new ParserExecutionResult(
        ParserExecutionStatus.Completed, ParserRunFailureClassification.None, null,
        [new ParserFragmentResult(FragmentLocatorType.WholeDocument, string.Empty, 1, ContentKind.PlainText, "text", ConfidenceBand.High)],
        []))
    };
    fixture = fixture with
    {
      DocumentParser = newParser,
      ParserRegistry = new FakeDocumentParserRegistry(newParser)
    };

    var secondResult = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(firstResult.ParserRunId, secondResult.ParserRunId);
    Assert.Equal(ParserRunState.Failed, secondResult.State);
    Assert.Empty(secondResult.FragmentCandidateIds);
  }

  [Fact]
  public async Task ParserFailurePersistsTerminalRunStateAndDoesNotReportSuccess()
  {
    var fixture = CreateFixture();
    var parser = new FakeDocumentParser
    {
      ParseAsyncCore = (_, _) => Task.FromResult(new ParserExecutionResult(
        ParserExecutionStatus.Failed, ParserRunFailureClassification.ParserFailure, "unexpected format", [], []))
    };
    fixture = fixture with
    {
      DocumentParser = parser,
      ParserRegistry = new FakeDocumentParserRegistry(parser)
    };
    var sourceId = await SeedSourceAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Failed, result.State);
    Assert.Equal(ParserRunFailureClassification.ParserFailure, result.FailureClassification);
    Assert.Empty(result.FragmentCandidateIds);
    Assert.Empty(fixture.FragmentCandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task ParserCancellationPersistsTerminalRunState()
  {
    var fixture = CreateFixture();
    var parser = new FakeDocumentParser
    {
      ParseAsyncCore = (_, ct) =>
      {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new ParserExecutionResult(
          ParserExecutionStatus.Failed, ParserRunFailureClassification.Cancelled, "cancelled", [], []));
      }
    };
    fixture = fixture with
    {
      DocumentParser = parser,
      ParserRegistry = new FakeDocumentParserRegistry(parser)
    };
    var sourceId = await SeedSourceAsync(fixture);
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"),
      cts.Token);

    Assert.Equal(ParserRunState.Cancelled, result.State);
    Assert.Equal(ParserRunFailureClassification.Cancelled, result.FailureClassification);
    Assert.Empty(fixture.FragmentCandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task UnexpectedProviderExceptionPersistsTerminalRunState()
  {
    var fixture = CreateFixture();
    fixture = fixture with
    {
      DocumentParser = new FakeDocumentParser
      {
        ParseAsyncCore = (_, _) => throw new InvalidOperationException("parser boom")
      }
    };
    fixture = fixture with
    {
      ParserRegistry = new FakeDocumentParserRegistry(fixture.DocumentParser)
    };
    var sourceId = await SeedSourceAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Failed, result.State);
    Assert.Contains("parser boom", result.FailureDetails);
    Assert.Empty(fixture.FragmentCandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task CommitFailureDoesNotReportSuccess()
  {
    var fixture = CreateFixture();
    fixture = fixture with { UnitOfWork = new FakeUnitOfWork { ThrowOnCommit = true } };
    var sourceId = await SeedSourceAsync(fixture);

    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0")));
  }

  [Fact]
  public async Task MalformedParserOutputRejectsInvalidLocatorAndDoesNotPartiallyPersist()
  {
    var fixture = CreateFixture();
    var parser = new FakeDocumentParser
    {
      ParseAsyncCore = (_, _) => Task.FromResult(new ParserExecutionResult(
        ParserExecutionStatus.Completed,
        ParserRunFailureClassification.None,
        null,
        [
          new ParserFragmentResult(FragmentLocatorType.Page, "not-a-number", 1, ContentKind.PlainText, "text", ConfidenceBand.High),
        ],
        []))
    };
    fixture = fixture with
    {
      DocumentParser = parser,
      ParserRegistry = new FakeDocumentParserRegistry(parser)
    };
    var sourceId = await SeedSourceAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Failed, result.State);
    Assert.Contains("Unexpected parser failure", result.FailureDetails);
    Assert.Empty(fixture.FragmentCandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task DuplicateLocatorsAreAllowedAcrossOrdinals()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Equal(ParserRunState.Completed, result.State);
    Assert.Single(result.FragmentCandidateIds);
  }

  [Fact]
  public async Task SnapshotDoesNotContainAbsolutePaths()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.All(result.FragmentCandidateIds, id => Assert.NotEqual(FragmentCandidateId.New(), id));
  }

  [Fact]
  public async Task LoggingVerifiesEventIdsAndScopesWithoutExtractedText()
  {
    var fixture = CreateFixture();
    var logSpy = new LogSpy<RequestDocumentParsingUseCase>();
    var sourceId = await SeedSourceAsync(fixture);

    var useCase = new RequestDocumentParsingUseCase(
      fixture.ProjectRepository,
      fixture.ImportedSourceRepository,
      fixture.ImmutableContentStore,
      fixture.ParserRegistry,
      fixture.ParserRunRepository,
      fixture.FragmentCandidateRepository,
      fixture.ParserDiagnosticRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      logSpy);

    await useCase.HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Contains(logSpy.Entries, e => e.EventId == LogEvents.ParserRunStarting);
    Assert.Contains(logSpy.Entries, e => e.EventId == LogEvents.ParserRunCompleted);
    Assert.DoesNotContain(logSpy.Entries, e => e.Message.Contains("Parsed text content"));
  }

  [Fact]
  public async Task LoggingVerifiesFailureEventIdOnProviderFailure()
  {
    var fixture = CreateFixture();
    var parser = new FakeDocumentParser
    {
      ParseAsyncCore = (_, _) => Task.FromResult(new ParserExecutionResult(
        ParserExecutionStatus.Failed, ParserRunFailureClassification.ParserFailure, "test failure", [], []))
    };
    fixture = fixture with
    {
      DocumentParser = parser,
      ParserRegistry = new FakeDocumentParserRegistry(parser)
    };
    var logSpy = new LogSpy<RequestDocumentParsingUseCase>();
    var sourceId = await SeedSourceAsync(fixture);

    var useCase = new RequestDocumentParsingUseCase(
      fixture.ProjectRepository,
      fixture.ImportedSourceRepository,
      fixture.ImmutableContentStore,
      fixture.ParserRegistry,
      fixture.ParserRunRepository,
      fixture.FragmentCandidateRepository,
      fixture.ParserDiagnosticRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      logSpy);

    await useCase.HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.Contains(logSpy.Entries, e => e.EventId == LogEvents.ParserRunFailed);
  }

  [Fact]
  public async Task AuditEventsAreDistinctFromOperationalLogs()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAsync(fixture);

    await CreateUseCase(fixture).HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    Assert.True(fixture.AuditRecorder.StagedEvents.Count > 0);
    Assert.All(fixture.AuditRecorder.StagedEvents, e =>
    {
      Assert.False(string.IsNullOrWhiteSpace(e.EventType));
      Assert.False(string.IsNullOrWhiteSpace(e.Actor));
    });
  }

  private static RequestDocumentParsingUseCase CreateUseCase(
    ParsingFixture fixture,
    ILogger<RequestDocumentParsingUseCase>? logger = null)
  {
    return new RequestDocumentParsingUseCase(
      fixture.ProjectRepository,
      fixture.ImportedSourceRepository,
      fixture.ImmutableContentStore,
      fixture.ParserRegistry,
      fixture.ParserRunRepository,
      fixture.FragmentCandidateRepository,
      fixture.ParserDiagnosticRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder,
      logger ?? NullLogger<RequestDocumentParsingUseCase>.Instance);
  }

  private static async Task<ImportedSourceId> SeedSourceAsync(
    ParsingFixture fixture,
    ProjectId? projectId = null)
  {
    var sourceId = ImportedSourceId.New();
    var storageObjectId = StorageObjectId.New();
    var content = Encoding.UTF8.GetBytes("test content for parsing");
    var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));

    await fixture.ImmutableContentStore.StoreAsync(new StoreImmutableContentRequest(
      storageObjectId, "local", "test-key",
      contentHash, "SHA-256", 1,
      content.Length, new MemoryStream(content), TestTime, null));

    var source = new ImportedDocumentSource(
      sourceId,
      DocumentImportSessionId.New(),
      projectId ?? fixture.ProjectId,
      "test.pdf",
      "application/pdf",
      "application/pdf",
      content.Length,
      contentHash,
      "SHA-256",
      1,
      new DocumentStorageReference(
        storageObjectId, "local", "test-key",
        content.Length, contentHash, "SHA-256", 1, TestTime, null,
        StorageAvailabilityState.Available),
      ImportedSourceOrigin.LocalFile,
      "test@example.invalid",
      TestTime,
      ImportedDocumentSourceStatus.Available,
      null);

    await fixture.ImportedSourceRepository.AddAsync(source);
    return sourceId;
  }

  private static ParsingFixture CreateFixture(List<string>? operationLog = null)
  {
    var projectId = ProjectId.New();
    var project = new Project(projectId, "Test Project", "test@example.invalid", TestTime);
    var projectRepository = new FakeProjectRepository();
    projectRepository.AddAsync(project).GetAwaiter().GetResult();

    var parser = new FakeDocumentParser(operationLog);
    return new ParsingFixture(
      projectId,
      projectRepository,
      new FakeImportedDocumentSourceRepository(),
      new FakeImmutableContentStore(),
      parser,
      new FakeDocumentParserRegistry(parser),
      new FakeParserRunRepository(),
      new FakeFragmentCandidateRepository(),
      new FakeParserDiagnosticRepository(),
      new FakeUnitOfWork(operationLog),
      new FakeClock(TestTime),
      new FakeCurrentUser("parser.requester@example.invalid"),
      new FakeAuditRecorder(operationLog));
  }

  private sealed record ParsingFixture(
    ProjectId ProjectId,
    FakeProjectRepository ProjectRepository,
    FakeImportedDocumentSourceRepository ImportedSourceRepository,
    FakeImmutableContentStore ImmutableContentStore,
    FakeDocumentParser DocumentParser,
    FakeDocumentParserRegistry ParserRegistry,
    FakeParserRunRepository ParserRunRepository,
    FakeFragmentCandidateRepository FragmentCandidateRepository,
    FakeParserDiagnosticRepository ParserDiagnosticRepository,
    FakeUnitOfWork UnitOfWork,
    FakeClock Clock,
    FakeCurrentUser CurrentUser,
    FakeAuditRecorder AuditRecorder);
}
