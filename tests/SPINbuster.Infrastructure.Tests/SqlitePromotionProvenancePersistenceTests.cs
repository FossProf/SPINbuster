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

public sealed class SqlitePromotionProvenancePersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task PromotionProvenanceRoundTripsAllFields()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;
    FragmentCandidateId candidateId;
    PromotionDiagnosticId diagnosticId;
    KnowledgeDocumentId knowledgeDocId;
    KnowledgeDocumentRevisionId revisionId;
    KnowledgeCitationId citationId;
    PromotionProvenanceId provenanceId;

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

      knowledgeDocId = KnowledgeDocumentId.New();
      var knowledgeDoc = new KnowledgeDocument(
        knowledgeDocId,
        seeded.ProjectId,
        KnowledgeDocumentType.Specification,
        "Test Specification",
        null,
        "structural",
        "test@example.invalid",
        createdAt.AddMinutes(3));
      await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDoc);
      StageAuditEvents(auditRecorder, knowledgeDoc.AuditTrail);

      revisionId = KnowledgeDocumentRevisionId.New();
      var revision = new KnowledgeDocumentRevision(
        revisionId,
        knowledgeDocId,
        KnowledgeSourceId.New(),
        "Rev 1",
        null,
        createdAt.AddMinutes(4),
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "content-hash-rev1",
        "metadata-hash-rev1",
        null,
        null,
        null,
        createdAt.AddMinutes(4));
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revision);

      citationId = KnowledgeCitationId.New();
      var citation = new KnowledgeCitation(
        citationId,
        revisionId,
        KnowledgeCitationLocationType.PageNumber,
        "1",
        "content-hash-rev1",
        createdAt.AddMinutes(5),
        "Quoted text.");
      await new SqliteKnowledgeCitationRepository(dbContext).AddAsync(citation);

      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(6));
      diagnostic.RecordSuccess(knowledgeDocId, revisionId, citationId, false, null);
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(dbContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(dbContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    var reviewState = FragmentCandidateReviewState.HumanAccepted;
    var reviewedBy = "reviewer@example.invalid";
    var reviewedAt = createdAt.AddMinutes(10);
    var promotedAt = createdAt.AddMinutes(11);
    provenanceId = PromotionProvenanceId.New();
    var expected = new PromotionProvenance(
      provenanceId,
      seeded.ProjectId,
      revisionId,
      diagnosticId,
      candidateId,
      "fragment-source-hash-aaa",
      reviewState,
      reviewedBy,
      reviewedAt,
      runId,
      "plain-text-deterministic",
      "1.0.0",
      "1.0.0",
      "contract-hash-sha256",
      seeded.SourceId,
      seeded.ContentHash,
      "identity-hash-123",
      PromotionAttemptId.New(),
      "promoter@example.invalid",
      promotedAt);

    await using (var persistContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(persistContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionProvenanceRepository(persistContext).AddAsync(expected);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var stored = await new SqlitePromotionProvenanceRepository(verificationContext)
      .GetByRevisionIdAsync(revisionId);

    Assert.NotNull(stored);
    Assert.Equal(provenanceId, stored!.Id);
    Assert.Equal(seeded.ProjectId, stored.ProjectId);
    Assert.Equal(revisionId, stored.PromotedRevisionId);
    Assert.Equal(diagnosticId, stored.DiagnosticId);
    Assert.Equal(candidateId, stored.FragmentCandidateId);
    Assert.Equal("fragment-source-hash-aaa", stored.FragmentSourceContentHash);
    Assert.Equal(reviewState, stored.ReviewState);
    Assert.Equal(reviewedBy, stored.ReviewedBy);
    Assert.Equal(reviewedAt, stored.ReviewedAtUtc);
    Assert.Equal(runId, stored.ParserRunId);
    Assert.Equal("plain-text-deterministic", stored.ParserKey);
    Assert.Equal("1.0.0", stored.ParserVersion);
    Assert.Equal("1.0.0", stored.ParserContractVersion);
    Assert.Equal("contract-hash-sha256", stored.ParserContractHash);
    Assert.Equal(seeded.SourceId, stored.ImportedSourceId);
    Assert.Equal(seeded.ContentHash, stored.ImportedSourceContentHash);
    Assert.Equal("identity-hash-123", stored.PromotionIdentityHash);
    Assert.Equal(expected.PromotionAttemptId, stored.PromotionAttemptId);
    Assert.Equal("promoter@example.invalid", stored.PromotedBy);
    Assert.Equal(promotedAt, stored.PromotedAtUtc);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM promotion_provenances"));
  }

  [Fact]
  public async Task AllStronglyTypedIdsRoundTripCorrectly()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    ParserRunId runId;
    FragmentCandidateId candidateId;
    PromotionDiagnosticId diagnosticId;
    KnowledgeDocumentId knowledgeDocId;
    KnowledgeDocumentRevisionId revisionId;
    KnowledgeCitationId citationId;
    PromotionProvenanceId provenanceId;
    PromotionAttemptId promotionAttemptId;

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

      knowledgeDocId = KnowledgeDocumentId.New();
      var knowledgeDoc = new KnowledgeDocument(
        knowledgeDocId,
        seeded.ProjectId,
        KnowledgeDocumentType.Drawing,
        "Test Drawing",
        "EXT-001",
        "architectural",
        "test@example.invalid",
        createdAt.AddMinutes(3));
      await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDoc);
      StageAuditEvents(auditRecorder, knowledgeDoc.AuditTrail);

      revisionId = KnowledgeDocumentRevisionId.New();
      var revision = new KnowledgeDocumentRevision(
        revisionId,
        knowledgeDocId,
        KnowledgeSourceId.New(),
        "Rev A",
        new DateOnly(2026, 7, 23),
        createdAt.AddMinutes(4),
        KnowledgeSourceAuthorityLevel.OwnerProvided,
        "content-hash-drawing",
        "metadata-hash-drawing",
        null,
        null,
        null,
        createdAt.AddMinutes(4));
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revision);

      citationId = KnowledgeCitationId.New();
      var citation = new KnowledgeCitation(
        citationId,
        revisionId,
        KnowledgeCitationLocationType.SheetNumber,
        "A-101",
        "content-hash-drawing",
        createdAt.AddMinutes(5),
        null);
      await new SqliteKnowledgeCitationRepository(dbContext).AddAsync(citation);

      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidateId,
        runId,
        seeded.ProjectId,
        createdAt.AddMinutes(6));
      diagnostic.RecordSuccess(knowledgeDocId, revisionId, citationId, false, null);
      diagnosticId = diagnostic.Id;

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(dbContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(dbContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();
    }

    provenanceId = PromotionProvenanceId.New();
    promotionAttemptId = PromotionAttemptId.New();
    var provenance = new PromotionProvenance(
      provenanceId,
      seeded.ProjectId,
      revisionId,
      diagnosticId,
      candidateId,
      "fragment-source-hash-bbb",
      FragmentCandidateReviewState.HumanAccepted,
      "reviewer@example.invalid",
      createdAt.AddMinutes(10),
      runId,
      "plain-text-deterministic",
      "1.0.0",
      "1.0.0",
      "contract-hash-sha256",
      seeded.SourceId,
      seeded.ContentHash,
      "identity-hash-456",
      promotionAttemptId,
      "promoter@example.invalid",
      createdAt.AddMinutes(11));

    await using (var persistContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(persistContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionProvenanceRepository(persistContext).AddAsync(provenance);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var stored = await new SqlitePromotionProvenanceRepository(verificationContext)
      .GetByRevisionIdAsync(revisionId);

    Assert.NotNull(stored);
    Assert.Equal(provenanceId.Value, stored!.Id.Value);
    Assert.Equal(seeded.ProjectId.Value, stored.ProjectId.Value);
    Assert.Equal(revisionId.Value, stored.PromotedRevisionId.Value);
    Assert.Equal(diagnosticId.Value, stored.DiagnosticId.Value);
    Assert.Equal(candidateId.Value, stored.FragmentCandidateId.Value);
    Assert.Equal(runId.Value, stored.ParserRunId.Value);
    Assert.Equal(seeded.SourceId.Value, stored.ImportedSourceId.Value);
    Assert.Equal(promotionAttemptId.Value, stored.PromotionAttemptId.Value);
  }

  [Fact]
  public async Task ForeignKeysRejectUnrelatedSourceRecords()
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

      var knowledgeDocId = KnowledgeDocumentId.New();
      var knowledgeDoc = new KnowledgeDocument(
        knowledgeDocId,
        seeded.ProjectId,
        KnowledgeDocumentType.Specification,
        "FK Test Document",
        null,
        "structural",
        "test@example.invalid",
        createdAt.AddMinutes(3));
      await new SqliteKnowledgeDocumentRepository(seedContext).AddAsync(knowledgeDoc);
      StageAuditEvents(auditRecorder, knowledgeDoc.AuditTrail);

      var realRevisionId = KnowledgeDocumentRevisionId.New();
      var revision = new KnowledgeDocumentRevision(
        realRevisionId,
        knowledgeDocId,
        KnowledgeSourceId.New(),
        "Rev 1",
        null,
        createdAt.AddMinutes(4),
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "content-hash",
        "metadata-hash",
        null,
        null,
        null,
        createdAt.AddMinutes(4));
      await new SqliteKnowledgeRevisionRepository(seedContext).AddAsync(revision);

      var citationId = KnowledgeCitationId.New();
      var citation = new KnowledgeCitation(
        citationId,
        realRevisionId,
        KnowledgeCitationLocationType.PageNumber,
        "1",
        "content-hash",
        createdAt.AddMinutes(5),
        null);
      await new SqliteKnowledgeCitationRepository(seedContext).AddAsync(citation);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(6));
      diagnostic.RecordSuccess(knowledgeDocId, realRevisionId, citationId, false, null);

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();

      var fakeRevisionId = KnowledgeDocumentRevisionId.New();
      var provenance = new PromotionProvenance(
        PromotionProvenanceId.New(),
        seeded.ProjectId,
        fakeRevisionId,
        diagnostic.Id,
        candidate.Id,
        "fragment-hash",
        FragmentCandidateReviewState.HumanAccepted,
        "reviewer@example.invalid",
        createdAt.AddMinutes(10),
        run.Id,
        "plain-text-deterministic",
        "1.0.0",
        "1.0.0",
        "contract-hash-sha256",
        seeded.SourceId,
        seeded.ContentHash,
        "identity-hash",
        PromotionAttemptId.New(),
        "promoter@example.invalid",
        createdAt.AddMinutes(11));

      var auditRecorder3 = new SqliteAuditRecorder();
      var unitOfWork3 = new SqliteUnitOfWork(seedContext, auditRecorder3, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionProvenanceRepository(seedContext).AddAsync(provenance);
      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork3.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(0L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM promotion_provenances"));
  }

  [Fact]
  public async Task UpgradeMigrationCreatesProvenanceTableWithoutChangingReleasedMigrations()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var tableExists = await QueryCountAsync(dbContext,
      "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='promotion_provenances'");
    Assert.Equal(1, tableExists);

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    Assert.Equal(14, appliedMigrations.Length);
  }

  [Fact]
  public async Task RepeatedMigrateAsyncIsSafe()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();
    await dbContext.Database.MigrateAsync();

    var tableExists = await QueryCountAsync(dbContext,
      "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='promotion_provenances'");
    Assert.Equal(1, tableExists);
  }

  [Fact]
  public async Task ProvenanceSurvivesProviderDisposalAndRecreation()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    PromotionProvenanceId provenanceId;
    KnowledgeDocumentRevisionId revisionId;

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

      var knowledgeDocId = KnowledgeDocumentId.New();
      var knowledgeDoc = new KnowledgeDocument(
        knowledgeDocId,
        seeded.ProjectId,
        KnowledgeDocumentType.Specification,
        "Disposal Test Document",
        null,
        "structural",
        "test@example.invalid",
        createdAt.AddMinutes(3));
      await new SqliteKnowledgeDocumentRepository(seedContext).AddAsync(knowledgeDoc);
      StageAuditEvents(auditRecorder, knowledgeDoc.AuditTrail);

      revisionId = KnowledgeDocumentRevisionId.New();
      var revision = new KnowledgeDocumentRevision(
        revisionId,
        knowledgeDocId,
        KnowledgeSourceId.New(),
        "Rev 1",
        null,
        createdAt.AddMinutes(4),
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "content-hash-disposal",
        "metadata-hash-disposal",
        null,
        null,
        null,
        createdAt.AddMinutes(4));
      await new SqliteKnowledgeRevisionRepository(seedContext).AddAsync(revision);

      var citationId = KnowledgeCitationId.New();
      var citation = new KnowledgeCitation(
        citationId,
        revisionId,
        KnowledgeCitationLocationType.Section,
        "3.1",
        "content-hash-disposal",
        createdAt.AddMinutes(5),
        null);
      await new SqliteKnowledgeCitationRepository(seedContext).AddAsync(citation);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(6));
      diagnostic.RecordSuccess(knowledgeDocId, revisionId, citationId, false, null);

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();

      provenanceId = PromotionProvenanceId.New();
      var provenance = new PromotionProvenance(
        provenanceId,
        seeded.ProjectId,
        revisionId,
        diagnostic.Id,
        candidate.Id,
        "fragment-hash-disposal",
        FragmentCandidateReviewState.HumanAccepted,
        "reviewer@example.invalid",
        createdAt.AddMinutes(10),
        run.Id,
        "plain-text-deterministic",
        "1.0.0",
        "1.0.0",
        "contract-hash-sha256",
        seeded.SourceId,
        seeded.ContentHash,
        "identity-hash-disposal",
        PromotionAttemptId.New(),
        "promoter@example.invalid",
        createdAt.AddMinutes(11));

      var auditRecorder3 = new SqliteAuditRecorder();
      var unitOfWork3 = new SqliteUnitOfWork(seedContext, auditRecorder3, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionProvenanceRepository(seedContext).AddAsync(provenance);
      await unitOfWork3.CommitAsync();
    }

    PromotionProvenance loaded;
    await using (var reloadContext = CreateDbContext())
    {
      loaded = (await new SqlitePromotionProvenanceRepository(reloadContext)
        .GetByRevisionIdAsync(revisionId))!;
    }

    Assert.NotNull(loaded);
    Assert.Equal(provenanceId, loaded!.Id);
    Assert.Equal(seeded.ProjectId, loaded.ProjectId);
    Assert.Equal(revisionId, loaded.PromotedRevisionId);
    Assert.Equal("fragment-hash-disposal", loaded.FragmentSourceContentHash);
    Assert.Equal(FragmentCandidateReviewState.HumanAccepted, loaded.ReviewState);
    Assert.Equal("reviewer@example.invalid", loaded.ReviewedBy);
    Assert.Equal("promoter@example.invalid", loaded.PromotedBy);
  }

  [Fact]
  public async Task QueryOrderingAndBoundsAreDeterministic()
  {
    var seeded = await SeedSourceAsync();
    var createdAt = seeded.CreatedAtUtc;
    FragmentCandidateId targetCandidateId;

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var run = CreateParserRun(seeded.ProjectId, seeded.SourceId, createdAt);
      run.Start(createdAt.AddMinutes(1));
      run.Complete(createdAt.AddMinutes(2), ParserExecutionStatus.Completed);

      var candidate = CreateFragmentCandidate(run.Id, seeded.ProjectId, seeded.SourceId, createdAt.AddMinutes(1));
      targetCandidateId = candidate.Id;

      await new SqliteParserRunRepository(seedContext).AddAsync(run);
      await new SqliteFragmentCandidateRepository(seedContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, run.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();

      var knowledgeDocId = KnowledgeDocumentId.New();
      var knowledgeDoc = new KnowledgeDocument(
        knowledgeDocId,
        seeded.ProjectId,
        KnowledgeDocumentType.Specification,
        "Ordering Test Document",
        null,
        "structural",
        "test@example.invalid",
        createdAt.AddMinutes(3));
      await new SqliteKnowledgeDocumentRepository(seedContext).AddAsync(knowledgeDoc);
      StageAuditEvents(auditRecorder, knowledgeDoc.AuditTrail);

      var revisionId = KnowledgeDocumentRevisionId.New();
      var revision = new KnowledgeDocumentRevision(
        revisionId,
        knowledgeDocId,
        KnowledgeSourceId.New(),
        "Rev 1",
        null,
        createdAt.AddMinutes(4),
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "content-hash-ordering",
        "metadata-hash-ordering",
        null,
        null,
        null,
        createdAt.AddMinutes(4));
      await new SqliteKnowledgeRevisionRepository(seedContext).AddAsync(revision);

      var citationId = KnowledgeCitationId.New();
      var citation = new KnowledgeCitation(
        citationId,
        revisionId,
        KnowledgeCitationLocationType.Paragraph,
        "2.1",
        "content-hash-ordering",
        createdAt.AddMinutes(5),
        null);
      await new SqliteKnowledgeCitationRepository(seedContext).AddAsync(citation);
      await unitOfWork.CommitAsync();

      var diagnostic = new PromotionDiagnostic(
        PromotionDiagnosticId.New(),
        candidate.Id,
        run.Id,
        seeded.ProjectId,
        createdAt.AddMinutes(6));
      diagnostic.RecordSuccess(knowledgeDocId, revisionId, citationId, false, null);

      var auditRecorder2 = new SqliteAuditRecorder();
      var unitOfWork2 = new SqliteUnitOfWork(seedContext, auditRecorder2, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionDiagnosticRepository(seedContext).AddAsync(diagnostic);
      await unitOfWork2.CommitAsync();

      var provenance = new PromotionProvenance(
        PromotionProvenanceId.New(),
        seeded.ProjectId,
        revisionId,
        diagnostic.Id,
        candidate.Id,
        "fragment-hash-ordering",
        FragmentCandidateReviewState.HumanAccepted,
        "reviewer@example.invalid",
        createdAt.AddMinutes(10),
        run.Id,
        "plain-text-deterministic",
        "1.0.0",
        "1.0.0",
        "contract-hash-sha256",
        seeded.SourceId,
        seeded.ContentHash,
        "identity-hash-ordering",
        PromotionAttemptId.New(),
        "promoter@example.invalid",
        createdAt.AddMinutes(11));

      var auditRecorder3 = new SqliteAuditRecorder();
      var unitOfWork3 = new SqliteUnitOfWork(seedContext, auditRecorder3, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqlitePromotionProvenanceRepository(seedContext).AddAsync(provenance);
      await unitOfWork3.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var result = await new SqlitePromotionProvenanceRepository(verificationContext)
      .GetByFragmentCandidateIdAsync(targetCandidateId);

    Assert.NotNull(result);
    Assert.Equal(targetCandidateId, result!.FragmentCandidateId);
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

    var project = new Project(ProjectId.New(), "Promotion Provenance Test Project", "test@example.invalid", createdAtUtc);
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
