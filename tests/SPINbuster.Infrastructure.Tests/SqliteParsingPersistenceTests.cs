using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;
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
  public async Task FragmentCandidatePersistAndReloadPreservesIdentityKey()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;

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

    var candidate = CreateFragmentCandidate(runId, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
    var originalIdentityKey = candidate.IdentityKey;
    var originalIdentityKeyHash = candidate.IdentityKeyHash;

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
    var stored = storedCandidates.First();

    Assert.Equal(originalIdentityKey, stored.IdentityKey);
    Assert.Equal(originalIdentityKeyHash, stored.IdentityKeyHash);
    Assert.Equal(candidate.ExtractedText, stored.ExtractedText);
    Assert.Equal(candidate.TextLength, stored.TextLength);
    Assert.Equal(candidate.Ordinal, stored.Ordinal);
    Assert.Equal(candidate.ContentKind, stored.ContentKind);
    Assert.Equal(candidate.ConfidenceBand, stored.ConfidenceBand);
    Assert.Equal(candidate.ImportedSourceId, stored.ImportedSourceId);
    Assert.Equal(candidate.SourceContentHash, stored.SourceContentHash);
  }

  [Fact]
  public async Task DifferentParserKeyProducesDifferentIdentityOnPersistAndReload()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");

    var candidateA = CreateFragmentCandidateWithLocatorAndParserIdentity(
      ParserRunId.New(), seeded.ProjectId, seeded.SourceId, locator, createdAt, "parser-a", "1.0.0");
    var candidateB = CreateFragmentCandidateWithLocatorAndParserIdentity(
      ParserRunId.New(), seeded.ProjectId, seeded.SourceId, locator, createdAt, "parser-b", "1.0.0");

    var keyA = FragmentCandidate.ComputeIdentityKey(seeded.SourceId, "parser-a", "1.0.0", locator);
    var keyB = FragmentCandidate.ComputeIdentityKey(seeded.SourceId, "parser-b", "1.0.0", locator);
    Assert.NotEqual(keyA, keyB);

    Assert.NotEqual(candidateA.IdentityKey, candidateB.IdentityKey);
  }

  [Fact]
  public void FragmentCandidateIdentityChangesWhenContractVersionChanges()
  {
    var sourceId = ImportedSourceId.New();
    var locator = new FragmentLocator(FragmentLocatorType.WholeDocument, "*");

    var key1 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "1.0.0", locator);
    var key2 = FragmentCandidate.ComputeIdentityKey(sourceId, "parser", "2.0.0", locator);

    Assert.NotEqual(key1, key2);
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

  [Fact]
  public async Task FreshMigrationCreatesFragmentReviewColumnsAndIndexes()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    Assert.Contains(appliedMigrations, m => m.EndsWith("AddFragmentReviewIndexesAndConstraint", StringComparison.Ordinal));

    var candidateCount = await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM parser_fragment_candidates");
    Assert.Equal(0L, candidateCount);
  }

  [Fact]
  public async Task UpgradeMigrationIsIdempotentForReviewIndexes()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    Assert.Contains(appliedMigrations, m => m.EndsWith("AddFragmentReviewIndexesAndConstraint", StringComparison.Ordinal));
  }

  [Fact]
  public async Task GeneratedBackfillForExistingCandidates()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedCandidates = await new SqliteFragmentCandidateRepository(verificationContext).GetByParserRunAsync(
      (await verificationContext.FragmentCandidates.FirstAsync()).ParserRunId, 100);
    var stored = storedCandidates.First();

    Assert.Equal(FragmentCandidateReviewState.Generated, stored.ReviewState);
    Assert.Null(stored.ReviewedBy);
    Assert.Null(stored.ReviewedAtUtc);
    Assert.Null(stored.ReviewNotes);
  }

  [Fact]
  public async Task AcceptedFragmentCandidatePersistsAndReloads()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateId = candidate.Id;
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    var acceptTime = createdAt.AddHours(1);

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var repository = new SqliteFragmentCandidateRepository(dbContext);
      var candidate = await repository.GetByIdAsync(candidateId);
      Assert.NotNull(candidate);

      var priorAuditCount = candidate!.AuditTrail.Count;
      candidate.Accept("reviewer@example.invalid", acceptTime, "Looks good.");

      await repository.UpdateAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail.Skip(priorAuditCount));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedCandidates = await new SqliteFragmentCandidateRepository(verificationContext).GetByParserRunAsync(
      (await verificationContext.FragmentCandidates.Where(c => c.Id == candidateId).FirstAsync()).ParserRunId, 100);
    var stored = storedCandidates.First(c => c.Id == candidateId);

    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, stored.ReviewState);
    Assert.Equal("reviewer@example.invalid", stored.ReviewedBy);
    Assert.Equal(acceptTime, stored.ReviewedAtUtc);
    Assert.Equal("Looks good.", stored.ReviewNotes);
    Assert.Equal(2, stored.AuditTrail.Count);
    Assert.Contains(stored.AuditTrail, e => e.EventType == "FragmentCandidateHumanAccepted");
  }

  [Fact]
  public async Task RejectedFragmentCandidatePersistsAndReloads()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateId = candidate.Id;
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    var rejectTime = createdAt.AddHours(2);

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var repository = new SqliteFragmentCandidateRepository(dbContext);
      var candidate = await repository.GetByIdAsync(candidateId);
      Assert.NotNull(candidate);

      var priorAuditCount = candidate!.AuditTrail.Count;
      candidate.Reject("reviewer@example.invalid", rejectTime, "Not relevant.");

      await repository.UpdateAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail.Skip(priorAuditCount));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedCandidates = await new SqliteFragmentCandidateRepository(verificationContext).GetByParserRunAsync(
      (await verificationContext.FragmentCandidates.Where(c => c.Id == candidateId).FirstAsync()).ParserRunId, 100);
    var stored = storedCandidates.First(c => c.Id == candidateId);

    Assert.Equal(FragmentCandidateReviewState.Rejected, stored.ReviewState);
    Assert.Equal("reviewer@example.invalid", stored.ReviewedBy);
    Assert.Equal(rejectTime, stored.ReviewedAtUtc);
    Assert.Equal("Not relevant.", stored.ReviewNotes);
    Assert.Contains(stored.AuditTrail, e => e.EventType == "FragmentCandidateRejected");
  }

  [Fact]
  public async Task AtomicRollbackDoesNotPersistReviewState()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateId = candidate.Id;
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    var duplicateAuditId = AuditEventId.New();

    await using (var seedContext = CreateDbContext())
    {
      seedContext.Add(new AuditEventRecord
      {
        Id = duplicateAuditId,
        SubjectType = nameof(FragmentCandidate),
        SubjectId = candidateId.ToString(),
        EventType = "SeedConflict",
        Actor = "seed@example.invalid",
        OccurredAtUtc = createdAt.AddMinutes(30),
        Description = "Seed audit event for rollback test.",
      });
      await seedContext.SaveChangesAsync();
    }

    await using var updateContext = CreateDbContext();
    var repository = new SqliteFragmentCandidateRepository(updateContext);
    var loadedCandidate = await repository.GetByIdAsync(candidateId);
    Assert.NotNull(loadedCandidate);

    loadedCandidate!.Accept("reviewer@example.invalid", createdAt.AddHours(1), "Accepted.");

    await repository.UpdateAsync(loadedCandidate);

    var auditRecorder2 = new SqliteAuditRecorder();
    var unitOfWork2 = new SqliteUnitOfWork(updateContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    auditRecorder2.Stage(new AuditEvent(
      duplicateAuditId,
      nameof(FragmentCandidate),
      candidateId.ToString(),
      "FragmentCandidateHumanAccepted",
      "reviewer@example.invalid",
      createdAt.AddHours(1),
      "Accepted."));
    await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork2.CommitAsync());

    await using var verificationContext = CreateDbContext();
    var storedCandidates = await new SqliteFragmentCandidateRepository(verificationContext).GetByParserRunAsync(
      (await verificationContext.FragmentCandidates.Where(c => c.Id == candidateId).FirstAsync()).ParserRunId, 100);
    var stored = storedCandidates.First(c => c.Id == candidateId);

    Assert.Equal(FragmentCandidateReviewState.Generated, stored.ReviewState);
    Assert.Null(stored.ReviewedBy);
  }

  [Fact]
  public async Task FilteredQueryByReviewStateReturnsCorrectSubset()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var projectId = seeded.ProjectId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(projectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      for (var i = 0; i < 3; i++)
      {
        var candidate = CreateFragmentCandidate(run.Id, projectId, seeded.SourceId, createdAt.AddMinutes(i + 1));
        await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
        StageAuditEvents(auditRecorder, candidate.AuditTrail);
      }

      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var repository = new SqliteFragmentCandidateRepository(verificationContext);

    var allCandidates = await repository.GetByProjectFilteredAsync(projectId, 100, null);
    Assert.Equal(3, allCandidates.Count);

    var generatedOnly = await repository.GetByProjectFilteredAsync(projectId, 100, FragmentCandidateReviewState.Generated);
    Assert.Equal(3, generatedOnly.Count);

    var acceptedOnly = await repository.GetByProjectFilteredAsync(projectId, 100, FragmentCandidateReviewState.HumanAccepted);
    Assert.Empty(acceptedOnly);
  }

  [Fact]
  public async Task FilteredQueryRespectsMaxResults()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var projectId = seeded.ProjectId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(projectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      for (var i = 0; i < 5; i++)
      {
        var candidate = CreateFragmentCandidate(run.Id, projectId, seeded.SourceId, createdAt.AddMinutes(i + 1));
        await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
        StageAuditEvents(auditRecorder, candidate.AuditTrail);
      }

      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var repository = new SqliteFragmentCandidateRepository(verificationContext);

    var bounded = await repository.GetByProjectFilteredAsync(projectId, 3, null);
    Assert.Equal(3, bounded.Count);

    var all = await repository.GetByProjectFilteredAsync(projectId, 100, null);
    Assert.Equal(5, all.Count);
  }

  [Fact]
  public async Task FilteredQueryStableOrdering()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var projectId = seeded.ProjectId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(projectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      for (var i = 0; i < 4; i++)
      {
        var candidate = CreateFragmentCandidate(run.Id, projectId, seeded.SourceId, createdAt.AddMinutes(i + 1));
        await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
        StageAuditEvents(auditRecorder, candidate.AuditTrail);
      }

      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var repository = new SqliteFragmentCandidateRepository(verificationContext);
    var results1 = (await repository.GetByProjectFilteredAsync(projectId, 100, null)).ToArray();
    var results2 = (await repository.GetByProjectFilteredAsync(projectId, 100, null)).ToArray();

    Assert.Equal(results1.Length, results2.Length);
    for (var i = 0; i < results1.Length; i++)
    {
      Assert.Equal(results1[i].Id, results2[i].Id);
    }
  }

  [Fact]
  public async Task WrongProjectIsolation()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var otherProjectId = ProjectId.New();

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var repository = new SqliteFragmentCandidateRepository(verificationContext);

    var ownProjectResults = await repository.GetByProjectFilteredAsync(seeded.ProjectId, 100, null);
    Assert.Single(ownProjectResults);

    var otherProjectResults = await repository.GetByProjectFilteredAsync(otherProjectId, 100, null);
    Assert.Empty(otherProjectResults);
  }

  [Fact]
  public async Task NoReleasedMigrationDrift()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
    Assert.Empty(pendingMigrations);
  }

  [Fact]
  public async Task ParserDiagnosticsTableExistsAfterMigration()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var tableExists = await QueryCountAsync(dbContext,
      "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='parser_diagnostics'");
    Assert.Equal(1, tableExists);
  }

  [Fact]
  public async Task AddRangePersistsDiagnosticsAndReloads()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var runId = ParserRunId.New();
    var diagnosticId1 = ParserDiagnosticId.New();
    var diagnosticId2 = ParserDiagnosticId.New();

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      runId = run.Id;
      await unitOfWork.CommitAsync();
    }

    var diagnostics = new[]
    {
      new ParserDiagnostic(
        diagnosticId1,
        runId,
        DiagnosticSeverity.Warning,
        "OVERLAPPING_CONTENT",
        "Fragment at ordinal 2 overlaps with fragment at ordinal 1.",
        createdAt.AddMinutes(1),
        DiagnosticRefType.Ordinal,
        "2"),
      new ParserDiagnostic(
        diagnosticId2,
        runId,
        DiagnosticSeverity.Info,
        "TABLE_SEPARATOR_MISSING",
        "Pipe-delimited table row was missing a trailing separator.",
        createdAt.AddMinutes(2),
        DiagnosticRefType.NormalizedLocator,
        "structural:section-2",
        FragmentLocatorType.StructuralPath,
        "section-2"),
    };

    await using (var dbContext = CreateDbContext())
    {
      var diagnosticRepo = new SqliteParserDiagnosticRepository(dbContext);
      await diagnosticRepo.AddRangeAsync(diagnostics);
      await dbContext.SaveChangesAsync();
    }

    await using var verificationContext = CreateDbContext();
    var loaded = await new SqliteParserDiagnosticRepository(verificationContext).GetByParserRunAsync(runId);
    Assert.Equal(2, loaded.Count);

    var loaded1 = loaded.First(d => d.Id == diagnosticId1);
    Assert.Equal(DiagnosticSeverity.Warning, loaded1.Severity);
    Assert.Equal("OVERLAPPING_CONTENT", loaded1.Code);
    Assert.Equal(DiagnosticRefType.Ordinal, loaded1.CandidateRefType);
    Assert.Equal("2", loaded1.CandidateRefValue);
    Assert.Null(loaded1.LocatorType);
    Assert.Null(loaded1.LocatorValue);

    var loaded2 = loaded.First(d => d.Id == diagnosticId2);
    Assert.Equal(DiagnosticSeverity.Info, loaded2.Severity);
    Assert.Equal("TABLE_SEPARATOR_MISSING", loaded2.Code);
    Assert.Equal(DiagnosticRefType.NormalizedLocator, loaded2.CandidateRefType);
    Assert.Equal("structural:section-2", loaded2.CandidateRefValue);
    Assert.Equal(FragmentLocatorType.StructuralPath, loaded2.LocatorType);
    Assert.Equal("section-2", loaded2.LocatorValue);
  }

  [Fact]
  public async Task GetByParserRunAndCandidateFiltersByCandidateRefValue()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      runId = run.Id;
      await unitOfWork.CommitAsync();
    }

    var diagnostics = new[]
    {
      new ParserDiagnostic(
        ParserDiagnosticId.New(),
        runId,
        DiagnosticSeverity.Warning,
        "OVERLAPPING_CONTENT",
        "Overlap detected.",
        createdAt.AddMinutes(1),
        DiagnosticRefType.Ordinal,
        "1"),
      new ParserDiagnostic(
        ParserDiagnosticId.New(),
        runId,
        DiagnosticSeverity.Info,
        "HEADING_EXTRACTED",
        "Heading extracted.",
        createdAt.AddMinutes(2),
        DiagnosticRefType.Ordinal,
        "2"),
    };

    await using (var dbContext = CreateDbContext())
    {
      await new SqliteParserDiagnosticRepository(dbContext).AddRangeAsync(diagnostics);
      await dbContext.SaveChangesAsync();
    }

    await using var verificationContext = CreateDbContext();
    var filtered = await new SqliteParserDiagnosticRepository(verificationContext)
      .GetByParserRunAndCandidateAsync(runId, "2");

    Assert.Single(filtered);
    Assert.Equal("HEADING_EXTRACTED", filtered[0].Code);
  }

  [Fact]
  public async Task DiagnosticsPersistAcrossMultipleParserRuns()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var runId1 = ParserRunId.New();
    var runId2 = ParserRunId.New();

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      var run1 = new ParserRun(runId1, seeded.ProjectId, seeded.SourceId, "parser-a", "1.0.0", "1.0.0", "hash-a", "content-hash", "SHA-256", 1, "test@example.invalid", createdAt);
      var run2 = new ParserRun(runId2, seeded.ProjectId, seeded.SourceId, "parser-b", "1.0.0", "1.0.0", "hash-b", "content-hash", "SHA-256", 1, "test@example.invalid", createdAt);

      await new SqliteParserRunRepository(dbContext).AddAsync(run1);
      await new SqliteParserRunRepository(dbContext).AddAsync(run2);
      StageAuditEvents(auditRecorder, run1.AuditTrail);
      StageAuditEvents(auditRecorder, run2.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using (var dbContext = CreateDbContext())
    {
      var repo = new SqliteParserDiagnosticRepository(dbContext);
      await repo.AddRangeAsync(new[]
      {
        new ParserDiagnostic(ParserDiagnosticId.New(), runId1, DiagnosticSeverity.Info, "CODE-A", "Diagnostic for run A.", createdAt.AddMinutes(1)),
        new ParserDiagnostic(ParserDiagnosticId.New(), runId2, DiagnosticSeverity.Warning, "CODE-B", "Diagnostic for run B.", createdAt.AddMinutes(2)),
      });
      await dbContext.SaveChangesAsync();
    }

    await using var verificationContext = CreateDbContext();
    var repo1 = new SqliteParserDiagnosticRepository(verificationContext);
    var run1Diags = await repo1.GetByParserRunAsync(runId1);
    Assert.Single(run1Diags);
    Assert.Equal("CODE-A", run1Diags[0].Code);

    var repo2 = new SqliteParserDiagnosticRepository(verificationContext);
    var run2Diags = await repo2.GetByParserRunAsync(runId2);
    Assert.Single(run2Diags);
    Assert.Equal("CODE-B", run2Diags[0].Code);
  }

  [Fact]
  public async Task DiagnosticsAreEmptyForRunWithNoDiagnostics()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      await unitOfWork.CommitAsync();

      await using var verificationContext = CreateDbContext();
      var loaded = await new SqliteParserDiagnosticRepository(verificationContext).GetByParserRunAsync(run.Id);
      Assert.Empty(loaded);
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

  private static FragmentCandidate CreateFragmentCandidateWithLocatorAndParserIdentity(
    ParserRunId runId,
    ProjectId projectId,
    ImportedSourceId sourceId,
    FragmentLocator locator,
    DateTimeOffset createdAtUtc,
    string parserKey,
    string parserContractVersion)
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
      "Parser identity content.",
      ConfidenceBand.High,
      parserKey,
      parserContractVersion,
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
