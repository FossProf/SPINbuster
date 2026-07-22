using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;
using SPINbuster.Infrastructure.Repositories;
using SPINbuster.Infrastructure.Services;
using System.Globalization;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqliteReportPersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task ReportRepositoryPersistsDraftProvenanceAndReloadsByOperationId()
  {
    var operationId = OperationId.New();

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      var seededInspection = await SeedInspectionContextAsync(seedContext);
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var reportRepository = new SqliteReportRepository(seedContext);
      var report = new Report(
        ReportId.New(),
        seededInspection.ProjectId,
        seededInspection.InspectionSessionId,
        new ReportTitle("Initial Draft Report"),
        [
          new ReportDraftSection("Summary", "Deterministic draft summary."),
          new ReportDraftSection("Observations", "Deterministic draft observations.")
        ],
        [seededInspection.FieldNoteId],
        [seededInspection.EvidenceAttachmentId!.Value],
        "author@example.invalid",
        seededInspection.CreatedAtUtc.AddHours(1));

      await reportRepository.AddAsync(report, operationId);
      StageAuditEvents(auditRecorder, report.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var reportRepositoryForVerification = new SqliteReportRepository(verificationContext);
    var storedByOperationId = await reportRepositoryForVerification.GetByOperationIdAsync(operationId);

    Assert.NotNull(storedByOperationId);
    Assert.Equal("Initial Draft Report", storedByOperationId!.Title.Value);
    Assert.Equal(1, storedByOperationId.RevisionNumber);
    Assert.Equal(ReportLifecycle.Draft, storedByOperationId.Lifecycle);
    Assert.Equal(["Summary", "Observations"], storedByOperationId.Sections.Select(section => section.Heading));
    Assert.Equal(1L, storedByOperationId.SourceFieldNoteIds.Count);
    Assert.Equal(1L, storedByOperationId.SourceEvidenceAttachmentIds.Count);
    Assert.Single(storedByOperationId.AuditTrail);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM reports"));
    Assert.Equal(2L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM report_sections"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM report_field_note_sources"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM report_evidence_sources"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM report_draft_operations"));
  }

  [Fact]
  public async Task ReportCommitRollsBackReportAndOperationMappingWhenStagedAuditInsertFails()
  {
    var duplicateAuditId = AuditEventId.New();

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      seedContext.Add(new AuditEventRecord
      {
        Id = duplicateAuditId,
        SubjectType = nameof(Report),
        SubjectId = "seed-report",
        EventType = "SeedEvent",
        Actor = "seed@example.invalid",
        OccurredAtUtc = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero),
        Description = "Seed audit event.",
      });
      await seedContext.SaveChangesAsync();
    }

    await using (var dbContext = CreateDbContext())
    {
      var seededInspection = await SeedInspectionContextAsync(dbContext);
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var reportRepository = new SqliteReportRepository(dbContext);
      var report = new Report(
        ReportId.New(),
        seededInspection.ProjectId,
        seededInspection.InspectionSessionId,
        new ReportTitle("Rollback Draft Report"),
        [new ReportDraftSection("Summary", "Rollback test section.")],
        [seededInspection.FieldNoteId],
        [seededInspection.EvidenceAttachmentId!.Value],
        "author@example.invalid",
        seededInspection.CreatedAtUtc.AddHours(1));

      await reportRepository.AddAsync(report, OperationId.New());
      auditRecorder.Stage(new AuditEvent(
        duplicateAuditId,
        nameof(Report),
        report.Id.ToString(),
        "ReportCreated",
        "author@example.invalid",
        seededInspection.CreatedAtUtc.AddHours(1),
        "Duplicate audit event for rollback test."));

      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(0L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM reports"));
    Assert.Equal(0L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM report_draft_operations"));
    Assert.Equal(
      1L,
      await QueryCountAsync(
        verificationContext,
        "SELECT COUNT(*) FROM audit_events WHERE SubjectType = 'Report'"));
  }

  [Fact]
  public async Task UniqueOperationIdConstraintPreventsSecondReportDraft()
  {
    var operationId = OperationId.New();
    var firstReportId = ReportId.New();

    await using (var firstContext = CreateDbContext())
    {
      await firstContext.Database.MigrateAsync();
      var seededInspection = await SeedInspectionContextAsync(firstContext);
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(firstContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var reportRepository = new SqliteReportRepository(firstContext);
      var firstReport = new Report(
        firstReportId,
        seededInspection.ProjectId,
        seededInspection.InspectionSessionId,
        new ReportTitle("First Draft Report"),
        [new ReportDraftSection("Summary", "First deterministic draft.")],
        [seededInspection.FieldNoteId],
        [seededInspection.EvidenceAttachmentId!.Value],
        "author@example.invalid",
        seededInspection.CreatedAtUtc.AddHours(1));

      await reportRepository.AddAsync(firstReport, operationId);
      StageAuditEvents(auditRecorder, firstReport.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using (var secondContext = CreateDbContext())
    {
      var seededInspection = await LoadSeededInspectionContextAsync(secondContext);
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(secondContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var reportRepository = new SqliteReportRepository(secondContext);
      var secondReport = new Report(
        ReportId.New(),
        seededInspection.ProjectId,
        seededInspection.InspectionSessionId,
        new ReportTitle("Second Draft Report"),
        [new ReportDraftSection("Summary", "Second deterministic draft.")],
        [seededInspection.FieldNoteId],
        [seededInspection.EvidenceAttachmentId!.Value],
        "author@example.invalid",
        seededInspection.CreatedAtUtc.AddHours(2));

      await reportRepository.AddAsync(secondReport, operationId);
      StageAuditEvents(auditRecorder, secondReport.AuditTrail);

      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM reports"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM report_draft_operations"));
    Assert.NotNull(await new SqliteReportRepository(verificationContext).GetByIdAsync(firstReportId));
    Assert.NotNull(await new SqliteReportRepository(verificationContext).GetByOperationIdAsync(operationId));
  }

  [Fact]
  public async Task PopulatedVerticalSliceDatabaseMigratesWithoutLosingStateAndSupportsReportDraftCreation()
  {
    SeededInspectionContext seededInspection;

    await using (var initialContext = CreateDbContext())
    {
      var migrator = initialContext.GetService<IMigrator>();
      await migrator.MigrateAsync("20260715215425_InitialSqlite");
      seededInspection = await SeedInspectionContextAsync(initialContext, includeEvidence: false);
    }

    await using (var migratedContext = CreateDbContext())
    {
      await migratedContext.Database.MigrateAsync();
      await migratedContext.Database.MigrateAsync();

      var appliedMigrations = (await migratedContext.Database.GetAppliedMigrationsAsync()).ToArray();
      Assert.Equal(9, appliedMigrations.Length);

      var storedProject = await new SqliteProjectRepository(migratedContext).GetByIdAsync(seededInspection.ProjectId);
      var storedInspectionSession = await new SqliteInspectionSessionRepository(migratedContext).GetByIdAsync(seededInspection.InspectionSessionId);

      Assert.NotNull(storedProject);
      Assert.NotNull(storedInspectionSession);
      Assert.Equal(ProjectLifecycle.Active, storedProject!.Lifecycle);
      Assert.Equal(2, storedProject.AuditTrail.Count);
      Assert.Equal(InspectionSessionLifecycle.InProgress, storedInspectionSession!.Lifecycle);
      Assert.Single(storedInspectionSession.FieldNotes);
      Assert.Equal(seededInspection.FieldNoteId, storedInspectionSession.FieldNotes.Single().Id);
      Assert.Equal("Observed corrosion at lower seam.", storedInspectionSession.FieldNotes.Single().RawText.Value);
      Assert.Equal(3, storedInspectionSession.AuditTrail.Count);

      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(migratedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var reportRepository = new SqliteReportRepository(migratedContext);
      var report = new Report(
        ReportId.New(),
        seededInspection.ProjectId,
        seededInspection.InspectionSessionId,
        new ReportTitle("Migrated Draft Report"),
        [new ReportDraftSection("Summary", "Draft created after migration.")],
        [seededInspection.FieldNoteId],
        [],
        "author@example.invalid",
        seededInspection.CreatedAtUtc.AddHours(2));
      var operationId = OperationId.New();

      await reportRepository.AddAsync(report, operationId);
      StageAuditEvents(auditRecorder, report.AuditTrail);
      await unitOfWork.CommitAsync();

      var storedReport = await reportRepository.GetByOperationIdAsync(operationId);
      Assert.NotNull(storedReport);
      Assert.Equal(report.Id, storedReport!.Id);
      Assert.Equal(1, storedReport.RevisionNumber);
      Assert.Equal(ReportLifecycle.Draft, storedReport.Lifecycle);
      Assert.Equal([seededInspection.FieldNoteId], storedReport.SourceFieldNoteIds);
      Assert.Empty(storedReport.SourceEvidenceAttachmentIds);
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
      // SQLite file handles can linger briefly on Windows after the test body
      // completes. A best-effort cleanup is sufficient for these temp files.
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

  private static async Task<SeededInspectionContext> SeedInspectionContextAsync(
    SpinbusterDbContext dbContext,
    bool includeEvidence = true)
  {
    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    var projectRepository = new SqliteProjectRepository(dbContext);
    var inspectionSessionRepository = new SqliteInspectionSessionRepository(dbContext);
    var createdAtUtc = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);
    var project = new Project(ProjectId.New(), "Project Falcon", "owner@example.invalid", createdAtUtc);
    var inspectionSession = new InspectionSession(
      InspectionSessionId.New(),
      project.Id,
      "Initial Walkdown",
      "inspector@example.invalid",
      createdAtUtc.AddMinutes(30));

    project.Activate("inspector@example.invalid", createdAtUtc.AddMinutes(31));
    inspectionSession.Start("inspector@example.invalid", createdAtUtc.AddMinutes(35));
    var fieldNote = inspectionSession.RecordFieldNote(
      FieldNoteId.New(),
      "inspector@example.invalid",
      createdAtUtc.AddHours(1),
      new FieldNoteRawText("Observed corrosion at lower seam."));
    EvidenceAttachment? evidenceAttachment = null;
    if (includeEvidence)
    {
      evidenceAttachment = inspectionSession.AttachEvidence(
        EvidenceAttachmentId.New(),
        "inspector@example.invalid",
        createdAtUtc.AddHours(1).AddMinutes(5),
        new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));
      inspectionSession.InterpretEvidence(
        evidenceAttachment.Id,
        new EvidenceInterpretation(
          "Corrosion visible near lower seam.",
          "reviewer@example.invalid",
          createdAtUtc.AddHours(1).AddMinutes(10)));
    }

    await projectRepository.AddAsync(project);
    await inspectionSessionRepository.AddAsync(inspectionSession);
    StageAuditEvents(auditRecorder, project.AuditTrail);
    StageAuditEvents(auditRecorder, inspectionSession.AuditTrail);
    await unitOfWork.CommitAsync();

    return new SeededInspectionContext(
      project.Id,
      inspectionSession.Id,
      fieldNote.Id,
      evidenceAttachment?.Id,
      createdAtUtc);
  }

  private static async Task<SeededInspectionContext> LoadSeededInspectionContextAsync(SpinbusterDbContext dbContext)
  {
    var projectId = await dbContext.Projects.Select(project => (ProjectId?)project.Id).SingleAsync();
    var inspectionSessionId = await dbContext.InspectionSessions.Select(session => (InspectionSessionId?)session.Id).SingleAsync();
    var fieldNoteId = await dbContext.FieldNotes.Select(fieldNote => (FieldNoteId?)fieldNote.Id).SingleAsync();
    var evidenceAttachmentId = await dbContext.EvidenceAttachments.Select(evidence => (EvidenceAttachmentId?)evidence.Id).SingleAsync();

    return new SeededInspectionContext(
      projectId!.Value,
      inspectionSessionId!.Value,
      fieldNoteId!.Value,
      evidenceAttachmentId!.Value,
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
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

  private sealed record SeededInspectionContext(
    ProjectId ProjectId,
    InspectionSessionId InspectionSessionId,
    FieldNoteId FieldNoteId,
    EvidenceAttachmentId? EvidenceAttachmentId,
    DateTimeOffset CreatedAtUtc);
}
