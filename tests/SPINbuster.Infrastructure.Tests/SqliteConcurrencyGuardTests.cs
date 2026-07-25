using Microsoft.EntityFrameworkCore;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Persistence.Records;
using SPINbuster.Infrastructure.Repositories;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqliteConcurrencyGuardTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public async Task UpdateSucceedsWhenTokenMatches()
  {
    var documentId = await SeedTestDataAsync();

    await using (var dbContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(dbContext);
      var domainDocument = await repository.GetByIdAsync(documentId);
      Assert.NotNull(domainDocument);
      Assert.Equal(0, domainDocument.ConcurrencyToken);

      await repository.UpdateAsync(domainDocument, 0);
      await dbContext.SaveChangesAsync();
    }

    await using (var verifyContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(verifyContext);
      var refreshed = await repository.GetByIdAsync(documentId);
      Assert.NotNull(refreshed);
      Assert.Equal(1, refreshed.ConcurrencyToken);
    }
  }

  [Fact]
  public async Task UpdateThrowsWhenTokenMismatched()
  {
    var documentId = await SeedTestDataAsync();

    await using (var dbContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(dbContext);
      var domainDocument = await repository.GetByIdAsync(documentId);
      Assert.NotNull(domainDocument);

      await repository.UpdateAsync(domainDocument, 0);
      await dbContext.SaveChangesAsync();
    }

    await using (var staleContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(staleContext);
      var staleDocument = await repository.GetByIdAsync(documentId);
      Assert.NotNull(staleDocument);
      Assert.Equal(1, staleDocument.ConcurrencyToken);

      await Assert.ThrowsAsync<ConcurrencyConflictException>(
        () => repository.UpdateAsync(staleDocument, 0));
    }
  }

  [Fact]
  public async Task TokenIncrementsAfterSuccessfulUpdate()
  {
    var documentId = await SeedTestDataAsync();

    await using (var dbContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(dbContext);
      var doc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(doc);
      Assert.Equal(0, doc.ConcurrencyToken);

      await repository.UpdateAsync(doc, 0);
      await dbContext.SaveChangesAsync();
    }

    await using (var verifyContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(verifyContext);
      var doc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(doc);
      Assert.Equal(1, doc.ConcurrencyToken);
    }
  }

  [Fact]
  public async Task ConcurrentUpdatesSecondFails()
  {
    var documentId = await SeedTestDataAsync();

    await using (var dbContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(dbContext);
      var doc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(doc);

      await repository.UpdateAsync(doc, 0);
      await dbContext.SaveChangesAsync();
    }

    await using (var staleContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(staleContext);
      var staleDoc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(staleDoc);

      await Assert.ThrowsAsync<ConcurrencyConflictException>(
        () => repository.UpdateAsync(staleDoc, 99));
    }
  }

  private async Task<KnowledgeDocumentId> SeedTestDataAsync()
  {
    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();

    var project = new Project(ProjectId.New(), "Test", "system", DateTimeOffset.UtcNow);
    project.Activate("system", DateTimeOffset.UtcNow);
    await new SqliteProjectRepository(dbContext).AddAsync(project);

    var documentId = KnowledgeDocumentId.New();
    var record = new KnowledgeDocumentRecord
    {
      Id = documentId,
      ProjectId = project.Id,
      DocumentType = KnowledgeDocumentType.Specification,
      CanonicalTitle = "Test Document",
      Lifecycle = KnowledgeDocumentLifecycle.Active,
      ConcurrencyToken = 0,
      CreatedBy = "system",
      CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    dbContext.KnowledgeDocuments.Add(record);
    await dbContext.SaveChangesAsync();
    return documentId;
  }

  private SpinbusterDbContext CreateDbContext()
  {
    return new SpinbusterDbContext(
      new DbContextOptionsBuilder<SpinbusterDbContext>()
        .UseSqlite($"Data Source={_databasePath}")
        .Options);
  }

  public void Dispose()
  {
    if (File.Exists(_databasePath))
    {
      try { File.Delete(_databasePath); } catch { }
    }
  }
}
