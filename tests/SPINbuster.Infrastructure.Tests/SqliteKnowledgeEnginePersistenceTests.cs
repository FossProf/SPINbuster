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
using System.Security.Cryptography;
using System.Globalization;

namespace SPINbuster.Infrastructure.Tests;

public sealed class SqliteKnowledgeEnginePersistenceTests : IDisposable
{
  private readonly string _databasePath = Path.Combine(
    Path.GetTempPath(),
    "spinbuster-tests",
    $"{Guid.NewGuid():N}.sqlite");

  [Fact]
  public void ReleasedKnowledgeEngineMigrationFilesRemainByteStable()
  {
    Assert.Equal(
      "86F56D322BD6AEFEA26FD4640CD70AA5FE62D170637AE6AB72335A777FD095AE",
      ComputeFileHash(Path.Combine("src", "SPINbuster.Infrastructure", "Persistence", "Migrations", "20260716184900_KnowledgeEnginePersistenceRc2.cs")));
    Assert.Equal(
      "835C1089E078CC86031CC9235EC31A65946AF2C22E7EC9D0D70F6A1E64CA83F3",
      ComputeFileHash(Path.Combine("src", "SPINbuster.Infrastructure", "Persistence", "Migrations", "20260716185107_KnowledgeEnginePersistenceSnapshotAlignment.cs")));
  }

  [Fact]
  public async Task KnowledgeDocumentRepositoryPersistsAndReloadsProjectScopedDocument()
  {
    var seededProject = await SeedProjectAsync();
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededProject.ProjectId,
      KnowledgeDocumentType.Specification,
      "Section 03 30 00 Cast-In-Place Concrete",
      "03 30 00",
      "Concrete",
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(10));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var documentRepository = new SqliteKnowledgeDocumentRepository(dbContext);

      await documentRepository.AddAsync(knowledgeDocument);
      StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(knowledgeDocument.Id);
    var projectDocuments = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByProjectAsync(seededProject.ProjectId);

    Assert.NotNull(storedDocument);
    Assert.Equal(KnowledgeDocumentType.Specification, storedDocument!.DocumentType);
    Assert.Equal("Section 03 30 00 Cast-In-Place Concrete", storedDocument.CanonicalTitle);
    Assert.Single(storedDocument.AuditTrail);
    Assert.Single(projectDocuments);
    Assert.Equal(storedDocument.Id, projectDocuments.Single().Id);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_documents"));
  }

  [Fact]
  public async Task KnowledgeRevisionPersistenceSupportsSupersessionHistoryCurrentRevisionAndCitations()
  {
    var seededDocument = await SeedKnowledgeDocumentAsync();
    var initialRevision = CreateRevision(
      seededDocument.Document.Id,
      "A",
      seededDocument.CreatedAtUtc.AddMinutes(11),
      seededDocument.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    seededDocument.Document.AddInitialRevision(
      initialRevision,
      "author@example.invalid",
      seededDocument.CreatedAtUtc.AddMinutes(12));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var documentRepository = new SqliteKnowledgeDocumentRepository(dbContext);
      var revisionRepository = new SqliteKnowledgeRevisionRepository(dbContext);
      var citationRepository = new SqliteKnowledgeCitationRepository(dbContext);

      await documentRepository.UpdateAsync(seededDocument.Document);
      await revisionRepository.AddAsync(initialRevision);
      await citationRepository.AddAsync(new KnowledgeCitation(
        KnowledgeCitationId.New(),
        initialRevision.Id,
        KnowledgeCitationLocationType.Section,
        "2.1.A",
        initialRevision.ContentHash,
        seededDocument.CreatedAtUtc.AddMinutes(13),
        "Minimum compressive strength requirement."));
      StageAuditEvents(auditRecorder, seededDocument.Document.AuditTrail.Skip(seededDocument.InitialAuditCount));
      await unitOfWork.CommitAsync();
    }

    KnowledgeDocument detachedDocument;
    await using (var loadContext = CreateDbContext())
    {
      detachedDocument = (await new SqliteKnowledgeDocumentRepository(loadContext).GetByIdAsync(seededDocument.Document.Id))!;
    }

    var successorRevision = CreateRevision(
      detachedDocument.Id,
      "B",
      seededDocument.CreatedAtUtc.AddHours(1),
      seededDocument.CreatedAtUtc.AddHours(1).AddMinutes(1),
      detachedDocument.CurrentAuthoritativeRevisionId);
    var priorAuditCount = detachedDocument.AuditTrail.Count;
    var supersession = detachedDocument.SupersedeCurrentRevision(
      successorRevision,
      "reviewer@example.invalid",
      seededDocument.CreatedAtUtc.AddHours(1).AddMinutes(2));

    await using (var updateContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(updateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var documentRepository = new SqliteKnowledgeDocumentRepository(updateContext);
      var revisionRepository = new SqliteKnowledgeRevisionRepository(updateContext);
      var citationRepository = new SqliteKnowledgeCitationRepository(updateContext);

      await documentRepository.UpdateAsync(detachedDocument);
      await revisionRepository.UpdateAsync(supersession.SupersededRevision);
      await revisionRepository.AddAsync(supersession.SuccessorRevision);
      await citationRepository.AddAsync(new KnowledgeCitation(
        KnowledgeCitationId.New(),
        successorRevision.Id,
        KnowledgeCitationLocationType.Paragraph,
        "3.2",
        successorRevision.ContentHash,
        seededDocument.CreatedAtUtc.AddHours(1).AddMinutes(3),
        "Revised curing language."));
      StageAuditEvents(auditRecorder, detachedDocument.AuditTrail.Skip(priorAuditCount));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(seededDocument.Document.Id);
    var currentRevision = await new SqliteKnowledgeRevisionRepository(verificationContext).GetCurrentByDocumentIdAsync(seededDocument.Document.Id);
    var revisionHistory = await new SqliteKnowledgeRevisionRepository(verificationContext).GetByDocumentIdAsync(seededDocument.Document.Id);
    var initialCitations = await new SqliteKnowledgeCitationRepository(verificationContext).GetByRevisionIdAsync(initialRevision.Id);
    var successorCitations = await new SqliteKnowledgeCitationRepository(verificationContext).GetByRevisionIdAsync(successorRevision.Id);

    Assert.NotNull(storedDocument);
    Assert.Equal(successorRevision.Id, storedDocument!.CurrentAuthoritativeRevisionId);
    Assert.Equal(2, storedDocument.Revisions.Count);
    Assert.Equal(4, storedDocument.AuditTrail.Count);
    Assert.NotNull(currentRevision);
    Assert.Equal(successorRevision.Id, currentRevision!.Id);
    Assert.Equal(KnowledgeRevisionLifecycle.Superseded, revisionHistory.Single(revision => revision.Id == initialRevision.Id).Lifecycle);
    Assert.Equal(successorRevision.Id, revisionHistory.Single(revision => revision.Id == initialRevision.Id).SupersededByRevisionId);
    Assert.Equal(KnowledgeRevisionLifecycle.CurrentAuthoritative, revisionHistory.Single(revision => revision.Id == successorRevision.Id).Lifecycle);
    Assert.Single(initialCitations);
    Assert.Single(successorCitations);
    Assert.Equal(2L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_document_revisions"));
    Assert.Equal(2L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_citations"));
  }

  [Fact]
  public async Task DuplicateRevisionLabelsAreRejectedByPersistenceConstraint()
  {
    var seededDocument = await SeedKnowledgeDocumentAsync();
    var initialRevision = CreateRevision(
      seededDocument.Document.Id,
      "A",
      seededDocument.CreatedAtUtc.AddMinutes(11),
      seededDocument.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    seededDocument.Document.AddInitialRevision(
      initialRevision,
      "author@example.invalid",
      seededDocument.CreatedAtUtc.AddMinutes(12));

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteKnowledgeDocumentRepository(seedContext).UpdateAsync(seededDocument.Document);
      await new SqliteKnowledgeRevisionRepository(seedContext).AddAsync(initialRevision);
      StageAuditEvents(auditRecorder, seededDocument.Document.AuditTrail.Skip(seededDocument.InitialAuditCount));
      await unitOfWork.CommitAsync();
    }

    await using (var duplicateContext = CreateDbContext())
    {
      var duplicateRevision = CreateRevision(
        seededDocument.Document.Id,
        "a",
        seededDocument.CreatedAtUtc.AddHours(1),
        seededDocument.CreatedAtUtc.AddHours(1).AddMinutes(1),
        supersedesRevisionId: initialRevision.Id);
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(duplicateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteKnowledgeRevisionRepository(duplicateContext).AddAsync(duplicateRevision);

      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_document_revisions"));
  }

  [Fact]
  public async Task KnowledgeRelationshipsPersistTraverseAndRejectDuplicates()
  {
    var seededDocument = await SeedKnowledgeDocumentAsync();
    var targetDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededDocument.Document.ProjectId,
      KnowledgeDocumentType.Drawing,
      "Clarifier 6 Anchor Detail",
      "S4.12",
      "Structural",
      "author@example.invalid",
      seededDocument.CreatedAtUtc.AddMinutes(20));

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      var documentRepository = new SqliteKnowledgeDocumentRepository(seedContext);

      await documentRepository.AddAsync(targetDocument);
      StageAuditEvents(auditRecorder, targetDocument.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    var relationship = new KnowledgeRelationship(
      KnowledgeRelationshipId.New(),
      seededDocument.Document.ProjectId,
      KnowledgeSubjectReference.ForDocument(seededDocument.Document.ProjectId, seededDocument.Document.Id),
      KnowledgeSubjectReference.ForDocument(seededDocument.Document.ProjectId, targetDocument.Id),
      KnowledgeRelationshipType.References,
      "Specification references the anchor detail sheet.",
      "reviewer@example.invalid",
      seededDocument.CreatedAtUtc.AddMinutes(25));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteKnowledgeRelationshipRepository(dbContext).AddAsync(relationship);
      StageAuditEvents(auditRecorder, relationship.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using (var duplicateContext = CreateDbContext())
    {
      var duplicateRelationship = new KnowledgeRelationship(
        KnowledgeRelationshipId.New(),
        seededDocument.Document.ProjectId,
        relationship.Source,
        relationship.Target,
        relationship.RelationshipType,
        "Duplicate relationship should fail.",
        "reviewer@example.invalid",
        seededDocument.CreatedAtUtc.AddMinutes(26));
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(duplicateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteKnowledgeRelationshipRepository(duplicateContext).AddAsync(duplicateRelationship);
      StageAuditEvents(auditRecorder, duplicateRelationship.AuditTrail);
      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    KnowledgeRelationship detachedRelationship;
    await using (var loadContext = CreateDbContext())
    {
      detachedRelationship = (await new SqliteKnowledgeRelationshipRepository(loadContext).GetByIdAsync(relationship.Id))!;
    }

    detachedRelationship.UpdateVerificationStatus(KnowledgeVerificationStatus.Verified);

    await using (var updateContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(updateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteKnowledgeRelationshipRepository(updateContext).UpdateAsync(detachedRelationship);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedRelationship = await new SqliteKnowledgeRelationshipRepository(verificationContext).GetByIdAsync(relationship.Id);
    var boundedNeighborhood = await new SqliteKnowledgeRelationshipRepository(verificationContext).GetBySubjectAsync(
      seededDocument.Document.ProjectId,
      relationship.Source,
      maxResults: 1);

    Assert.NotNull(storedRelationship);
    Assert.Equal(KnowledgeVerificationStatus.Verified, storedRelationship!.VerificationStatus);
    Assert.Single(storedRelationship.AuditTrail);
    Assert.Single(boundedNeighborhood);
    Assert.Equal(relationship.Id, boundedNeighborhood.Single().Id);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_relationships"));
  }

  [Fact]
  public async Task KnowledgeDocumentRevisionAndAuditCommitAtomically()
  {
    var seededDocument = await SeedKnowledgeDocumentAsync();
    var revision = CreateRevision(
      seededDocument.Document.Id,
      "A",
      seededDocument.CreatedAtUtc.AddMinutes(11),
      seededDocument.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    seededDocument.Document.AddInitialRevision(
      revision,
      "author@example.invalid",
      seededDocument.CreatedAtUtc.AddMinutes(12));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteKnowledgeDocumentRepository(dbContext).UpdateAsync(seededDocument.Document);
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revision);
      StageAuditEvents(auditRecorder, seededDocument.Document.AuditTrail.Skip(seededDocument.InitialAuditCount));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_documents"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_document_revisions"));
    Assert.Equal(2L, await QueryCountAsync(
      verificationContext,
      "SELECT COUNT(*) FROM audit_events WHERE SubjectType = 'KnowledgeDocument'"));
  }

  [Fact]
  public async Task KnowledgeDocumentRevisionAndAuditRollbackTogetherOnAuditFailure()
  {
    var duplicateAuditId = AuditEventId.New();
    var seededDocument = await SeedKnowledgeDocumentAsync();
    var revision = CreateRevision(
      seededDocument.Document.Id,
      "A",
      seededDocument.CreatedAtUtc.AddMinutes(11),
      seededDocument.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    seededDocument.Document.AddInitialRevision(
      revision,
      "author@example.invalid",
      seededDocument.CreatedAtUtc.AddMinutes(12));

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      seedContext.AuditEvents.Add(new AuditEventRecord
      {
        Id = duplicateAuditId,
        SubjectType = nameof(KnowledgeDocument),
        SubjectId = "seed-knowledge-document",
        EventType = "SeedEvent",
        Actor = "seed@example.invalid",
        OccurredAtUtc = seededDocument.CreatedAtUtc,
        Description = "Seed audit event.",
      });
      await seedContext.SaveChangesAsync();
    }

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteKnowledgeDocumentRepository(dbContext).UpdateAsync(seededDocument.Document);
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revision);
      auditRecorder.Stage(new AuditEvent(
        duplicateAuditId,
        nameof(KnowledgeDocument),
        seededDocument.Document.Id.ToString(),
        "KnowledgeRevisionCreated",
        "author@example.invalid",
        seededDocument.CreatedAtUtc.AddMinutes(12),
        "Duplicate audit event for rollback test."));

      await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    var storedDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(seededDocument.Document.Id);

    Assert.NotNull(storedDocument);
    Assert.Empty(storedDocument!.Revisions);
    Assert.Single(storedDocument.AuditTrail);
    Assert.Equal(0L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_document_revisions"));
  }

  [Fact]
  public async Task DatabaseRejectsSecondCurrentAuthoritativeRevisionForSameDocument()
  {
    var seededDocument = await SeedKnowledgeDocumentAsync();
    var firstRevision = CreateRevision(
      seededDocument.Document.Id,
      "A",
      seededDocument.CreatedAtUtc.AddMinutes(11),
      seededDocument.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    seededDocument.Document.AddInitialRevision(
      firstRevision,
      "author@example.invalid",
      seededDocument.CreatedAtUtc.AddMinutes(12));

    await using (var seedContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(seedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteKnowledgeDocumentRepository(seedContext).UpdateAsync(seededDocument.Document);
      await new SqliteKnowledgeRevisionRepository(seedContext).AddAsync(firstRevision);
      StageAuditEvents(auditRecorder, seededDocument.Document.AuditTrail.Skip(seededDocument.InitialAuditCount));
      await unitOfWork.CommitAsync();
    }

    await using var duplicateContext = CreateDbContext();
    var duplicateRevision = new KnowledgeDocumentRevisionRecord
    {
      Id = KnowledgeDocumentRevisionId.New(),
      KnowledgeDocumentId = seededDocument.Document.Id,
      KnowledgeSourceId = KnowledgeSourceId.New(),
      RevisionLabel = "B",
      EffectiveDate = DateOnly.FromDateTime(seededDocument.CreatedAtUtc.UtcDateTime.AddDays(1)),
      ReceivedAtUtc = seededDocument.CreatedAtUtc.AddDays(1),
      SourceAuthority = KnowledgeSourceAuthorityLevel.EngineerIssued,
      VerificationStatus = KnowledgeVerificationStatus.Verified,
      ContentHash = "content-hash-b",
      MetadataHash = "metadata-hash-b",
      SupersedesRevisionId = null,
      SupersededByRevisionId = null,
      SourceSystemReference = "source-B",
      DescriptiveNotes = "Bypass-domain duplicate current revision.",
      CreatedAtUtc = seededDocument.CreatedAtUtc.AddDays(1),
      IngestionStatus = KnowledgeIngestionStatus.Processed,
      Lifecycle = KnowledgeRevisionLifecycle.CurrentAuthoritative,
    };

    duplicateContext.KnowledgeDocumentRevisions.Add(duplicateRevision);

    await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
  }

  [Fact]
  public async Task MigrationMetadataAndRepeatedMigrationIncludeKnowledgeSlice()
  {
    await using var dbContext = CreateDbContext();
    var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();

    Assert.Contains(
      migrationsAssembly.Migrations.Keys,
      migration => migration.EndsWith("KnowledgeEnginePersistenceRc2", StringComparison.Ordinal));
    Assert.Contains(
      migrationsAssembly.Migrations.Keys,
      migration => migration.EndsWith("KnowledgeEnginePersistenceSnapshotAlignment", StringComparison.Ordinal));

    await dbContext.Database.MigrateAsync();
    await dbContext.Database.MigrateAsync();

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();

    Assert.Equal(7, appliedMigrations.Length);
    Assert.Contains(appliedMigrations, migration => migration.EndsWith("KnowledgeEnginePersistenceRc2", StringComparison.Ordinal));
    Assert.Contains(appliedMigrations, migration => migration.EndsWith("KnowledgeEnginePersistenceSnapshotAlignment", StringComparison.Ordinal));
    Assert.Equal(7L, await QueryCountAsync(dbContext, "SELECT COUNT(*) FROM __EFMigrationsHistory"));
  }

  [Fact]
  public async Task PopulatedAiProposalExecutableDatabaseMigratesAndPreservesAuthoritativeState()
  {
    SeededAiProposalState seededState;

    await using (var initialContext = CreateDbContext())
    {
      var migrator = initialContext.GetService<IMigrator>();
      await migrator.MigrateAsync("20260716020704_AiDraftProposalSlice");
      seededState = await SeedAiProposalExecutableStateAsync(initialContext);
    }

    await using var migratedContext = CreateDbContext();
    await migratedContext.Database.MigrateAsync();
    await migratedContext.Database.MigrateAsync();

    var appliedMigrations = (await migratedContext.Database.GetAppliedMigrationsAsync()).ToArray();
    Assert.Equal(7, appliedMigrations.Length);

    var storedProject = await new SqliteProjectRepository(migratedContext).GetByIdAsync(seededState.ProjectId);
    var storedInspectionSession = await new SqliteInspectionSessionRepository(migratedContext).GetByIdAsync(seededState.InspectionSessionId);
    var storedReport = await new SqliteReportRepository(migratedContext).GetByIdAsync(seededState.ReportId);
    var storedProposal = await new SqliteAiProposalRepository(migratedContext).GetByIdAsync(seededState.ProposalId);
    var storedModelRun = await new SqliteModelRunRepository(migratedContext).GetByIdAsync(seededState.ModelRunId);
    var storedManifest = await new SqliteContextManifestRepository(migratedContext).GetByIdAsync(seededState.ContextManifestId);
    var auditCountBeforeKnowledgeInsert = await QueryCountAsync(migratedContext, "SELECT COUNT(*) FROM audit_events");

    Assert.NotNull(storedProject);
    Assert.NotNull(storedInspectionSession);
    Assert.NotNull(storedReport);
    Assert.NotNull(storedProposal);
    Assert.NotNull(storedModelRun);
    Assert.NotNull(storedManifest);
    Assert.Equal(ProjectLifecycle.Active, storedProject!.Lifecycle);
    Assert.Equal(InspectionSessionLifecycle.InProgress, storedInspectionSession!.Lifecycle);
    Assert.Equal(ReportLifecycle.Draft, storedReport!.Lifecycle);
    Assert.Equal(1, storedReport.RevisionNumber);
    Assert.Equal(ProposalStatus.ReadyForReview, storedProposal!.Status);
    Assert.Equal(ModelRunState.ReadyForHumanReview, storedModelRun!.State);
    Assert.Equal(seededState.ManifestHash, storedManifest!.ManifestHash);

    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededState.ProjectId,
      KnowledgeDocumentType.Report,
      "Field Knowledge Index",
      "KE-001",
      "Knowledge",
      "author@example.invalid",
      seededState.CreatedAtUtc.AddHours(2));
    var revision = CreateRevision(
      knowledgeDocument.Id,
      "R1",
      seededState.CreatedAtUtc.AddHours(2),
      seededState.CreatedAtUtc.AddHours(2).AddMinutes(1),
      supersedesRevisionId: null);
    knowledgeDocument.AddInitialRevision(
      revision,
      "author@example.invalid",
      seededState.CreatedAtUtc.AddHours(2).AddMinutes(2));

    var reportLifecycleBefore = storedReport.Lifecycle;
    var reportAuditCountBefore = storedReport.AuditTrail.Count;

    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(migratedContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    await new SqliteKnowledgeDocumentRepository(migratedContext).AddAsync(knowledgeDocument);
    await new SqliteKnowledgeRevisionRepository(migratedContext).AddAsync(revision);
    StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);
    await unitOfWork.CommitAsync();

    var reportAfterKnowledgeInsert = await new SqliteReportRepository(migratedContext).GetByIdAsync(seededState.ReportId);
    var proposalAfterKnowledgeInsert = await new SqliteAiProposalRepository(migratedContext).GetByIdAsync(seededState.ProposalId);

    Assert.NotNull(reportAfterKnowledgeInsert);
    Assert.NotNull(proposalAfterKnowledgeInsert);
    Assert.Equal(reportLifecycleBefore, reportAfterKnowledgeInsert!.Lifecycle);
    Assert.Equal(reportAuditCountBefore, reportAfterKnowledgeInsert.AuditTrail.Count);
    Assert.Equal(seededState.ProposalId, proposalAfterKnowledgeInsert!.Id);
    Assert.Equal(1L, await QueryCountAsync(migratedContext, "SELECT COUNT(*) FROM knowledge_documents"));
    Assert.Equal(auditCountBeforeKnowledgeInsert + 2, await QueryCountAsync(migratedContext, "SELECT COUNT(*) FROM audit_events"));
  }

  [Fact]
  public async Task NewDocumentWithCurrentRevisionCommitsAtomicallyViaDeferredReferenceHandler()
  {
    var seededProject = await SeedProjectAsync();
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededProject.ProjectId,
      KnowledgeDocumentType.Specification,
      "Deferred Reference Document",
      "DR-100",
      "Concrete",
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(10));

    var revision = CreateRevision(
      knowledgeDocument.Id,
      "A",
      seededProject.CreatedAtUtc.AddMinutes(11),
      seededProject.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    knowledgeDocument.AddInitialRevision(
      revision,
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(12));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDocument);
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revision);
      StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(knowledgeDocument.Id);
    var storedRevision = await new SqliteKnowledgeRevisionRepository(verificationContext).GetByIdAsync(revision.Id);

    Assert.NotNull(storedDocument);
    Assert.NotNull(storedRevision);
    Assert.Equal(revision.Id, storedDocument!.CurrentAuthoritativeRevisionId);
    Assert.Equal(knowledgeDocument.Id, storedRevision!.DocumentId);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_documents"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_document_revisions"));
  }

  [Fact]
  public async Task RevisionSupersessionCommitsCorrectlyViaDeferredReferenceHandler()
  {
    var seededProject = await SeedProjectAsync();
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededProject.ProjectId,
      KnowledgeDocumentType.Specification,
      "Supersession Test Document",
      "SUP-200",
      "Structural",
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(10));

    var revisionA = CreateRevision(
      knowledgeDocument.Id,
      "A",
      seededProject.CreatedAtUtc.AddMinutes(11),
      seededProject.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    knowledgeDocument.AddInitialRevision(
      revisionA,
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(12));

    var auditCountAfterInitialRevision = knowledgeDocument.AuditTrail.Count;

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDocument);
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revisionA);
      StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    var revisionB = CreateRevision(
      knowledgeDocument.Id,
      "B",
      seededProject.CreatedAtUtc.AddMinutes(20),
      seededProject.CreatedAtUtc.AddMinutes(21),
      supersedesRevisionId: revisionA.Id);

    knowledgeDocument.SupersedeCurrentRevision(
      revisionB,
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(22));

    await using (var updateContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(updateContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
      await new SqliteKnowledgeDocumentRepository(updateContext).UpdateAsync(knowledgeDocument);
      await new SqliteKnowledgeRevisionRepository(updateContext).UpdateAsync(revisionA);
      await new SqliteKnowledgeRevisionRepository(updateContext).AddAsync(revisionB);
      StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail.Skip(auditCountAfterInitialRevision));
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(knowledgeDocument.Id);

    Assert.NotNull(storedDocument);
    Assert.Equal(revisionB.Id, storedDocument!.CurrentAuthoritativeRevisionId);
    Assert.Equal(2L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_document_revisions"));
  }

  [Fact]
  public async Task DeferredReferenceHandlerRollbacksCleanlyOnSecondPassFailure()
  {
    var seededProject = await SeedProjectAsync();
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededProject.ProjectId,
      KnowledgeDocumentType.Specification,
      "Rollback Test Document",
      "RB-300",
      "Mechanical",
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(10));

    var revision = CreateRevision(
      knowledgeDocument.Id,
      "A",
      seededProject.CreatedAtUtc.AddMinutes(11),
      seededProject.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    knowledgeDocument.AddInitialRevision(
      revision,
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(12));

    var preSeededRevisionId = revision.Id;

    await using (var seedContext = CreateDbContext())
    {
      await seedContext.Database.MigrateAsync();
      seedContext.KnowledgeDocuments.Add(new KnowledgeDocumentRecord
      {
        Id = knowledgeDocument.Id,
        ProjectId = seededProject.ProjectId,
        DocumentType = KnowledgeDocumentType.Specification,
        CanonicalTitle = "Pre-seeded Document",
        Lifecycle = KnowledgeDocumentLifecycle.Active,
        CreatedBy = "seed@example.invalid",
        CreatedAtUtc = seededProject.CreatedAtUtc,
      });
      seedContext.KnowledgeDocumentRevisions.Add(new KnowledgeDocumentRevisionRecord
      {
        Id = preSeededRevisionId,
        KnowledgeDocumentId = knowledgeDocument.Id,
        KnowledgeSourceId = KnowledgeSourceId.New(),
        RevisionLabel = "PRESEEDED",
        EffectiveDate = DateOnly.FromDateTime(seededProject.CreatedAtUtc.UtcDateTime.AddDays(1)),
        ReceivedAtUtc = seededProject.CreatedAtUtc.AddDays(1),
        SourceAuthority = KnowledgeSourceAuthorityLevel.EngineerIssued,
        VerificationStatus = KnowledgeVerificationStatus.Verified,
        ContentHash = "content-hash-preseeded",
        MetadataHash = "metadata-hash-preseeded",
        SupersedesRevisionId = null,
        SupersededByRevisionId = null,
        SourceSystemReference = "source-preseeded",
        DescriptiveNotes = "Pre-seeded to cause PK collision on commit.",
        CreatedAtUtc = seededProject.CreatedAtUtc.AddDays(1),
        IngestionStatus = KnowledgeIngestionStatus.Processed,
        Lifecycle = KnowledgeRevisionLifecycle.Received,
      });
      await seedContext.SaveChangesAsync();
    }

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDocument);
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revision);
      StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);

      await Assert.ThrowsAnyAsync<Exception>(() => unitOfWork.CommitAsync());
    }

    await using var verificationContext = CreateDbContext();
    var survivingDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(knowledgeDocument.Id);
    Assert.NotNull(survivingDocument);
    Assert.Null(survivingDocument!.CurrentAuthoritativeRevisionId);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_documents"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_document_revisions"));
  }

  [Fact]
  public async Task UnrelatedAggregateCommitsDoNotInterfereWithDeferredReferences()
  {
    var seededProject = await SeedProjectAsync();
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededProject.ProjectId,
      KnowledgeDocumentType.Specification,
      "Unrelated Aggregate Document",
      "UA-400",
      "Civil",
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(10));

    var revision = CreateRevision(
      knowledgeDocument.Id,
      "A",
      seededProject.CreatedAtUtc.AddMinutes(11),
      seededProject.CreatedAtUtc.AddMinutes(12),
      supersedesRevisionId: null);

    knowledgeDocument.AddInitialRevision(
      revision,
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(12));

    var inspectionSession = new InspectionSession(
      InspectionSessionId.New(),
      seededProject.ProjectId,
      "Parallel Walkdown",
      "inspector@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(15));
    inspectionSession.Start("inspector@example.invalid", seededProject.CreatedAtUtc.AddMinutes(16));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });

      await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDocument);
      await new SqliteKnowledgeRevisionRepository(dbContext).AddAsync(revision);
      await new SqliteInspectionSessionRepository(dbContext).AddAsync(inspectionSession);
      StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);
      StageAuditEvents(auditRecorder, inspectionSession.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(knowledgeDocument.Id);
    var storedSession = await new SqliteInspectionSessionRepository(verificationContext).GetByIdAsync(inspectionSession.Id);

    Assert.NotNull(storedDocument);
    Assert.NotNull(storedSession);
    Assert.Equal(revision.Id, storedDocument!.CurrentAuthoritativeRevisionId);
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM knowledge_documents"));
    Assert.Equal(1L, await QueryCountAsync(verificationContext, "SELECT COUNT(*) FROM inspection_sessions"));
  }

  [Fact]
  public async Task EmptyDeferredReferenceHandlerCollectionCommitsNormally()
  {
    var seededProject = await SeedProjectAsync();
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededProject.ProjectId,
      KnowledgeDocumentType.Specification,
      "Empty Handler Document",
      "EH-500",
      "Electrical",
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(10));

    await using (var dbContext = CreateDbContext())
    {
      var auditRecorder = new SqliteAuditRecorder();
      var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, []);
      await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDocument);
      StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);
      await unitOfWork.CommitAsync();
    }

    await using var verificationContext = CreateDbContext();
    var storedDocument = await new SqliteKnowledgeDocumentRepository(verificationContext).GetByIdAsync(knowledgeDocument.Id);

    Assert.NotNull(storedDocument);
    Assert.Equal(knowledgeDocument.Id, storedDocument!.Id);
    Assert.Null(storedDocument.CurrentAuthoritativeRevisionId);
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

  private async Task<SeededProjectContext> SeedProjectAsync()
  {
    var createdAtUtc = new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero);

    await using var dbContext = CreateDbContext();
    await dbContext.Database.MigrateAsync();
    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    var projectRepository = new SqliteProjectRepository(dbContext);
    var project = new Project(ProjectId.New(), "Project Falcon", "owner@example.invalid", createdAtUtc);
    project.Activate("inspector@example.invalid", createdAtUtc.AddMinutes(1));

    await projectRepository.AddAsync(project);
    StageAuditEvents(auditRecorder, project.AuditTrail);
    await unitOfWork.CommitAsync();

    return new SeededProjectContext(project.Id, createdAtUtc);
  }

  private async Task<SeededKnowledgeDocumentContext> SeedKnowledgeDocumentAsync()
  {
    var seededProject = await SeedProjectAsync();
    var knowledgeDocument = new KnowledgeDocument(
      KnowledgeDocumentId.New(),
      seededProject.ProjectId,
      KnowledgeDocumentType.Specification,
      "Concrete Placement Notes",
      "KE-100",
      "Concrete",
      "author@example.invalid",
      seededProject.CreatedAtUtc.AddMinutes(5));

    await using var dbContext = CreateDbContext();
    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    await new SqliteKnowledgeDocumentRepository(dbContext).AddAsync(knowledgeDocument);
    StageAuditEvents(auditRecorder, knowledgeDocument.AuditTrail);
    await unitOfWork.CommitAsync();

    return new SeededKnowledgeDocumentContext(knowledgeDocument, knowledgeDocument.AuditTrail.Count, seededProject.CreatedAtUtc);
  }

  private async Task<SeededAiProposalState> SeedAiProposalExecutableStateAsync(SpinbusterDbContext dbContext)
  {
    var createdAtUtc = new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero);
    var auditRecorder = new SqliteAuditRecorder();
    var unitOfWork = new SqliteUnitOfWork(dbContext, auditRecorder, NullLogger<SqliteUnitOfWork>.Instance, new[] { new KnowledgeDocumentDeferredReferenceHandler() });
    var projectRepository = new SqliteProjectRepository(dbContext);
    var inspectionSessionRepository = new SqliteInspectionSessionRepository(dbContext);
    var reportRepository = new SqliteReportRepository(dbContext);
    var contextManifestRepository = new SqliteContextManifestRepository(dbContext);
    var modelRunRepository = new SqliteModelRunRepository(dbContext);
    var proposalRepository = new SqliteAiProposalRepository(dbContext);
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
      new ReportTitle("Existing Draft Report"),
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
          [])
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
      "operation-knowledge-upgrade",
      "request-fingerprint-knowledge-upgrade",
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

    await projectRepository.AddAsync(project);
    await inspectionSessionRepository.AddAsync(inspectionSession);
    await reportRepository.AddAsync(report, OperationId.New());
    await contextManifestRepository.AddAsync(contextManifest);
    await modelRunRepository.AddAsync(modelRun);
    await modelRunRepository.AddAttemptAsync(attempt);
    await proposalRepository.AddAsync(proposal);
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

    return new SeededAiProposalState(
      project.Id,
      inspectionSession.Id,
      report.Id,
      contextManifest.Id,
      contextManifest.ManifestHash,
      modelRun.Id,
      proposal.Id,
      createdAtUtc);
  }

  private static KnowledgeDocumentRevision CreateRevision(
    KnowledgeDocumentId knowledgeDocumentId,
    string revisionLabel,
    DateTimeOffset createdAtUtc,
    DateTimeOffset receivedAtUtc,
    KnowledgeDocumentRevisionId? supersedesRevisionId)
  {
    return new KnowledgeDocumentRevision(
      KnowledgeDocumentRevisionId.New(),
      knowledgeDocumentId,
      KnowledgeSourceId.New(),
      revisionLabel,
      DateOnly.FromDateTime(createdAtUtc.UtcDateTime),
      receivedAtUtc,
      KnowledgeSourceAuthorityLevel.EngineerIssued,
      $"content-hash-{revisionLabel.ToLowerInvariant()}",
      $"metadata-hash-{revisionLabel.ToLowerInvariant()}",
      supersedesRevisionId,
      $"source-{revisionLabel}",
      $"Revision {revisionLabel} notes.",
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

  private static string ComputeFileHash(string relativePath)
  {
    var repositoryRoot = FindRepositoryRoot();
    var absolutePath = Path.Combine(repositoryRoot, relativePath);
    var normalizedContents = File.ReadAllText(absolutePath).Replace("\r\n", "\n", StringComparison.Ordinal);
    var bytes = System.Text.Encoding.UTF8.GetBytes(normalizedContents);
    return Convert.ToHexString(SHA256.HashData(bytes));
  }

  private static string FindRepositoryRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "SPINbuster.sln")))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate the SPINbuster repository root from the current test execution directory.");
  }

  private sealed record SeededProjectContext(
    ProjectId ProjectId,
    DateTimeOffset CreatedAtUtc);

  private sealed record SeededKnowledgeDocumentContext(
    KnowledgeDocument Document,
    int InitialAuditCount,
    DateTimeOffset CreatedAtUtc);

  private sealed record SeededAiProposalState(
    ProjectId ProjectId,
    InspectionSessionId InspectionSessionId,
    ReportId ReportId,
    ContextManifestId ContextManifestId,
    string ManifestHash,
    ModelRunId ModelRunId,
    ProposalId ProposalId,
    DateTimeOffset CreatedAtUtc);
}
