using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;
using System.Text;

namespace SPINbuster.Application.Tests;

public sealed class LoadParsingSnapshotUseCaseTests
{
  private static readonly DateTimeOffset TestTime = new(2026, 7, 18, 14, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task SnapshotReturnsSourceIdentityAndParserRunsWithoutPaths()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAndRunAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadParsingSnapshotQuery(fixture.ProjectId, sourceId));

    Assert.Equal(sourceId, result.ImportedSourceId);
    Assert.Equal(fixture.ProjectId, result.ProjectId);
    Assert.Single(result.ParserRuns);
    Assert.DoesNotContain(result.ParserRuns, r => r.FragmentCandidates.Any(c => c.LocatorValue.Contains('\\')));
  }

  [Fact]
  public async Task SnapshotReturnsBoundedResults()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAndRunAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadParsingSnapshotQuery(fixture.ProjectId, sourceId));

    Assert.True(result.ParserRuns.Count <= 100);
    Assert.All(result.ParserRuns, r =>
    {
      Assert.True(r.FragmentCandidates.Count <= 10_000);
      Assert.True(r.AuditHistory.Count <= 500);
    });
  }

  [Fact]
  public async Task SnapshotIncludesAuditHistory()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAndRunAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadParsingSnapshotQuery(fixture.ProjectId, sourceId));

    var run = result.ParserRuns.Single();
    Assert.True(run.AuditHistory.Count > 0);
    Assert.All(run.AuditHistory, a =>
    {
      Assert.False(string.IsNullOrWhiteSpace(a.EventType));
      Assert.False(string.IsNullOrWhiteSpace(a.Actor));
      Assert.False(string.IsNullOrWhiteSpace(a.Description));
    });
  }

  [Fact]
  public async Task SnapshotIncludesFragmentCandidateDetails()
  {
    var fixture = CreateFixture();
    var sourceId = await SeedSourceAndRunAsync(fixture);

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadParsingSnapshotQuery(fixture.ProjectId, sourceId));

    var candidate = result.ParserRuns.Single().FragmentCandidates.Single();
    Assert.Equal(FragmentLocatorType.WholeDocument, candidate.LocatorType);
    Assert.Equal(1, candidate.Ordinal);
    Assert.Equal(ContentKind.PlainText, candidate.ContentKind);
    Assert.Equal(20, candidate.TextLength);
    Assert.False(string.IsNullOrWhiteSpace(candidate.IdentityKeyHash));
  }

  [Fact]
  public async Task ProjectNotFoundThrowsApplicationEntityNotFoundException()
  {
    var fixture = CreateFixture();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new LoadParsingSnapshotQuery(ProjectId.New(), ImportedSourceId.New())));
  }

  [Fact]
  public async Task SourceNotFoundThrowsApplicationEntityNotFoundException()
  {
    var fixture = CreateFixture();

    await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new LoadParsingSnapshotQuery(fixture.ProjectId, ImportedSourceId.New())));
  }

  [Fact]
  public async Task SourceBelongingToDifferentProjectThrowsDomainInvariantException()
  {
    var fixture = CreateFixture();
    var otherProjectId = ProjectId.New();
    var otherProject = new Project(otherProjectId, "Other", "test@example.invalid", TestTime);
    await fixture.ProjectRepository.AddAsync(otherProject);
    var sourceId = ImportedSourceId.New();
    var content = Encoding.UTF8.GetBytes("test content");
    var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
    var storageObjectId = StorageObjectId.New();

    await fixture.ImmutableContentStore.StoreAsync(new StoreImmutableContentRequest(
      storageObjectId, "local", "key", contentHash, "SHA-256", 1,
      content.Length, new MemoryStream(content), TestTime, null));

    await fixture.ImportedSourceRepository.AddAsync(new ImportedDocumentSource(
      sourceId, DocumentImportSessionId.New(), otherProjectId,
      "test.pdf", "application/pdf", "application/pdf", content.Length,
      contentHash, "SHA-256", 1,
      new DocumentStorageReference(storageObjectId, "local", "key",
        content.Length, contentHash, "SHA-256", 1, TestTime, null, StorageAvailabilityState.Available),
      ImportedSourceOrigin.LocalFile, "test@example.invalid", TestTime,
      ImportedDocumentSourceStatus.Available, null));

    await Assert.ThrowsAsync<DomainInvariantException>(async () =>
      await CreateUseCase(fixture).HandleAsync(
        new LoadParsingSnapshotQuery(fixture.ProjectId, sourceId)));
  }

  [Fact]
  public async Task EmptySnapshotReturnsEmptyParserRuns()
  {
    var fixture = CreateFixture();
    var sourceId = ImportedSourceId.New();
    var content = Encoding.UTF8.GetBytes("test content");
    var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
    var storageObjectId = StorageObjectId.New();

    await fixture.ImmutableContentStore.StoreAsync(new StoreImmutableContentRequest(
      storageObjectId, "local", "key", contentHash, "SHA-256", 1,
      content.Length, new MemoryStream(content), TestTime, null));

    await fixture.ImportedSourceRepository.AddAsync(new ImportedDocumentSource(
      sourceId, DocumentImportSessionId.New(), fixture.ProjectId,
      "test.pdf", "application/pdf", "application/pdf", content.Length,
      contentHash, "SHA-256", 1,
      new DocumentStorageReference(storageObjectId, "local", "key",
        content.Length, contentHash, "SHA-256", 1, TestTime, null, StorageAvailabilityState.Available),
      ImportedSourceOrigin.LocalFile, "test@example.invalid", TestTime,
      ImportedDocumentSourceStatus.Available, null));

    var result = await CreateUseCase(fixture).HandleAsync(
      new LoadParsingSnapshotQuery(fixture.ProjectId, sourceId));

    Assert.Empty(result.ParserRuns);
    Assert.Equal(sourceId, result.ImportedSourceId);
  }

  private static async Task<ImportedSourceId> SeedSourceAndRunAsync(ParsingFixture fixture)
  {
    var content = Encoding.UTF8.GetBytes("test content for parsing");
    var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
    var storageObjectId = StorageObjectId.New();
    var sourceId = ImportedSourceId.New();

    await fixture.ImmutableContentStore.StoreAsync(new StoreImmutableContentRequest(
      storageObjectId, "local", "test-key", contentHash, "SHA-256", 1,
      content.Length, new MemoryStream(content), TestTime, null));

    await fixture.ImportedSourceRepository.AddAsync(new ImportedDocumentSource(
      sourceId, DocumentImportSessionId.New(), fixture.ProjectId,
      "test.pdf", "application/pdf", "application/pdf", content.Length,
      contentHash, "SHA-256", 1,
      new DocumentStorageReference(storageObjectId, "local", "test-key",
        content.Length, contentHash, "SHA-256", 1, TestTime, null, StorageAvailabilityState.Available),
      ImportedSourceOrigin.LocalFile, "test@example.invalid", TestTime,
      ImportedDocumentSourceStatus.Available, null));

    var parseUseCase = new RequestDocumentParsingUseCase(
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
      NullLogger<RequestDocumentParsingUseCase>.Instance);

    await parseUseCase.HandleAsync(
      new RequestDocumentParsingCommand(fixture.ProjectId, sourceId, "test-parser", "1.0.0"));

    return sourceId;
  }

  private static LoadParsingSnapshotUseCase CreateUseCase(ParsingFixture fixture)
  {
    return new LoadParsingSnapshotUseCase(
      fixture.ProjectRepository,
      fixture.ImportedSourceRepository,
      fixture.ParserRunRepository,
      fixture.FragmentCandidateRepository,
      fixture.ParserDiagnosticRepository,
      NullLogger<LoadParsingSnapshotUseCase>.Instance);
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
