using Microsoft.EntityFrameworkCore;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;
using SPINbuster.Infrastructure.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqliteCanonicalIdentityMigrationTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task DistinctDocumentsInOneProjectGetUniqueHashes()
  {
    await using var dbContext = CreateDbContext();
    await SpinbusterDbContext.MigrateWithSha256Async(dbContext);

    var projectId = ProjectId.New();
    await SeedProjectAsync(dbContext, projectId);

    var hashA = ComputeExpectedHash(projectId, KnowledgeDocumentType.Specification, "Section 03 30 00 - Cast-in-Place Concrete", "03 30 00", "Concrete");
    var hashB = ComputeExpectedHash(projectId, KnowledgeDocumentType.Specification, "Section 04 20 00 - Unit Masonry", "04 20 00", "Masonry");

    var recordA = new KnowledgeDocumentRecord
    {
      Id = KnowledgeDocumentId.New(),
      ProjectId = projectId,
      DocumentType = KnowledgeDocumentType.Specification,
      CanonicalTitle = "Section 03 30 00 - Cast-in-Place Concrete",
      ExternalReferenceNumber = "03 30 00",
      DisciplineOrCategory = "Concrete",
      Lifecycle = KnowledgeDocumentLifecycle.Active,
      ConcurrencyToken = 0,
      CanonicalIdentityHash = hashA,
      CreatedBy = "system",
      CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    var recordB = new KnowledgeDocumentRecord
    {
      Id = KnowledgeDocumentId.New(),
      ProjectId = projectId,
      DocumentType = KnowledgeDocumentType.Specification,
      CanonicalTitle = "Section 04 20 00 - Unit Masonry",
      ExternalReferenceNumber = "04 20 00",
      DisciplineOrCategory = "Masonry",
      Lifecycle = KnowledgeDocumentLifecycle.Active,
      ConcurrencyToken = 0,
      CanonicalIdentityHash = hashB,
      CreatedBy = "system",
      CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    dbContext.KnowledgeDocuments.AddRange(recordA, recordB);
    await dbContext.SaveChangesAsync();

    var reloadedA = await dbContext.KnowledgeDocuments.FindAsync(recordA.Id);
    var reloadedB = await dbContext.KnowledgeDocuments.FindAsync(recordB.Id);

    Assert.NotNull(reloadedA);
    Assert.NotNull(reloadedB);
    Assert.Equal(hashA, reloadedA.CanonicalIdentityHash);
    Assert.Equal(hashB, reloadedB.CanonicalIdentityHash);
    Assert.NotEqual(reloadedA.CanonicalIdentityHash, reloadedB.CanonicalIdentityHash);
  }

  [Fact]
  public async Task DuplicateCanonicalIdentityHashDetectedByUniqueIndex()
  {
    await using var dbContext = CreateDbContext();
    await SpinbusterDbContext.MigrateWithSha256Async(dbContext);

    var projectId = ProjectId.New();
    await SeedProjectAsync(dbContext, projectId);

    var sharedHash = "duplicate-test-hash";

    var recordA = new KnowledgeDocumentRecord
    {
      Id = KnowledgeDocumentId.New(),
      ProjectId = projectId,
      DocumentType = KnowledgeDocumentType.Specification,
      CanonicalTitle = "First Document",
      Lifecycle = KnowledgeDocumentLifecycle.Active,
      ConcurrencyToken = 0,
      CanonicalIdentityHash = sharedHash,
      CreatedBy = "system",
      CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    dbContext.KnowledgeDocuments.Add(recordA);
    await dbContext.SaveChangesAsync();

    var recordB = new KnowledgeDocumentRecord
    {
      Id = KnowledgeDocumentId.New(),
      ProjectId = projectId,
      DocumentType = KnowledgeDocumentType.Specification,
      CanonicalTitle = "Second Document",
      Lifecycle = KnowledgeDocumentLifecycle.Active,
      ConcurrencyToken = 0,
      CanonicalIdentityHash = sharedHash,
      CreatedBy = "system",
      CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    dbContext.KnowledgeDocuments.Add(recordB);

    await Assert.ThrowsAsync<DbUpdateException>(
      () => dbContext.SaveChangesAsync());
  }

  [Fact]
  public async Task DifferentProjectsAllowSameCanonicalTitle()
  {
    await using var dbContext = CreateDbContext();
    await SpinbusterDbContext.MigrateWithSha256Async(dbContext);

    var projectA = ProjectId.New();
    var projectB = ProjectId.New();
    await SeedProjectAsync(dbContext, projectA);
    await SeedProjectAsync(dbContext, projectB);

    var recordA = new KnowledgeDocumentRecord
    {
      Id = KnowledgeDocumentId.New(),
      ProjectId = projectA,
      DocumentType = KnowledgeDocumentType.Specification,
      CanonicalTitle = "Shared Title",
      Lifecycle = KnowledgeDocumentLifecycle.Active,
      ConcurrencyToken = 0,
      CanonicalIdentityHash = "hash-for-project-a",
      CreatedBy = "system",
      CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    var recordB = new KnowledgeDocumentRecord
    {
      Id = KnowledgeDocumentId.New(),
      ProjectId = projectB,
      DocumentType = KnowledgeDocumentType.Specification,
      CanonicalTitle = "Shared Title",
      Lifecycle = KnowledgeDocumentLifecycle.Active,
      ConcurrencyToken = 0,
      CanonicalIdentityHash = "hash-for-project-b",
      CreatedBy = "system",
      CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    dbContext.KnowledgeDocuments.AddRange(recordA, recordB);
    await dbContext.SaveChangesAsync();

    var count = await dbContext.KnowledgeDocuments.CountAsync();
    Assert.Equal(2, count);
  }

  [Fact]
  public async Task AllMigrationsApplyCleanlyAndNoPendingMigrations()
  {
    await using var dbContext = CreateDbContext();
    await SpinbusterDbContext.MigrateWithSha256Async(dbContext);

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();

    Assert.Empty(pendingMigrations);
    Assert.True(appliedMigrations.Length >= 17, $"Expected at least 17 migrations, found {appliedMigrations.Length}");
  }

  [Fact]
  public async Task SnapshotModelMatchesAllAppliedMigrations()
  {
    await using var dbContext = CreateDbContext();
    await SpinbusterDbContext.MigrateWithSha256Async(dbContext);

    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
    Assert.Empty(pendingMigrations);

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    Assert.Contains(appliedMigrations, m => m.Contains("ConcurrencyTokenAndCanonicalIdentityHash", StringComparison.Ordinal));
    Assert.Contains(appliedMigrations, m => m.Contains("AttemptAndDiagnosticOwnership", StringComparison.Ordinal));
    Assert.Contains(appliedMigrations, m => m.Contains("GovernedSourceAuthority", StringComparison.Ordinal));
  }

  private static string ComputeExpectedHash(
    ProjectId projectId,
    KnowledgeDocumentType documentType,
    string canonicalTitle,
    string? externalReferenceNumber,
    string? disciplineOrCategory)
  {
    var input = $"{projectId.Value:D}|{documentType}|{canonicalTitle.ToUpperInvariant()}|{externalReferenceNumber?.ToUpperInvariant() ?? string.Empty}|{disciplineOrCategory?.ToUpperInvariant() ?? string.Empty}";
    var bytes = Encoding.UTF8.GetBytes(input);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }

  private static async Task SeedProjectAsync(SpinbusterDbContext dbContext, ProjectId projectId)
  {
    var project = new Project(projectId, $"Test Project {projectId.Value.ToString("N")[..8]}", "system", DateTimeOffset.UtcNow);
    project.Activate("system", DateTimeOffset.UtcNow);
    await new SqliteProjectRepository(dbContext).AddAsync(project);
    await dbContext.SaveChangesAsync();
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
}
