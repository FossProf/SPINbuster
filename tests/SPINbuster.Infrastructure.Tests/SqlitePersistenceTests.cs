using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;
using SPINbuster.Infrastructure.Repositories;
using SPINbuster.Infrastructure.Services;
using System.Globalization;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqlitePersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task CommitPersistsAggregateAndStagedAuditTogether()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder);
    var projectRepository = new SqliteProjectRepository(dbContext);
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));

    await projectRepository.AddAsync(project);
    StageAuditEvents(auditRecorder, project.AuditTrail);
    await unitOfWork.CommitAsync();

    await using var verificationContext = CreateDbContext();
    var storedProject = await new SqliteProjectRepository(verificationContext).GetByIdAsync(project.Id);

    Assert.NotNull(storedProject);
    Assert.Single(storedProject!.AuditTrail);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM projects"));
  }

  [Fact]
  public async Task CommitRollsBackAggregateWhenStagedAuditInsertFails()
  {
    var duplicateAuditId = AuditEventId.New();

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      seedContext.Add(new AuditEventRecord
      {
        Id = duplicateAuditId,
        SubjectType = nameof(Project),
        SubjectId = "seed-project",
        EventType = "SeedEvent",
        Actor = "seed@example.invalid",
        OccurredAtUtc = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero),
        Description = "Seed audit event.",
      });
      await seedContext.SaveChangesAsync();
    }

    await using var dbContext = CreateDbContext();
    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder);
    var projectRepository = new SqliteProjectRepository(dbContext);
    var project = new Project(
      ProjectId.New(),
      "Project Viper",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));

    await projectRepository.AddAsync(project);
    auditRecorder.Stage(new AuditEvent(
      duplicateAuditId,
      nameof(Project),
      project.Id.ToString(),
      "ProjectCreated",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero),
      "Duplicate audit event for rollback test."));

    await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());

    await using var verificationContext = CreateDbContext();
    Assert.Null(await new SqliteProjectRepository(verificationContext).GetByIdAsync(project.Id));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM audit_events"));
  }

  [Fact]
  public async Task ProjectRepositorySupportsDetachedUpdates()
  {
    var projectId = ProjectId.New();
    var createdAtUtc = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder);
      var projectRepository = new SqliteProjectRepository(seedContext);
      var project = new Project(projectId, "Project Falcon", "owner@example.invalid", createdAtUtc);

      await projectRepository.AddAsync(project);
      StageAuditEvents(auditRecorder, project.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    Project detachedProject;
    await using (var loadContext = CreateDbContext())
    {
      detachedProject = (await new SqliteProjectRepository(loadContext).GetByIdAsync(projectId))!;
    }

    detachedProject.Activate("inspector@example.invalid", createdAtUtc.AddHours(1));

    await using (var updateContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(updateContext, auditRecorder);
      var projectRepository = new SqliteProjectRepository(updateContext);

      await projectRepository.UpdateAsync(detachedProject);
      StageAuditEvents(auditRecorder, detachedProject.AuditTrail.Skip(1));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedProject = await new SqliteProjectRepository(verificationContext).GetByIdAsync(projectId);

    Assert.NotNull(storedProject);
    Assert.Equal(ProjectLifecycle.Active, storedProject!.Lifecycle);
    Assert.Equal(2, storedProject.AuditTrail.Count);
  }

  [Fact]
  public async Task InspectionSessionRepositorySupportsDetachedUpdatesForChildAppendsAndInterpretation()
  {
    var projectId = ProjectId.New();
    var sessionId = InspectionSessionId.New();
    var createdAtUtc = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder);
      var projectRepository = new SqliteProjectRepository(seedContext);
      var inspectionSessionRepository = new SqliteInspectionSessionRepository(seedContext);
      var project = new Project(projectId, "Project Falcon", "owner@example.invalid", createdAtUtc);
      var inspectionSession = new InspectionSession(
        sessionId,
        projectId,
        "Initial Walkdown",
        "inspector@example.invalid",
        createdAtUtc.AddMinutes(30));

      project.Activate("inspector@example.invalid", createdAtUtc.AddMinutes(31));
      inspectionSession.Start("inspector@example.invalid", createdAtUtc.AddMinutes(35));

      await projectRepository.AddAsync(project);
      await inspectionSessionRepository.AddAsync(inspectionSession);
      StageAuditEvents(auditRecorder, project.AuditTrail);
      StageAuditEvents(auditRecorder, inspectionSession.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    InspectionSession detachedSession;
    await using (var loadContext = CreateDbContext())
    {
      detachedSession = (await new SqliteInspectionSessionRepository(loadContext).GetByIdAsync(sessionId))!;
    }

    var initialAuditCount = detachedSession.AuditTrail.Count;
    detachedSession.RecordFieldNote(
      FieldNoteId.New(),
      "inspector@example.invalid",
      createdAtUtc.AddHours(1),
      new FieldNoteRawText("Observed label mismatch."));
    var evidence = detachedSession.AttachEvidence(
      EvidenceAttachmentId.New(),
      "inspector@example.invalid",
      createdAtUtc.AddHours(1).AddMinutes(5),
      new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));
    detachedSession.InterpretEvidence(
      evidence.Id,
      new EvidenceInterpretation(
        "Corrosion visible near lower seam.",
        "reviewer@example.invalid",
        createdAtUtc.AddHours(1).AddMinutes(10)));

    await using (var updateContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(updateContext, auditRecorder);
      var inspectionSessionRepository = new SqliteInspectionSessionRepository(updateContext);

      await inspectionSessionRepository.UpdateAsync(detachedSession);
      StageAuditEvents(auditRecorder, detachedSession.AuditTrail.Skip(initialAuditCount));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedSession = await new SqliteInspectionSessionRepository(verificationContext).GetByIdAsync(sessionId);

    Assert.NotNull(storedSession);
    Assert.Single(storedSession!.FieldNotes);
    Assert.Single(storedSession.EvidenceAttachments);
    Assert.NotNull(storedSession.EvidenceAttachments[0].Interpretation);
    Assert.Equal(5, storedSession.AuditTrail.Count);
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

  [Fact]
  public async Task MigrationMetadataIncludesInitialSqliteMigration()
  {
    await using var dbContext = CreateDbContext();
    var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();

    Assert.Contains(
      migrationsAssembly.Migrations.Keys,
      migration => migration.EndsWith("InitialSqlite", StringComparison.Ordinal));
  }

  [Fact]
  public async Task EmptyDatabaseMigratesAndRecordsHistory()
  {
    await using var dbContext = CreateDbContext();

    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();

    Assert.Equal(2, appliedMigrations.Length);
    Assert.Contains(appliedMigrations, migration => migration.EndsWith("InitialSqlite", StringComparison.Ordinal));
    Assert.Contains(appliedMigrations, migration => migration.EndsWith("ReportDraftSlice", StringComparison.Ordinal));
    Assert.Equal(2L, await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM __EFMigrationsHistory"));
  }

  [Fact]
  public async Task SecondMigrationExecutionIsIdempotent()
  {
    await using var dbContext = CreateDbContext();

    await dbContext.Database.MigrateAsync();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();

    Assert.Equal(2, appliedMigrations.Length);
    Assert.Contains(appliedMigrations, migration => migration.EndsWith("InitialSqlite", StringComparison.Ordinal));
    Assert.Contains(appliedMigrations, migration => migration.EndsWith("ReportDraftSlice", StringComparison.Ordinal));
    Assert.Equal(2L, await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM __EFMigrationsHistory"));
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
}
