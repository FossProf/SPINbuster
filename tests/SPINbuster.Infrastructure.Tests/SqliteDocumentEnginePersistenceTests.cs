using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Repositories;
using SPINbuster.Infrastructure.Services;
using System.Globalization;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqliteDocumentEnginePersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task ImportedDocumentSourceAndAuditPersistAndReload()
  {
    var project = await SeedProjectAsync();
    var storageObject = new StorageObject(
      StorageObjectId.New(),
      "document-engine-foundation",
      "storage-1",
      12,
      "ABC",
      "SHA-256",
      1,
      project.CreatedAtUtc.AddMinutes(1),
      null,
      StorageAvailabilityState.Available);
    var importSession = new DocumentImportSession(
      DocumentImportSessionId.New(),
      project.ProjectId,
      "importer@example.invalid",
      project.CreatedAtUtc.AddMinutes(2));
    importSession.BeginValidation("importer@example.invalid", project.CreatedAtUtc.AddMinutes(3));
    importSession.BeginImporting("importer@example.invalid", project.CreatedAtUtc.AddMinutes(4));
    var source = new ImportedDocumentSource(
      ImportedSourceId.New(),
      importSession.Id,
      project.ProjectId,
      "detail.pdf",
      "application/pdf",
      "application/pdf",
      12,
      "ABC",
      "SHA-256",
      1,
      storageObject.ToReference(),
      ImportedSourceOrigin.LocalFile,
      "importer@example.invalid",
      project.CreatedAtUtc.AddMinutes(5),
      ImportedDocumentSourceStatus.Available,
      null);
    importSession.RecordAcceptedSource(source.Id, "importer@example.invalid", project.CreatedAtUtc.AddMinutes(5));
    importSession.Complete("importer@example.invalid", project.CreatedAtUtc.AddMinutes(6));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteStorageObjectRepository(dbContext).AddAsync(storageObject);
      await new SqliteDocumentImportSessionRepository(dbContext).AddAsync(importSession);
      await new SqliteImportedDocumentSourceRepository(dbContext).AddAsync(source);
      StageAuditEvents(auditRecorder, importSession.AuditTrail);
      StageAuditEvents(auditRecorder, source.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedSession = await new SqliteDocumentImportSessionRepository(verificationContext).GetByIdAsync(importSession.Id);
    var storedSource = await new SqliteImportedDocumentSourceRepository(verificationContext).GetByIdAsync(source.Id);

    Assert.NotNull(storedSession);
    Assert.NotNull(storedSource);
    Assert.Equal(DocumentImportSessionState.Completed, storedSession!.State);
    Assert.Equal("detail.pdf", storedSource!.OriginalFileName);
    Assert.Equal(storageObject.Id, storedSource.StorageReference.StorageObjectId);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM imported_document_sources"));
  }

  [Fact]
  public async Task ProcessingAttemptCandidateAndAuditPersistAndReload()
  {
    var seeded = await SeedImportedSourceAsync();
    var attempt = new DocumentProcessingAttempt(
      DocumentProcessingAttemptId.New(),
      seeded.Source.Id,
      seeded.ProjectId,
      "fixture",
      "fixture-processor",
      "1.0.0",
      seeded.CreatedAtUtc.AddMinutes(10),
      1,
      seeded.Source.ContentHash);
    attempt.Start(seeded.CreatedAtUtc.AddMinutes(11));
    attempt.MarkOutputReceived(seeded.CreatedAtUtc.AddMinutes(12), "OUTPUT");
    attempt.BeginValidation(seeded.CreatedAtUtc.AddMinutes(13));
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(),
      seeded.ProjectId,
      seeded.Source.Id,
      attempt.Id,
      DocumentCandidateType.MetadataCandidate,
      "document-metadata-candidate",
      "1.0.0",
      """{"title":"detail.pdf"}""",
      seeded.Source.ContentHash,
      null,
      ConfidenceBand.High,
      [],
      seeded.CreatedAtUtc.AddMinutes(14));
    candidate.MarkValidated(seeded.CreatedAtUtc.AddMinutes(15));
    candidate.MarkReadyForReview(seeded.CreatedAtUtc.AddMinutes(16));
    attempt.Complete(seeded.CreatedAtUtc.AddMinutes(17));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteDocumentProcessingAttemptRepository(dbContext).AddAsync(attempt);
      await new SqliteDocumentCandidateRepository(dbContext).AddAsync(candidate);
      StageAuditEvents(auditRecorder, attempt.AuditTrail);
      StageAuditEvents(auditRecorder, candidate.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedAttempt = await new SqliteDocumentProcessingAttemptRepository(verificationContext).GetByIdAsync(attempt.Id);
    var storedCandidate = await new SqliteDocumentCandidateRepository(verificationContext).GetByIdAsync(candidate.Id);

    Assert.NotNull(storedAttempt);
    Assert.NotNull(storedCandidate);
    Assert.Equal(DocumentProcessingAttemptState.Completed, storedAttempt!.State);
    Assert.Equal(DocumentCandidateStatus.ReadyForReview, storedCandidate!.Status);
    Assert.Equal(candidate.PayloadHash, storedCandidate.PayloadHash);
  }

  [Fact]
  public async Task MigrationMetadataAndRepeatedMigrationIncludeDocumentSlice()
  {
    await using var dbContext = CreateDbContext();
    var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();

    Assert.Contains(
      migrationsAssembly.Migrations.Keys,
      migration => migration.EndsWith("DocumentEngineFoundationRc", StringComparison.Ordinal));

    await dbContext.Database.MigrateAsync();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();

    Assert.Contains(appliedMigrations, migration => migration.EndsWith("DocumentEngineFoundationRc", StringComparison.Ordinal));
    Assert.Equal(appliedMigrations.Length, await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM __EFMigrationsHistory"));
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

  private async Task<SeededProjectContext> SeedProjectAsync()
  {
    var createdAtUtc = new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);

    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();
    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    var project = new Project(ProjectId.New(), "Project Falcon", "owner@example.invalid", createdAtUtc);
    project.Activate("owner@example.invalid", createdAtUtc.AddMinutes(1));
    await new SqliteProjectRepository(dbContext).AddAsync(project);
    StageAuditEvents(auditRecorder, project.AuditTrail);
    await unitOfWork.CommitAsync();
    return new SeededProjectContext(project.Id, createdAtUtc);
  }

  private async Task<SeededImportedSourceContext> SeedImportedSourceAsync()
  {
    var project = await SeedProjectAsync();
    var storageObject = new StorageObject(
      StorageObjectId.New(),
      "document-engine-foundation",
      "storage-1",
      12,
      "ABC",
      "SHA-256",
      1,
      project.CreatedAtUtc.AddMinutes(1),
      null,
      StorageAvailabilityState.Available);
    var importSession = new DocumentImportSession(
      DocumentImportSessionId.New(),
      project.ProjectId,
      "importer@example.invalid",
      project.CreatedAtUtc.AddMinutes(2));
    importSession.BeginValidation("importer@example.invalid", project.CreatedAtUtc.AddMinutes(3));
    importSession.BeginImporting("importer@example.invalid", project.CreatedAtUtc.AddMinutes(4));
    var source = new ImportedDocumentSource(
      ImportedSourceId.New(),
      importSession.Id,
      project.ProjectId,
      "detail.pdf",
      "application/pdf",
      "application/pdf",
      12,
      "ABC",
      "SHA-256",
      1,
      storageObject.ToReference(),
      ImportedSourceOrigin.LocalFile,
      "importer@example.invalid",
      project.CreatedAtUtc.AddMinutes(5),
      ImportedDocumentSourceStatus.Available,
      null);
    importSession.RecordAcceptedSource(source.Id, "importer@example.invalid", project.CreatedAtUtc.AddMinutes(5));
    importSession.Complete("importer@example.invalid", project.CreatedAtUtc.AddMinutes(6));

    await using var dbContext = CreateDbContext();
    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    await new SqliteStorageObjectRepository(dbContext).AddAsync(storageObject);
    await new SqliteDocumentImportSessionRepository(dbContext).AddAsync(importSession);
    await new SqliteImportedDocumentSourceRepository(dbContext).AddAsync(source);
    StageAuditEvents(auditRecorder, importSession.AuditTrail);
    StageAuditEvents(auditRecorder, source.AuditTrail);
    await unitOfWork.CommitAsync();

    return new SeededImportedSourceContext(project.ProjectId, source, project.CreatedAtUtc);
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

  private sealed record SeededProjectContext(ProjectId ProjectId, DateTimeOffset CreatedAtUtc);

  private sealed record SeededImportedSourceContext(ProjectId ProjectId, ImportedDocumentSource Source, DateTimeOffset CreatedAtUtc);
}
