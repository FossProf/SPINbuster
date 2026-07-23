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

public sealed class SqlitePromotionDiagnosticPersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task PromotionDiagnosticRoundTripsAllFields()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;
    FragmentCandidateId candidateId;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);
      runId = run.Id;

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateId = candidate.Id;

      await new SqliteParserRunRepository(dbContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    var promotionDiagnostic = new PromotionDiagnostic(
      PromotionDiagnosticId.New(),
      candidateId,
      runId,
      seeded.ProjectId,
      createdAt.AddMinutes(3));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var repository = new SqlitePromotionDiagnosticRepository(dbContext);

      await repository.AddAsync(promotionDiagnostic);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var stored = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByIdAsync(promotionDiagnostic.Id);

    Assert.NotNull(stored);
    Assert.Equal(PromotionDiagnosticStatus.Eligible, stored!.Status);
    Assert.Equal(candidateId, stored.FragmentCandidateId);
    Assert.Equal(runId, stored.ParserRunId);
    Assert.Equal(seeded.ProjectId, stored.ProjectId);
    Assert.Null(stored.FailureReason);
    Assert.Null(stored.KnowledgeDocumentId);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM promotion_diagnostics"));
  }

  [Fact]
  public async Task PromotionDiagnosticUpdatePersistsStatusChange()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    PromotionDiagnosticId diagnosticId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    PromotionDiagnostic reloaded;
    await using (var loadContext = CreateDbContext())
    {
      reloaded = (await new SqlitePromotionDiagnosticRepository(loadContext)
        .GetByIdAsync(diagnosticId))!;
    }

    var knowledgeDocId = KnowledgeDocumentId.New();
    var revisionId = KnowledgeDocumentRevisionId.New();
    var citationId = KnowledgeCitationId.New();
    reloaded.RecordSuccess(knowledgeDocId, revisionId, citationId, false, null);

    await using (var updateContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(updateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(updateContext).UpdateAsync(reloaded);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var stored = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByIdAsync(diagnosticId);

    Assert.NotNull(stored);
    Assert.Equal(PromotionDiagnosticStatus.Promoted, stored!.Status);
    Assert.Equal(knowledgeDocId, stored.KnowledgeDocumentId);
    Assert.Equal(revisionId, stored.KnowledgeDocumentRevisionId);
    Assert.Equal(citationId, stored.KnowledgeCitationId);
    Assert.False(stored.SupersededExistingRevision);
  }

  [Fact]
  public async Task GetByFragmentCandidateAsyncReturnsUniqueMatch()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateId;
    PromotionDiagnosticId diagnosticId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateId = candidate.Id;

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var found = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByFragmentCandidateAsync(candidateId);
    var notFound = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByFragmentCandidateAsync(FragmentCandidateId.New());

    Assert.NotNull(found);
    Assert.Equal(diagnosticId, found!.Id);
    Assert.Null(notFound);
  }

  [Fact]
  public async Task GetByProjectAsyncReturnsBoundedCollection()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    PromotionDiagnosticId diagnosticAId;
    PromotionDiagnosticId diagnosticBId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var runA = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      runA.Start(createdAt.AddMinutes(1));
      runA.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);
      var candidateA = CreateFragmentCandidate(runA.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      var runB = new ParserRun(
        ParserRunId.New(),
        seeded.ProjectId,
        seeded.SourceId,
        "parser-b",
        "1.0.0",
        "1.0.0",
        "contract-hash-b",
        seeded.ContentHash,
        "SHA-256",
        1,
        "test@example.invalid",
        createdAt);
      runB.Start(createdAt.AddMinutes(1));
      runB.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);
      var candidateB = CreateFragmentCandidate(runB.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(seedContext).AddAsync(runA);
      await new SqliteParserRunRepository(seedContext).AddAsync(runB);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidateA);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidateB);
      StageAuditEvents(auditRecorder, runA.AuditTrail);
      StageAuditEvents(auditRecorder, candidateA.AuditTrail);
      StageAuditEvents(auditRecorder, runB.AuditTrail);
      StageAuditEvents(auditRecorder, candidateB.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnosticA = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateA.Id,
        runA.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnosticAId = diagnosticA.Id;

      var diagnosticB = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateB.Id,
        runB.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(4));
      diagnosticBId = diagnosticB.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnosticA);
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnosticB);
      await unitOfWork2.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var allResults = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByProjectAsync(seeded.ProjectId, maxResults: 10);
    var boundedResults = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByProjectAsync(seeded.ProjectId, maxResults: 1);

    Assert.Equal(2, allResults.Count);
    Assert.Single(boundedResults);
  }

  [Fact]
  public async Task FragmentCandidateUniqueIndexPreventsDuplicatePromotionDiagnostic()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;

    ParserRunId runId;
    FragmentCandidateId candidateId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);
      runId = run.Id;

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateId = candidate.Id;

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var first = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(3));

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(first);
      await unitOfWork2.CommitAsync();
    }

    await using (var duplicateContext = CreateDbContext())
    {
      var duplicate = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(4));
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(duplicateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(duplicateContext).AddAsync(duplicate);
      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM promotion_diagnostics"));
  }

  [Fact]
  public async Task FindSuccessfulByContentHashAsyncReturnsMatchAfterPromotion()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    PromotionDiagnosticId diagnosticId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnostic.RecordSuccess(
        KnowledgeDocumentId.New(),
        KnowledgeDocumentRevisionId.New(),
        KnowledgeCitationId.New(),
        false,
        null);
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var found = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .FindSuccessfulByContentHashAsync(
        seeded.ProjectId,
        "content-hash",
        string.Empty);
    var notFoundWrongHash = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .FindSuccessfulByContentHashAsync(
        seeded.ProjectId,
        "wrong-hash",
        string.Empty);

    Assert.NotNull(found);
    Assert.Equal(diagnosticId, found!.Id);
    Assert.Null(notFoundWrongHash);
  }

  [Fact]
  public async Task FailedDiagnosticRoundTripsFailureReason()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    PromotionDiagnosticId diagnosticId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnostic.RecordFailure("Duplicate content detected in upstream source.");
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var stored = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByIdAsync(diagnosticId);

    Assert.NotNull(stored);
    Assert.Equal(PromotionDiagnosticStatus.Failed, stored!.Status);
    Assert.Equal("Duplicate content detected in upstream source.", stored.FailureReason);
    Assert.Null(stored.KnowledgeDocumentId);
  }

  [Fact]
  public async Task PromotionDiagnosticMigrationCreatesTableAndIndexes()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var tableExists = await QueryCountAsync(dbContext,
      "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='promotion_diagnostics'");
    Assert.Equal(1, tableExists);

    var uniqueIndexExists = await QueryCountAsync(dbContext,
      "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_promotion_diagnostics_FragmentCandidateId'");
    Assert.Equal(1, uniqueIndexExists);

    var projectIndexExists = await QueryCountAsync(dbContext,
      "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_promotion_diagnostics_ProjectId'");
    Assert.Equal(1, projectIndexExists);
  }

  [Fact]
  public async Task PromotionDiagnosticMigrationIsIdempotent()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    Assert.Contains(appliedMigrations, m => m.EndsWith("PromotionDiagnosticSlice", StringComparison.Ordinal));
  }

  [Fact]
  public async Task PromotionDiagnosticPersistsSupersessionFields()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    PromotionDiagnosticId diagnosticId;
    var supersededRevisionId = KnowledgeDocumentRevisionId.New();

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnostic.RecordSuccess(
        KnowledgeDocumentId.New(),
        KnowledgeDocumentRevisionId.New(),
        KnowledgeCitationId.New(),
        true,
        supersededRevisionId);
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var stored = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByIdAsync(diagnosticId);

    Assert.NotNull(stored);
    Assert.True(stored!.SupersededExistingRevision);
    Assert.Equal(supersededRevisionId, stored.SupersededRevisionId);
  }

  [Fact]
  public async Task WrongProjectIsolation()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var otherProjectId = ProjectId.New();
    PromotionDiagnosticId diagnosticId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var ownProjectResults = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByProjectAsync(seeded.ProjectId, 100);
    var otherProjectResults = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByProjectAsync(otherProjectId, 100);

    Assert.Single(ownProjectResults);
    Assert.Empty(otherProjectResults);
  }

  [Fact]
  public async Task RestartReplayIdempotencyProducesEligibleDiagnostic()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(3));

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var stored = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByIdAsync(PromotionDiagnosticId.New());

    Assert.Null(stored);

    var allDiagnostics = await new SqlitePromotionDiagnosticRepository(verificationContext)
      .GetByProjectAsync(seeded.ProjectId, 100);

    Assert.Single(allDiagnostics);
    Assert.Equal(PromotionDiagnosticStatus.Eligible, allDiagnostics.First().Status);
  }

  [Fact]
  public async Task PromotionDiagnosticRollbackDoesNotPersistOnAuditConflict()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    var duplicateAuditId = AuditEventId.New();
    PromotionDiagnosticId diagnosticId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      diagnosticId = PromotionDiagnosticId.New();

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      seedContext.AuditEvents.Add(new AuditEventRecord
      {
        Id = duplicateAuditId,
        SubjectType = nameof(PromotionDiagnostic),
        SubjectId = diagnosticId.ToString(),
        EventType = "SeedConflict",
        Actor = "seed@example.invalid",
        OccurredAtUtc = createdAt.AddMinutes(30),
        Description = "Seed audit event for rollback test.",
      });
      await seedContext.SaveChangesAsync();
    }

    await using (var updateContext = CreateDbContext())
    {
      var diagnostic = new PromotionDiagnostic(
        diagnosticId,
        FragmentCandidateId.New(),
        ParserRunId.New(),
        seeded.ProjectId,
        createdAt.AddMinutes(3));

      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(updateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(updateContext).AddAsync(diagnostic);
      auditRecorder.Stage(new AuditEvent(
        duplicateAuditId,
        nameof(PromotionDiagnostic),
        diagnosticId.ToString(),
        "SeedConflict",
        "seed@example.invalid",
        createdAt.AddMinutes(30),
        "Seed audit event for rollback test."));

      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(0L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM promotion_diagnostics"));
  }

  [Fact]
  public async Task NoReleasedMigrationDrift()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
    Assert.Empty(pendingMigrations);
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
    var createdAtUtc = new DateTimeOffset(2026, 7, 23, 9, 0, 0, TimeSpan.Zero);
    var content = System.Text.Encoding.UTF8.GetBytes("Hello, world!");
    var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));

    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

    var project = new Project(ProjectId.New(), "Promotion Test Project", "test@example.invalid", createdAtUtc);
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
