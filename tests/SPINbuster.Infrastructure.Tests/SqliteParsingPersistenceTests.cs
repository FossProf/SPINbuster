using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Repositories;
using SPINbuster.Infrastructure.Services;
using System.Globalization;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqliteParsingPersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task FreshMigrationCreatesParserTables()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();

    Assert.Contains(appliedMigrations, migration => migration.EndsWith("ParsingFoundationSlice", StringComparison.Ordinal));
    Assert.Equal(appliedMigrations.Length, await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM __EFMigrationsHistory"));

    var runCount = await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM parser_runs");
    var candidateCount = await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM parser_fragment_candidates");
    Assert.Equal(0L, runCount);
    Assert.Equal(0L, candidateCount);
  }

  [Fact]
  public async Task UpgradeMigrationIsIdempotent()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    Assert.Contains(appliedMigrations, migration => migration.EndsWith("ParsingFoundationSlice", StringComparison.Ordinal));
  }

  [Fact]
  public async Task ParserRunAndFragmentCandidatePersistAndReload()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2));
      runId = run.Id;

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedRun = await new SqliteParserRunRepository(verificationContext).GetByIdAsync(runId);
    var storedCandidates = await new SqliteFragmentCandidateRepository(verificationContext).GetByParserRunAsync(runId, 100);

    Assert.NotNull(storedRun);
    Assert.Equal(ParserRunState.Completed, storedRun!.State);
    Assert.Equal(seeded.ProjectId, storedRun.ProjectId);
    Assert.Equal(seeded.SourceId, storedRun.ImportedSourceId);
    Assert.Single(storedCandidates);
    Assert.Equal(ContentKind.PlainText, storedCandidates.First().ContentKind);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM parser_runs"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM parser_fragment_candidates"));
  }

  [Fact]
  public async Task DuplicateReplayKeyViolatesUniqueConstraint()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run1 = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run1);
      StageAuditEvents(auditRecorder, run1.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run2 = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      await new SqliteParserRunRepository(dbContext).AddAsync(run2);

      await Assert.ThrowsAsync<DbUpdateException>(async () =>
      {
        StageAuditEvents(auditRecorder, run2.AuditTrail);
        await unitOfWork.CommitAsync();
      });
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM parser_runs"));
  }

  [Fact]
  public async Task BoundedQueriesRespectMaxResults()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      for (var i = 0; i < 5; i++)
      {
        var run = new ParserRun(
          ParserRunId.New(),
          seeded.ProjectId,
          seeded.SourceId,
          $"parser-{i}",
          "1.0.0",
          "1.0.0",
          "contract-hash",
          seeded.ContentHash,
          "SHA-256",
          1,
          "test@example.invalid",
          createdAt.AddMinutes(i));
        await new SqliteParserRunRepository(dbContext).AddAsync(run);
        StageAuditEvents(auditRecorder, run.AuditTrail);
      }

      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var runs = await new SqliteParserRunRepository(verificationContext).GetByProjectAsync(seeded.ProjectId, 3);
    Assert.Equal(3, runs.Count);

    var allRuns = await new SqliteParserRunRepository(verificationContext).GetByProjectAsync(seeded.ProjectId, 100);
    Assert.Equal(5, allRuns.Count);
  }

  [Fact]
  public async Task CancellationStatePersistedCorrectly()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Cancel(createdAt.AddMinutes(2), "User requested cancellation.");
      runId = run.Id;
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedRun = await new SqliteParserRunRepository(verificationContext).GetByIdAsync(runId);
    Assert.NotNull(storedRun);
    Assert.Equal(ParserRunState.Cancelled, storedRun!.State);
    Assert.Contains("cancellation", storedRun.FailureReason!, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task FailedStatePersistedCorrectly()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Fail(createdAt.AddMinutes(2), "Parser encountered invalid format.");
      runId = run.Id;
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedRun = await new SqliteParserRunRepository(verificationContext).GetByIdAsync(runId);
    Assert.NotNull(storedRun);
    Assert.Equal(ParserRunState.Failed, storedRun!.State);
    Assert.Contains("invalid format", storedRun.FailureReason!, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void FragmentCandidateIdentityIsDeterministic()
  {
    var locator1 = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var locator2 = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    var sourceId = ImportedSourceId.New();

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator1);
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator2);

    Assert.Equal(key1, key2);
    Assert.Contains("WholeDocument", key1, StringComparison.Ordinal);
  }

  [Fact]
  public async Task FragmentCandidatePersistAndReloadPreservesLocator()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var runId = ParserRunId.New();

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      await unitOfWork.CommitAsync();
      runId = run.Id;
    }

    var locator = new FragmentLocator(FragmentLocatorType.LineRange, "1-50");
    var candidate = CreateFragmentCandidateWithLocator(runId, seeded.ProjectId, seeded.SourceId, locator, createdAt);

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedCandidates = await new SqliteFragmentCandidateRepository(verificationContext).GetByParserRunAsync(runId, 100);
    Assert.Single(storedCandidates);
    var stored = storedCandidates.First();
    Assert.Equal(FragmentLocatorType.LineRange, stored.Locator.LocatorType);
    Assert.Equal("1-50", stored.Locator.RawValue);
  }

  [Fact]
  public async Task GetByImportedSourceReturnsOrderedResults()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      for (var i = 0; i < 3; i++)
      {
        var run = new ParserRun(
          ParserRunId.New(),
          seeded.ProjectId,
          seeded.SourceId,
          $"parser-order-{i}",
          "1.0.0",
          "1.0.0",
          "contract-hash",
          seeded.ContentHash,
          "SHA-256",
          1,
          "test@example.invalid",
          createdAt.AddMinutes(i));
        await new SqliteParserRunRepository(dbContext).AddAsync(run);
        StageAuditEvents(auditRecorder, run.AuditTrail);
      }

      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var runs = await new SqliteParserRunRepository(verificationContext).GetByImportedSourceAsync(seeded.SourceId, 100);
    Assert.Equal(3, runs.Count);
    var ordered = runs.ToArray();
    for (var i = 1; i < ordered.Length; i++)
    {
      Assert.True(ordered[i].CreatedAtUtc >= ordered[i - 1].CreatedAtUtc);
    }
  }

  public void Dispose()
  {
    try
    {
      if (File.Exists(_databasePath))
      {
        File.Delete(_databasePath);
      }
    }
    catch (IOException)
    {
    }
  }

  private SpinbusterDbContext CreateDbContext()
  {
    Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
    var options = new DbContextOptionsBuilder<SpinbusterDbContext>()
      .UseSqlite($"Data Source={_databasePath}")
      .EnableSensitiveDataLogging()
      .Options;
    return new SpinbusterDbContext(options);
  }

  private async Task<SeededParsingContext> SeedSourceAsync()
  {
    var createdAtUtc = new DateTimeOffset(2026, 7, 19, 9, 0, 0, TimeSpan.Zero);
    var content = System.Text.Encoding.UTF8.GetBytes("Hello, world!");
    var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));

    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

    var project = new Project(ProjectId.New(), "Parsing Test Project", "test@example.invalid", createdAtUtc);
    project.Activate("test@example.invalid", createdAtUtc.AddMinutes(1));
    await new SqliteProjectRepository(dbContext).AddAsync(project);
    StageAuditEvents(auditRecorder, project.AuditTrail);

    var storageObject = new StorageObject(
      StorageObjectId.New(),
      "document-engine-foundation",
      "storage-1",
      content.Length,
      contentHash,
      "SHA-256",
      1,
      createdAtUtc.AddMinutes(2),
      null,
      StorageAvailabilityState.Available);
    await new SqliteStorageObjectRepository(dbContext).AddAsync(storageObject);

    var importSession = new DocumentImportSession(
      DocumentImportSessionId.New(),
      project.Id,
      "test@example.invalid",
      createdAtUtc.AddMinutes(3));
    importSession.BeginValidation("test@example.invalid", createdAtUtc.AddMinutes(4));
    importSession.BeginImporting("test@example.invalid", createdAtUtc.AddMinutes(5));
    await new SqliteDocumentImportSessionRepository(dbContext).AddAsync(importSession);
    StageAuditEvents(auditRecorder, importSession.AuditTrail);

    var source = new ImportedDocumentSource(
      ImportedSourceId.New(),
      importSession.Id,
      project.Id,
      "test.txt",
      "text/plain",
      "text/plain",
      content.Length,
      contentHash,
      "SHA-256",
      1,
      storageObject.ToReference(),
      ImportedSourceOrigin.LocalFile,
      "test@example.invalid",
      createdAtUtc.AddMinutes(6),
      ImportedDocumentSourceStatus.Available,
      null);
    await new SqliteImportedDocumentSourceRepository(dbContext).AddAsync(source);
    StageAuditEvents(auditRecorder, source.AuditTrail);

    importSession.RecordAcceptedSource(source.Id, "test@example.invalid", createdAtUtc.AddMinutes(6));
    importSession.Complete("test@example.invalid", createdAtUtc.AddMinutes(7));
    await unitOfWork.CommitAsync();

    return new SeededParsingContext(project.Id, source.Id, source.ContentHash, createdAtUtc);
  }

  private static ParserRun CreateParserRun(ProjectId projectId, ImportedSourceId sourceId, DateTimeOffset createdAtUtc)
  {
    return new ParserRun(
      ParserRunId.New(),
      projectId,
      sourceId,
      "plain-text-deterministic",
      "1.0.0",
      "1.0.0",
      "contract-hash-sha256",
      "content-hash",
      "SHA-256",
      1,
      "test@example.invalid",
      createdAtUtc);
  }

  private static FragmentCandidate CreateFragmentCandidate(
    ParserRunId runId,
    ProjectId projectId,
    ImportedSourceId sourceId,
    DateTimeOffset createdAtUtc)
  {
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");
    return new FragmentCandidate(
      FragmentCandidateId.New(),
      runId,
      projectId,
      sourceId,
      "content-hash",
      locator,
      1,
      ContentKind.PlainText,
      "Parsed text content.",
      ConfidenceBand.High,
      "plain-text-deterministic",
      "1.0.0",
      createdAtUtc);
  }

  private static FragmentCandidate CreateFragmentCandidateWithLocator(
    ParserRunId runId,
    ProjectId projectId,
    ImportedSourceId sourceId,
    FragmentLocator locator,
    DateTimeOffset createdAtUtc)
  {
    return new FragmentCandidate(
      FragmentCandidateId.New(),
      runId,
      projectId,
      sourceId,
      "content-hash",
      locator,
      1,
      ContentKind.PlainText,
      "Line range content.",
      ConfidenceBand.High,
      "plain-text-deterministic",
      "1.0.0",
      createdAtUtc);
  }

  private static void StageAuditEvents(SqliteAuditRecorder auditRecorder, IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      auditRecorder.Stage(auditEvent);
    }
  }

  private static async Task<long> QueryCountAsync(SpinbusterDbContext dbContext, string sql)
  {
    await dbContext.Database.OpenConnectionAsync();
    try
    {
      await using var command = dbContext.Database.GetDbConnection().CreateCommand();
      command.CommandText = sql;
      var result = await command.ExecuteScalarAsync();
      return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }
    finally
    {
      await dbContext.Database.CloseConnectionAsync();
    }
  }

  private sealed record SeededParsingContext(
    ProjectId ProjectId,
    ImportedSourceId SourceId,
    string ContentHash,
    DateTimeOffset CreatedAtUtc);
}
