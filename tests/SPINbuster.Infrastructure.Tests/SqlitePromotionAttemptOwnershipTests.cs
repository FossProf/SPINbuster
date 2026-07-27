using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Repositories;
using SPINbuster.Infrastructure.Services;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqlitePromotionAttemptOwnershipTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task SameCandidateTwoAttemptsDifferentOutcomesReloadChronologically()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateId;
    ParserRunId runId;

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
    }

    var recordId = PromotionRecordId.New();
    PromotionAttemptId failureAttemptId;
    PromotionAttemptId successAttemptId;
    PromotionDiagnosticId failureDiagnosticId;
    PromotionDiagnosticId successDiagnosticId;

    await using (var firstContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(firstContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      var failDiagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      failDiagnostic.RecordFailure("Project not active.");
      failureDiagnosticId = failDiagnostic.Id;
      await new SqlitePromotionDiagnosticRepository(firstContext).AddAsync(failDiagnostic);

      var failAttempt = new PromotionAttempt(
        PromotionAttemptId.New(),
        recordId,
        PromotionAttemptOutcome.RetryablePreconditionFailure,
        failDiagnostic.Id,
        candidateId,
        seeded.ContentHash,
        createdAt.AddMinutes(3),
        "Project not active.");
      failureAttemptId = failAttempt.Id;
      await new SqlitePromotionAttemptRepository(firstContext).AddAsync(failAttempt);
      await unitOfWork.CommitAsync();
    }

    await using (var secondContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(secondContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      var successDiagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(5));
      successDiagnostic.RecordSuccess(
        KnowledgeDocumentId.New(),
        KnowledgeDocumentRevisionId.New(),
        KnowledgeCitationId.New(),
        false,
        null);
      successDiagnosticId = successDiagnostic.Id;
      await new SqlitePromotionDiagnosticRepository(secondContext).AddAsync(successDiagnostic);

      var successAttempt = new PromotionAttempt(
        PromotionAttemptId.New(),
        recordId,
        PromotionAttemptOutcome.Promoted,
        successDiagnostic.Id,
        candidateId,
        seeded.ContentHash,
        createdAt.AddMinutes(5),
        null);
      successAttemptId = successAttempt.Id;
      await new SqlitePromotionAttemptRepository(secondContext).AddAsync(successAttempt);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var attempts = await new SqlitePromotionAttemptRepository(verificationContext)
      .GetByFragmentCandidateAsync(candidateId);

    Assert.Equal(2, attempts.Count);
    Assert.Equal(PromotionAttemptOutcome.RetryablePreconditionFailure, attempts[0].Outcome);
    Assert.Equal(failureAttemptId, attempts[0].Id);
    Assert.Equal(failureDiagnosticId, attempts[0].DiagnosticId);
    Assert.Equal(PromotionAttemptOutcome.Promoted, attempts[1].Outcome);
    Assert.Equal(successAttemptId, attempts[1].Id);
    Assert.Equal(successDiagnosticId, attempts[1].DiagnosticId);
    Assert.True(attempts[1].AttemptedAtUtc >= attempts[0].AttemptedAtUtc);
    Assert.Equal(seeded.ContentHash, attempts[0].ContentHash);
    Assert.Equal(seeded.ContentHash, attempts[1].ContentHash);
  }

  [Fact]
  public async Task SameCandidateThreeAttemptsReloadChronologically()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateId;
    ParserRunId runId;

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
    }

    var recordId = PromotionRecordId.New();
    var outcomes = new[]
    {
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionAttemptOutcome.RetryablePreconditionFailure,
      PromotionAttemptOutcome.Promoted,
    };

    for (var i = 0; i < 3; i++)
    {
      await using var context = CreateDbContext();
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(context, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(3 + (i * 2)));

      if (outcomes[i] == PromotionAttemptOutcome.Promoted)
      {
        diagnostic.RecordSuccess(
          KnowledgeDocumentId.New(),
          KnowledgeDocumentRevisionId.New(),
          KnowledgeCitationId.New(),
          false,
          null);
      }
      else
      {
        diagnostic.RecordFailure($"Attempt {i + 1} failed.");
      }

      await new SqlitePromotionDiagnosticRepository(context).AddAsync(diagnostic);

      var attempt = new PromotionAttempt(
        PromotionAttemptId.New(),
        recordId,
        outcomes[i],
        diagnostic.Id,
        candidateId,
        seeded.ContentHash,
        createdAt.AddMinutes(3 + (i * 2)),
        outcomes[i] == PromotionAttemptOutcome.Promoted ? null : $"Attempt {i + 1} failed.");

      await new SqlitePromotionAttemptRepository(context).AddAsync(attempt);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var attempts = await new SqlitePromotionAttemptRepository(verificationContext)
      .GetByFragmentCandidateAsync(candidateId);

    Assert.Equal(3, attempts.Count);
    Assert.Equal(PromotionAttemptOutcome.RetryablePreconditionFailure, attempts[0].Outcome);
    Assert.Equal(PromotionAttemptOutcome.RetryablePreconditionFailure, attempts[1].Outcome);
    Assert.Equal(PromotionAttemptOutcome.Promoted, attempts[2].Outcome);
    Assert.True(attempts[1].AttemptedAtUtc >= attempts[0].AttemptedAtUtc);
    Assert.True(attempts[2].AttemptedAtUtc >= attempts[1].AttemptedAtUtc);
  }

  [Fact]
  public async Task DifferentCandidateIndependentAttemptHistory()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateAId;
    FragmentCandidateId candidateBId;
    ParserRunId runId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);
      runId = run.Id;

      var candidateA = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateAId = candidateA.Id;

      var candidateB = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateBId = candidateB.Id;

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidateA);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidateB);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidateA.AuditTrail);
      StageAuditEvents(auditRecorder, candidateB.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    var recordA = PromotionRecordId.New();
    var recordB = PromotionRecordId.New();

    await using (var context = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(context, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      var diagnosticA = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateAId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnosticA.RecordFailure("Target A not ready.");
      await new SqlitePromotionDiagnosticRepository(context).AddAsync(diagnosticA);

      var attemptA = new PromotionAttempt(
        PromotionAttemptId.New(),
        recordA,
        PromotionAttemptOutcome.RetryablePreconditionFailure,
        diagnosticA.Id,
        candidateAId,
        seeded.ContentHash,
        createdAt.AddMinutes(3),
        "Target A not ready.");
      await new SqlitePromotionAttemptRepository(context).AddAsync(attemptA);

      var diagnosticB = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateBId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(4));
      diagnosticB.RecordSuccess(
        KnowledgeDocumentId.New(),
        KnowledgeDocumentRevisionId.New(),
        KnowledgeCitationId.New(),
        false,
        null);
      await new SqlitePromotionDiagnosticRepository(context).AddAsync(diagnosticB);

      var attemptB = new PromotionAttempt(
        PromotionAttemptId.New(),
        recordB,
        PromotionAttemptOutcome.Promoted,
        diagnosticB.Id,
        candidateBId,
        seeded.ContentHash,
        createdAt.AddMinutes(4),
        null);
      await new SqlitePromotionAttemptRepository(context).AddAsync(attemptB);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var attemptsA = await new SqlitePromotionAttemptRepository(verificationContext)
      .GetByFragmentCandidateAsync(candidateAId);
    var attemptsB = await new SqlitePromotionAttemptRepository(verificationContext)
      .GetByFragmentCandidateAsync(candidateBId);

    Assert.Single(attemptsA);
    Assert.Equal(PromotionAttemptOutcome.RetryablePreconditionFailure, attemptsA[0].Outcome);
    Assert.Single(attemptsB);
    Assert.Equal(PromotionAttemptOutcome.Promoted, attemptsB[0].Outcome);
    Assert.NotEqual(attemptsA[0].Id, attemptsB[0].Id);
  }

  [Fact]
  public async Task NoDiagnosticFromCandidateALeaksIntoCandidateB()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId candidateAId;
    FragmentCandidateId candidateBId;
    ParserRunId runId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);
      runId = run.Id;

      var candidateA = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateAId = candidateA.Id;

      var candidateB = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      candidateBId = candidateB.Id;

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidateA);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidateB);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidateA.AuditTrail);
      StageAuditEvents(auditRecorder, candidateB.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using (var context = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(context, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      var diagnosticA = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateAId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(3));
      diagnosticA.RecordFailure("Only for candidate A.");
      await new SqlitePromotionDiagnosticRepository(context).AddAsync(diagnosticA);

      var attemptA = new PromotionAttempt(
        PromotionAttemptId.New(),
        PromotionRecordId.New(),
        PromotionAttemptOutcome.RetryablePreconditionFailure,
        diagnosticA.Id,
        candidateAId,
        seeded.ContentHash,
        createdAt.AddMinutes(3),
        "Only for candidate A.");
      await new SqlitePromotionAttemptRepository(context).AddAsync(attemptA);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var attemptsA = await new SqlitePromotionAttemptRepository(verificationContext)
      .GetByFragmentCandidateAsync(candidateAId);
    var attemptsB = await new SqlitePromotionAttemptRepository(verificationContext)
      .GetByFragmentCandidateAsync(candidateBId);

    Assert.Single(attemptsA);
    Assert.Empty(attemptsB);
    Assert.Equal(candidateAId, attemptsA[0].FragmentCandidateId);
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

  private async Task<SeededAttemptContext> SeedSourceAsync()
  {
    var createdAtUtc = new DateTimeOffset(2026, 7, 27, 9, 0, 0, TimeSpan.Zero);
    var content = System.Text.Encoding.UTF8.GetBytes("Hello, world!");
    var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));

    await using var dbContext = CreateDbContext();
    await SpinbusterDbContext.MigrateWithSha256Async(dbContext);

    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

    var project = new Project(ProjectId.New(), "Attempt Ownership Test Project", "test@example.invalid", createdAtUtc);
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

    return new SeededAttemptContext(project.Id, source.Id, source.ContentHash, createdAtUtc);
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

  private sealed record SeededAttemptContext(
    ProjectId ProjectId,
    ImportedSourceId SourceId,
    string ContentHash,
    DateTimeOffset CreatedAtUtc);
}
