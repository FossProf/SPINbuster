using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Repositories;
using SPINbuster.Infrastructure.Services;
using System.Globalization;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqliteAiSubstratePersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task AiSubstrateCommitsPersistAndReloadWithAudit()
  {
    var createdAtUtc = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);
    var project = new Project(ProjectId.New(), "Project Falcon", "owner@example.invalid", createdAtUtc);
    project.Activate("inspector@example.invalid", createdAtUtc.AddMinutes(1));
    var inspectionSession = new InspectionSession(
      InspectionSessionId.New(),
      project.Id,
      "Initial Walkdown",
      "inspector@example.invalid",
      createdAtUtc.AddMinutes(2));
    inspectionSession.Start("inspector@example.invalid", createdAtUtc.AddMinutes(3));
    var fieldNote = inspectionSession.RecordFieldNote(
      FieldNoteId.New(),
      "inspector@example.invalid",
      createdAtUtc.AddMinutes(4),
      new FieldNoteRawText("Observed corrosion at lower seam."));
    var evidence = inspectionSession.AttachEvidence(
      EvidenceAttachmentId.New(),
      "inspector@example.invalid",
      createdAtUtc.AddMinutes(5),
      new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));
    inspectionSession.InterpretEvidence(
      evidence.Id,
      new EvidenceInterpretation(
        "Corrosion visible near lower seam.",
        "reviewer@example.invalid",
        createdAtUtc.AddMinutes(6)));
    var report = new Report(
      ReportId.New(),
      project.Id,
      inspectionSession.Id,
      new ReportTitle("Initial Draft Report"),
      [new ReportDraftSection("Summary", "Existing authoritative draft.")],
      [fieldNote.Id],
      [evidence.Id],
      "inspector@example.invalid",
      createdAtUtc.AddMinutes(7));
    var contextManifest = new ContextManifest(
      ContextManifestId.New(),
      project.Id,
      inspectionSession.Id,
      "report-draft-context-policy/1.0",
      [
        new ContextManifestSourceEntry(
          0,
          project.Id,
          ContextSourceType.FieldNote,
          fieldNote.Id.ToString(),
          "raw-v1",
          "hash-field-note",
          AuthorityClassification.Authoritative,
          "Included for report-draft proposal.",
          null,
          false,
          []),
      ],
      [],
      createdAtUtc.AddMinutes(8));
    var modelRun = new ModelRun(
      ModelRunId.New(),
      project.Id,
      inspectionSession.Id,
      report.Id,
      "inspector@example.invalid",
      contextManifest.Id,
      contextManifest.ManifestHash,
      "tier0-deterministic",
      "deterministic-fixture",
      "sha256:deterministic-fixture-v1",
      "report-draft-proposal-default",
      "0.1.0",
      "report-draft-proposal",
      "1.0.0",
      "operation-1",
      "request-fingerprint-1",
      createdAtUtc.AddMinutes(9));
    modelRun.MarkContextBuilding();
    modelRun.MarkContextValidated();
    modelRun.Queue();
    modelRun.StartRunning();
    modelRun.MarkOutputReceived();
    modelRun.MarkSchemaValidating();
    modelRun.MarkPolicyValidating();
    modelRun.MarkReadyForHumanReview();
    var attempt = new ModelRunAttempt(
      ModelRunAttemptId.New(),
      modelRun.Id,
      1,
      "input-hash",
      createdAtUtc.AddMinutes(9),
      createdAtUtc.AddMinutes(10),
      42,
      128,
      96,
      "{\"ok\":true}",
      "output-hash",
      ModelRunFailureClassification.None,
      null);
    var proposal = new AiProposal(
      ProposalId.New(),
      modelRun.Id,
      project.Id,
      inspectionSession.Id,
      report.Id,
      "tier0-deterministic",
      "deterministic-fixture",
      "sha256:deterministic-fixture-v1",
      "report-draft-proposal-default",
      "0.1.0",
      "report-draft-proposal",
      "1.0.0",
      contextManifest.Id,
      contextManifest.ManifestHash,
      createdAtUtc.AddMinutes(10),
      42,
      128,
      96,
      0.2m,
      [fieldNote.Id.ToString(), evidence.Id.ToString()],
      "{\"sections\":[]}");
    proposal.MarkReadyForReview(ConfidenceBand.Medium, ["warning-1"], ["uncertainty-1"]);

    await using (var dbContext = CreateDbContext())
    {
      await dbContext.Database.MigrateAsync();
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteProjectRepository(dbContext).AddAsync(project);
      await new SqliteInspectionSessionRepository(dbContext).AddAsync(inspectionSession);
      await new SqliteReportRepository(dbContext).AddAsync(report, new SPINbuster.Application.OperationId(Guid.NewGuid()));
      await new SqliteContextManifestRepository(dbContext).AddAsync(contextManifest);
      await new SqliteModelRunRepository(dbContext).AddAsync(modelRun);
      await new SqliteModelRunRepository(dbContext).AddAttemptAsync(attempt);
      await new SqliteAiProposalRepository(dbContext).AddAsync(proposal);
      StageAuditEvents(auditRecorder, project.AuditTrail);
      StageAuditEvents(auditRecorder, inspectionSession.AuditTrail);
      StageAuditEvents(auditRecorder, report.AuditTrail);
      auditRecorder.Stage(new AuditEvent(
        AuditEventId.New(),
        nameof(ModelRun),
        modelRun.Id.ToString(),
        "AiModelRunCompleted",
        "inspector@example.invalid",
        createdAtUtc.AddMinutes(10),
        "AI proposal is ready for human review."));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedManifest = await new SqliteContextManifestRepository(verificationContext).GetByIdAsync(contextManifest.Id);
    var storedModelRun = await new SqliteModelRunRepository(verificationContext).GetByIdAsync(modelRun.Id);
    var storedAttempts = await new SqliteModelRunRepository(verificationContext).GetAttemptsAsync(modelRun.Id);
    var storedProposal = await new SqliteAiProposalRepository(verificationContext).GetByIdAsync(proposal.Id);

    Assert.NotNull(storedManifest);
    Assert.Equal(contextManifest.ManifestHash, storedManifest!.ManifestHash);
    Assert.NotNull(storedModelRun);
    Assert.Equal(ModelRunState.ReadyForHumanReview, storedModelRun!.State);
    Assert.Single(storedAttempts);
    Assert.Equal("output-hash", storedAttempts.Single().RawOutputHash);
    Assert.NotNull(storedProposal);
    Assert.Equal(ProposalStatus.ReadyForReview, storedProposal!.Status);
    Assert.False(string.IsNullOrWhiteSpace(storedProposal.StructuredPayloadHash));
    Assert.Equal(["warning-1"], storedProposal.Warnings);
    Assert.Equal(["uncertainty-1"], storedProposal.UncertaintyCodes);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM context_manifests"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM model_runs"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM ai_proposals"));
  }

  [Fact]
  public async Task DuplicateProposalAuditFailureRollsBackProposalAndModelRun()
  {
    var duplicateAuditId = AuditEventId.New();

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      seedContext.AuditEvents.Add(new SPINbuster.Infrastructure.Persistence.Records.AuditEventRecord
      {
        Id = duplicateAuditId,
        SubjectType = nameof(ModelRun),
        SubjectId = "seed-model-run",
        EventType = "SeedEvent",
        Actor = "seed@example.invalid",
        OccurredAtUtc = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero),
        Description = "Seed audit event.",
      });
      await seedContext.SaveChangesAsync();
    }

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var projectId = ProjectId.New();
      var contextManifest = new ContextManifest(
        ContextManifestId.New(),
        projectId,
        InspectionSessionId.New(),
        "report-draft-context-policy/1.0",
        [
          new ContextManifestSourceEntry(
            0,
            projectId,
            ContextSourceType.FieldNote,
            "field-note-1",
            "raw-v1",
            "hash-1",
            AuthorityClassification.Authoritative,
            "Included for report-draft proposal.",
            null,
            false,
            [])
        ],
        [],
        new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
      var modelRun = new ModelRun(
        ModelRunId.New(),
        contextManifest.ProjectId,
        contextManifest.InspectionSessionId,
        ReportId.New(),
        "inspector@example.invalid",
        contextManifest.Id,
        contextManifest.ManifestHash,
        "tier0-deterministic",
        "deterministic-fixture",
        "sha256:deterministic-fixture-v1",
        "report-draft-proposal-default",
        "0.1.0",
        "report-draft-proposal",
        "1.0.0",
        "operation-1",
        "request-fingerprint-1",
        new DateTimeOffset(2026, 7, 15, 9, 1, 0, TimeSpan.Zero));

      await new SqliteContextManifestRepository(dbContext).AddAsync(contextManifest);
      await new SqliteModelRunRepository(dbContext).AddAsync(modelRun);
      auditRecorder.Stage(new AuditEvent(
        duplicateAuditId,
        nameof(ModelRun),
        modelRun.Id.ToString(),
        "AiModelRunCompleted",
        "inspector@example.invalid",
        new DateTimeOffset(2026, 7, 15, 9, 2, 0, TimeSpan.Zero),
        "Duplicate audit event for rollback test."));

      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(0L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM context_manifests"));
    Assert.Equal(0L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM model_runs"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM audit_events"));
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
      // Best-effort cleanup is sufficient for temp SQLite files.
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

  [Fact]
  public async Task PopulatedReportDraftSliceDatabaseMigratesAndPreservesExistingState()
  {
    ProjectId projectId;
    InspectionSessionId inspectionSessionId;
    ReportId reportId;

    await using (var initialContext = CreateDbContext())
    {
      var migrator = initialContext.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
      await migrator.MigrateAsync("20260715231420_ReportDraftSlice");
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(initialContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var projectRepository = new SqliteProjectRepository(initialContext);
      var inspectionSessionRepository = new SqliteInspectionSessionRepository(initialContext);
      var reportRepository = new SqliteReportRepository(initialContext);
      var createdAtUtc = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);
      var project = new Project(ProjectId.New(), "Project Falcon", "owner@example.invalid", createdAtUtc);
      project.Activate("inspector@example.invalid", createdAtUtc.AddMinutes(1));
      var inspectionSession = new InspectionSession(
        InspectionSessionId.New(),
        project.Id,
        "Initial Walkdown",
        "inspector@example.invalid",
        createdAtUtc.AddMinutes(2));
      inspectionSession.Start("inspector@example.invalid", createdAtUtc.AddMinutes(3));
      var fieldNote = inspectionSession.RecordFieldNote(
        FieldNoteId.New(),
        "inspector@example.invalid",
        createdAtUtc.AddMinutes(4),
        new FieldNoteRawText("Observed corrosion at lower seam."));
      var report = new Report(
        ReportId.New(),
        project.Id,
        inspectionSession.Id,
        new ReportTitle("Existing Draft Report"),
        [new ReportDraftSection("Summary", "Existing authoritative draft.")],
        [fieldNote.Id],
        [],
        "inspector@example.invalid",
        createdAtUtc.AddMinutes(5));

      await projectRepository.AddAsync(project);
      await inspectionSessionRepository.AddAsync(inspectionSession);
      await reportRepository.AddAsync(report, new SPINbuster.Application.OperationId(Guid.NewGuid()));
      StageAuditEvents(auditRecorder, project.AuditTrail);
      StageAuditEvents(auditRecorder, inspectionSession.AuditTrail);
      StageAuditEvents(auditRecorder, report.AuditTrail);
      await unitOfWork.CommitAsync();

      projectId = project.Id;
      inspectionSessionId = inspectionSession.Id;
      reportId = report.Id;
    }

    await using (var migratedContext = CreateDbContext())
    {
      await migratedContext.Database.MigrateAsync();
      await migratedContext.Database.MigrateAsync();

      var storedProject = await new SqliteProjectRepository(migratedContext).GetByIdAsync(projectId);
      var storedInspectionSession = await new SqliteInspectionSessionRepository(migratedContext).GetByIdAsync(inspectionSessionId);
      var storedReport = await new SqliteReportRepository(migratedContext).GetByIdAsync(reportId);

      Assert.NotNull(storedProject);
      Assert.NotNull(storedInspectionSession);
      Assert.NotNull(storedReport);
      Assert.Equal(ProjectLifecycle.Active, storedProject!.Lifecycle);
      Assert.Equal(InspectionSessionLifecycle.InProgress, storedInspectionSession!.Lifecycle);
      Assert.Equal("Existing Draft Report", storedReport!.Title.Value);

      var contextManifest = new ContextManifest(
        ContextManifestId.New(),
        projectId,
        inspectionSessionId,
        "report-draft-context-policy/1.0",
        [
          new ContextManifestSourceEntry(
            0,
            projectId,
            ContextSourceType.FieldNote,
            storedInspectionSession.FieldNotes.Single().Id.ToString(),
            "raw-v1",
            "hash-field-note",
            AuthorityClassification.Authoritative,
            "Included for migrated AI proposal.",
            null,
            false,
            [])
        ],
        [],
        new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero));
      var modelRun = new ModelRun(
        ModelRunId.New(),
        projectId,
        inspectionSessionId,
        reportId,
        "inspector@example.invalid",
        contextManifest.Id,
        contextManifest.ManifestHash,
        "tier0-deterministic",
        "deterministic-fixture",
        "sha256:deterministic-fixture-v1",
        "report-draft-proposal-default",
        "0.1.0",
        "report-draft-proposal",
        "1.0.0",
        "operation-after-migration",
        "request-fingerprint-after-migration",
        new DateTimeOffset(2026, 7, 15, 12, 1, 0, TimeSpan.Zero));
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(migratedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteContextManifestRepository(migratedContext).AddAsync(contextManifest);
      await new SqliteModelRunRepository(migratedContext).AddAsync(modelRun);
      auditRecorder.Stage(new AuditEvent(
        AuditEventId.New(),
        nameof(ModelRun),
        modelRun.Id.ToString(),
        "AiModelRunCompleted",
        "inspector@example.invalid",
        new DateTimeOffset(2026, 7, 15, 12, 2, 0, TimeSpan.Zero),
        "Migration follow-on AI model run."));
      await unitOfWork.CommitAsync();

      Assert.Equal(1L, await QueryCountAsync(migratedContext, "SELECT COUNT(*) FROM context_manifests"));
      Assert.Equal(1L, await QueryCountAsync(migratedContext, "SELECT COUNT(*) FROM model_runs"));
    }
  }
}
