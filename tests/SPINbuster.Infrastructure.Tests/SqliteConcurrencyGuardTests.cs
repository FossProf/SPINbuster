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
  public async Task UpdateSucceedsWhenNoConcurrentModification()
  {
    var documentId = await SeedTestDataAsync();

    await using (var dbContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(dbContext);
      var domainDocument = await repository.GetByIdAsync(documentId);
      Assert.NotNull(domainDocument);
      Assert.Equal(0, domainDocument.ConcurrencyToken);

      await repository.UpdateAsync(domainDocument);
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
  public async Task ConcurrentUpdateThrowsDbUpdateConcurrencyException()
  {
    var documentId = await SeedTestDataAsync();

    await using (var contextB = CreateDbContext())
    {
      var trackedRecord = await contextB.KnowledgeDocuments.FindAsync(documentId);
      Assert.NotNull(trackedRecord);
      Assert.Equal(0, trackedRecord.ConcurrencyToken);

      await using (var contextA = CreateDbContext())
      {
        var repository = new SqliteKnowledgeDocumentRepository(contextA);
        var doc = await repository.GetByIdAsync(documentId);
        Assert.NotNull(doc);
        await repository.UpdateAsync(doc);
        await contextA.SaveChangesAsync();
      }

      trackedRecord.Lifecycle = KnowledgeDocumentLifecycle.Archived;

      await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
        () => contextB.SaveChangesAsync());
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

      await repository.UpdateAsync(doc);
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
  public async Task DoubleUpdateOnSameContextIncrementsTokenTwice()
  {
    var documentId = await SeedTestDataAsync();

    await using (var dbContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(dbContext);
      var doc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(doc);
      Assert.Equal(0, doc.ConcurrencyToken);

      await repository.UpdateAsync(doc);
      await dbContext.SaveChangesAsync();
    }

    await using (var verifyContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(verifyContext);
      var doc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(doc);
      Assert.Equal(1, doc.ConcurrencyToken);
    }

    await using (var secondUpdateContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(secondUpdateContext);
      var doc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(doc);
      Assert.Equal(1, doc.ConcurrencyToken);

      await repository.UpdateAsync(doc);
      await secondUpdateContext.SaveChangesAsync();
    }

    await using (var finalVerifyContext = CreateDbContext())
    {
      var repository = new SqliteKnowledgeDocumentRepository(finalVerifyContext);
      var doc = await repository.GetByIdAsync(documentId);
      Assert.NotNull(doc);
      Assert.Equal(2, doc.ConcurrencyToken);
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
      CanonicalIdentityHash = "test-hash",
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
