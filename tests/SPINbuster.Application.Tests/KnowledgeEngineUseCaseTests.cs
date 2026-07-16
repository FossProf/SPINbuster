using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.AddKnowledgeCitation;
using SPINbuster.Application.UseCases.AddKnowledgeDocumentRevision;
using SPINbuster.Application.UseCases.CreateKnowledgeRelationship;
using SPINbuster.Application.UseCases.LoadKnowledgeDocument;
using SPINbuster.Application.UseCases.LoadKnowledgeNeighborhood;
using SPINbuster.Application.UseCases.LoadKnowledgeRevisionHistory;
using SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;
using SPINbuster.Application.UseCases.RegisterKnowledgeDocument;
using SPINbuster.Application.UseCases.SupersedeKnowledgeRevision;
using SPINbuster.Application.UseCases.VerifyKnowledgeRevision;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class KnowledgeEngineUseCaseTests
{
  [Fact]
  public async Task RegisterKnowledgeDocumentStagesAuditBeforeCommit()
  {
    var operationLog = new List<string>();
    var fixture = CreateFixture(operationLog);
    var useCase = CreateRegisterUseCase(fixture);

    var result = await useCase.HandleAsync(new RegisterKnowledgeDocumentCommand(
      fixture.ProjectId,
      KnowledgeDocumentType.Drawing,
      "Clarifier Plan Set",
      "DWG-100",
      "Structural"));

    Assert.NotEqual(default, result.KnowledgeDocumentId);
    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
    Assert.Equal(["audit-stage", "commit"], operationLog);
  }

  [Fact]
  public async Task AddKnowledgeDocumentRevisionPersistsExplicitRepositoryUpdates()
  {
    var fixture = CreateFixture();
    var registerResult = await CreateRegisterUseCase(fixture).HandleAsync(new RegisterKnowledgeDocumentCommand(
      fixture.ProjectId,
      KnowledgeDocumentType.Specification,
      "Concrete Spec",
      "03 30 00",
      "Civil"));
    var useCase = new AddKnowledgeDocumentRevisionUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new AddKnowledgeDocumentRevisionCommand(
      registerResult.KnowledgeDocumentId,
      KnowledgeSourceId.New(),
      "A",
      new DateOnly(2026, 7, 16),
      fixture.Clock.UtcNow,
      KnowledgeSourceAuthorityLevel.EngineerIssued,
      "content-hash-a",
      "metadata-hash-a",
      "spec-system",
      "Initial issue.",
      KnowledgeIngestionStatus.MetadataCaptured));

    Assert.Equal(result.KnowledgeDocumentRevisionId, fixture.KnowledgeRevisionRepository.AddedRevisions.Single().Id);
    Assert.Contains(fixture.KnowledgeDocumentRepository.UpdatedDocuments, document => document.Id == registerResult.KnowledgeDocumentId);
    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task SupersedeKnowledgeRevisionUpdatesPriorRevisionAndStagesAuditBeforeCommit()
  {
    var operationLog = new List<string>();
    var fixture = CreateFixture(operationLog);
    var documentId = await RegisterDocumentWithInitialRevisionAsync(fixture);
    var currentRevision = fixture.KnowledgeRevisionRepository.AddedRevisions.Single();
    var useCase = new SupersedeKnowledgeRevisionUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new SupersedeKnowledgeRevisionCommand(
      documentId,
      currentRevision.Id,
      KnowledgeSourceId.New(),
      "B",
      new DateOnly(2026, 7, 16),
      fixture.Clock.UtcNow,
      KnowledgeSourceAuthorityLevel.EngineerIssued,
      "content-hash-b",
      "metadata-hash-b",
      "spec-system",
      "Superseding issue.",
      KnowledgeIngestionStatus.MetadataCaptured));

    Assert.Equal(currentRevision.Id, result.SupersededRevisionId);
    Assert.Contains(fixture.KnowledgeRevisionRepository.UpdatedRevisions, revision => revision.Id == currentRevision.Id);
    Assert.Contains(fixture.AuditRecorder.StagedEvents, auditEvent => auditEvent.EventType == "KnowledgeRevisionSuperseded");
    Assert.Equal("audit-stage", operationLog[^2]);
    Assert.Equal("commit", operationLog[^1]);
  }

  [Fact]
  public async Task VerifyKnowledgeRevisionStagesAuditAndCommit()
  {
    var fixture = CreateFixture();
    var documentId = await RegisterDocumentWithInitialRevisionAsync(fixture);
    var revisionId = fixture.KnowledgeRevisionRepository.AddedRevisions.Single().Id;
    var useCase = new VerifyKnowledgeRevisionUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new VerifyKnowledgeRevisionCommand(
      documentId,
      revisionId,
      KnowledgeVerificationStatus.Verified));

    Assert.Equal(KnowledgeVerificationStatus.Verified, result.VerificationStatus);
    Assert.Contains(fixture.AuditRecorder.StagedEvents, auditEvent => auditEvent.EventType == "KnowledgeRevisionVerificationChanged");
  }

  [Fact]
  public async Task AddKnowledgeCitationStagesAuditBeforeCommit()
  {
    var operationLog = new List<string>();
    var fixture = CreateFixture(operationLog);
    await RegisterDocumentWithInitialRevisionAsync(fixture);
    var revisionId = fixture.KnowledgeRevisionRepository.AddedRevisions.Single().Id;
    var useCase = new AddKnowledgeCitationUseCase(
      fixture.KnowledgeCitationRepository,
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new AddKnowledgeCitationCommand(
      fixture.ProjectId,
      revisionId,
      KnowledgeCitationLocationType.Section,
      "Section 3.6.B",
      "Curing requirements."));

    Assert.Equal(revisionId, result.KnowledgeDocumentRevisionId);
    Assert.Equal(result.KnowledgeCitationId, fixture.KnowledgeCitationRepository.AddedCitations.Single().Id);
    Assert.Equal(["audit-stage", "commit"], operationLog[^2..]);
    Assert.Contains(fixture.AuditRecorder.StagedEvents, auditEvent => auditEvent.EventType == "KnowledgeCitationAdded");
  }

  [Fact]
  public async Task AddKnowledgeCitationRejectsDuplicateLocator()
  {
    var fixture = CreateFixture();
    await RegisterDocumentWithInitialRevisionAsync(fixture);
    var revisionId = fixture.KnowledgeRevisionRepository.AddedRevisions.Single().Id;
    var useCase = new AddKnowledgeCitationUseCase(
      fixture.KnowledgeCitationRepository,
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    await useCase.HandleAsync(new AddKnowledgeCitationCommand(
      fixture.ProjectId,
      revisionId,
      KnowledgeCitationLocationType.Section,
      "Section 3.6.B",
      "Original citation."));

    var exception = await Assert.ThrowsAsync<DomainInvariantException>(() => useCase.HandleAsync(new AddKnowledgeCitationCommand(
      fixture.ProjectId,
      revisionId,
      KnowledgeCitationLocationType.Section,
      "Section 3.6.B",
      "Duplicate citation.")));

    Assert.Contains("Duplicate knowledge citation", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public async Task CreateKnowledgeRelationshipRejectsCrossProjectSubject()
  {
    var fixture = CreateFixture();
    var documentId = await RegisterDocumentWithInitialRevisionAsync(fixture);
    var useCase = new CreateKnowledgeRelationshipUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeRelationshipRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var exception = await Assert.ThrowsAsync<DomainInvariantException>(() => useCase.HandleAsync(
      new CreateKnowledgeRelationshipCommand(
        fixture.ProjectId,
        KnowledgeSubjectReference.ForDocument(fixture.ProjectId, documentId),
        KnowledgeSubjectReference.ForDocument(ProjectId.New(), KnowledgeDocumentId.New()),
        KnowledgeRelationshipType.References,
        "Cross-project links are not allowed.")));

    Assert.Contains("project", exception.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task CreateKnowledgeRelationshipRecordsContradictionWithoutCancelingState()
  {
    var fixture = CreateFixture();
    var leftDocumentId = await RegisterDocumentWithInitialRevisionAsync(fixture, "Clarifier Plan Set", "DWG-100");
    var rightDocumentId = await RegisterDocumentWithInitialRevisionAsync(fixture, "RFI 27", "RFI-27");
    var useCase = new CreateKnowledgeRelationshipUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeRelationshipRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new CreateKnowledgeRelationshipCommand(
      fixture.ProjectId,
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, leftDocumentId),
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, rightDocumentId),
      KnowledgeRelationshipType.Contradicts,
      "The RFI contradicts the prior plan note."));

    Assert.NotEqual(default, result.KnowledgeRelationshipId);
    Assert.True(result.ContradictionDetected);
    Assert.Contains(fixture.AuditRecorder.StagedEvents, auditEvent => auditEvent.EventType == "KnowledgeContradictionDetected");
  }

  [Fact]
  public async Task LoadKnowledgeDocumentDoesNotCommitOrStageAudit()
  {
    var fixture = CreateFixture();
    var documentId = await RegisterDocumentWithInitialRevisionAsync(fixture);
    var useCase = new LoadKnowledgeDocumentUseCase(fixture.KnowledgeDocumentRepository);
    var stagedAuditCount = fixture.AuditRecorder.StagedEvents.Count;
    var initialCommitCount = fixture.UnitOfWork.CommitCount;

    var result = await useCase.HandleAsync(new LoadKnowledgeDocumentQuery(documentId));

    Assert.Equal(documentId, result.KnowledgeDocumentId);
    Assert.Equal(stagedAuditCount, fixture.AuditRecorder.StagedEvents.Count);
    Assert.Equal(initialCommitCount, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task LoadKnowledgeRevisionHistoryReturnsHistoryWithoutCommit()
  {
    var fixture = CreateFixture();
    var documentId = await RegisterDocumentWithInitialRevisionAsync(fixture);
    var currentRevision = fixture.KnowledgeRevisionRepository.AddedRevisions.Single();
    await new SupersedeKnowledgeRevisionUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new SupersedeKnowledgeRevisionCommand(
        documentId,
        currentRevision.Id,
        KnowledgeSourceId.New(),
        "B",
        new DateOnly(2026, 7, 16),
        fixture.Clock.UtcNow,
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "content-hash-b",
        "metadata-hash-b",
        "spec-system",
        "Superseding issue.",
        KnowledgeIngestionStatus.MetadataCaptured));
    var loadUseCase = new LoadKnowledgeRevisionHistoryUseCase(fixture.KnowledgeRevisionRepository);
    var initialCommitCount = fixture.UnitOfWork.CommitCount;

    var result = await loadUseCase.HandleAsync(new LoadKnowledgeRevisionHistoryQuery(documentId));

    Assert.Equal(2, result.Revisions.Count);
    Assert.Equal(initialCommitCount, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task LoadKnowledgeNeighborhoodReturnsBoundedRelationshipGraph()
  {
    var fixture = CreateFixture();
    var firstDocumentId = await RegisterDocumentWithInitialRevisionAsync(fixture, "Drawing A", "DWG-A");
    var secondDocumentId = await RegisterDocumentWithInitialRevisionAsync(fixture, "Drawing B", "DWG-B");
    var thirdDocumentId = await RegisterDocumentWithInitialRevisionAsync(fixture, "Drawing C", "DWG-C");
    var createUseCase = new CreateKnowledgeRelationshipUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeRelationshipRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    await createUseCase.HandleAsync(new CreateKnowledgeRelationshipCommand(
      fixture.ProjectId,
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, firstDocumentId),
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, secondDocumentId),
      KnowledgeRelationshipType.References,
      "A references B."));
    await createUseCase.HandleAsync(new CreateKnowledgeRelationshipCommand(
      fixture.ProjectId,
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, secondDocumentId),
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, thirdDocumentId),
      KnowledgeRelationshipType.References,
      "B references C."));
    var loadUseCase = new LoadKnowledgeNeighborhoodUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeRelationshipRepository);

    var result = await loadUseCase.HandleAsync(new LoadKnowledgeNeighborhoodQuery(
      fixture.ProjectId,
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, firstDocumentId),
      MaxDepth: 1,
      MaxRelationships: 1));

    Assert.Single(result.Relationships);
    Assert.Equal(2, result.Nodes.Count);
  }

  [Fact]
  public async Task LoadKnowledgeNeighborhoodRejectsUnreasonableRelationshipLimit()
  {
    var fixture = CreateFixture();
    var documentId = await RegisterDocumentWithInitialRevisionAsync(fixture, "Drawing A", "DWG-A");
    var loadUseCase = new LoadKnowledgeNeighborhoodUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeRelationshipRepository);

    var exception = await Assert.ThrowsAsync<DomainInvariantException>(() => loadUseCase.HandleAsync(new LoadKnowledgeNeighborhoodQuery(
      fixture.ProjectId,
      KnowledgeSubjectReference.ForDocument(fixture.ProjectId, documentId),
      MaxDepth: 1,
      MaxRelationships: LoadKnowledgeNeighborhoodUseCase.MaxRelationshipLimit + 1)));

    Assert.Contains("must not exceed", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public async Task LoadProjectKnowledgeSnapshotReturnsDurablePresentationModelWithoutCommit()
  {
    var fixture = CreateFixture();
    var specificationId = await RegisterDocumentWithInitialRevisionAsync(fixture, "Concrete Spec", "03 30 00");
    var initialRevision = fixture.KnowledgeRevisionRepository.AddedRevisions.Single();
    var superseded = await new SupersedeKnowledgeRevisionUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new SupersedeKnowledgeRevisionCommand(
        specificationId,
        initialRevision.Id,
        KnowledgeSourceId.New(),
        "B",
        new DateOnly(2026, 7, 16),
        fixture.Clock.UtcNow,
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "content-hash-b",
        "metadata-hash-b",
        "spec-system",
        "Superseding issue.",
        KnowledgeIngestionStatus.MetadataCaptured));
    var rfiDocumentId = await RegisterDocumentWithInitialRevisionAsync(fixture, "RFI 27", "RFI-27");
    var rfiRevision = fixture.KnowledgeRevisionRepository.AddedRevisions.Last();

    await new AddKnowledgeCitationUseCase(
      fixture.KnowledgeCitationRepository,
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new AddKnowledgeCitationCommand(
        fixture.ProjectId,
        superseded.SuccessorRevisionId,
        KnowledgeCitationLocationType.Section,
        "Section 3.6.B",
        "Revised curing requirements."));
    await new CreateKnowledgeRelationshipUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.KnowledgeRelationshipRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new CreateKnowledgeRelationshipCommand(
        fixture.ProjectId,
        KnowledgeSubjectReference.ForRevision(fixture.ProjectId, rfiRevision.Id),
        KnowledgeSubjectReference.ForRevision(fixture.ProjectId, superseded.SuccessorRevisionId),
        KnowledgeRelationshipType.Clarifies,
        "RFI clarifies the revised curing requirement."));

    var useCase = new LoadProjectKnowledgeSnapshotUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeCitationRepository,
      fixture.KnowledgeRelationshipRepository,
      new FakeAuditEventQueryRepository(fixture.AuditRecorder));
    var initialCommitCount = fixture.UnitOfWork.CommitCount;
    var stagedAuditCount = fixture.AuditRecorder.StagedEvents.Count;

    var result = await useCase.HandleAsync(new LoadProjectKnowledgeSnapshotQuery(fixture.ProjectId));

    Assert.Equal(fixture.ProjectId, result.ProjectId);
    Assert.Equal(2, result.Documents.Count);
    Assert.Contains(result.Documents, document => document.KnowledgeDocumentId == specificationId);
    Assert.Contains(result.Documents, document => document.KnowledgeDocumentId == rfiDocumentId);
    Assert.Single(result.Relationships);
    Assert.Contains(result.Documents.SelectMany(document => document.Revisions), revision => revision.Citations.Count == 1);
    Assert.Equal(initialCommitCount, fixture.UnitOfWork.CommitCount);
    Assert.Equal(stagedAuditCount, fixture.AuditRecorder.StagedEvents.Count);
  }

  [Fact]
  public async Task CommitFailureIsNotReportedAsSuccessForKnowledgeRegistration()
  {
    var fixture = CreateFixture();
    fixture.UnitOfWork.ThrowOnCommit = true;
    var useCase = CreateRegisterUseCase(fixture);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.HandleAsync(new RegisterKnowledgeDocumentCommand(
      fixture.ProjectId,
      KnowledgeDocumentType.Drawing,
      "Clarifier Plan Set",
      "DWG-100",
      "Structural")));

    Assert.Equal("Commit failed.", exception.Message);
  }

  [Fact]
  public async Task CancellationTokenFlowsToKnowledgeQueries()
  {
    var fixture = CreateFixture();
    var documentId = await RegisterDocumentWithInitialRevisionAsync(fixture);
    using var cancellationSource = new CancellationTokenSource();
    cancellationSource.Cancel();
    var useCase = new LoadKnowledgeDocumentUseCase(fixture.KnowledgeDocumentRepository);

    await useCase.HandleAsync(new LoadKnowledgeDocumentQuery(documentId), cancellationSource.Token);

    Assert.True(fixture.KnowledgeDocumentRepository.LastCancellationToken.IsCancellationRequested);
  }

  private static RegisterKnowledgeDocumentUseCase CreateRegisterUseCase(KnowledgeFixture fixture)
  {
    return new RegisterKnowledgeDocumentUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
  }

  private static async Task<KnowledgeDocumentId> RegisterDocumentWithInitialRevisionAsync(
    KnowledgeFixture fixture,
    string title = "Clarifier Plan Set",
    string externalReference = "DWG-100")
  {
    var document = await CreateRegisterUseCase(fixture).HandleAsync(new RegisterKnowledgeDocumentCommand(
      fixture.ProjectId,
      KnowledgeDocumentType.Drawing,
      title,
      externalReference,
      "Structural"));
    await new AddKnowledgeDocumentRevisionUseCase(
      fixture.KnowledgeDocumentRepository,
      fixture.KnowledgeRevisionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new AddKnowledgeDocumentRevisionCommand(
        document.KnowledgeDocumentId,
        KnowledgeSourceId.New(),
        "A",
        new DateOnly(2026, 7, 16),
        fixture.Clock.UtcNow,
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        $"content-hash-{title}",
        $"metadata-hash-{title}",
        "source-system",
        "Initial issue.",
        KnowledgeIngestionStatus.MetadataCaptured));
    return document.KnowledgeDocumentId;
  }

  private static KnowledgeFixture CreateFixture(List<string>? operationLog = null)
  {
    return new KnowledgeFixture(
      ProjectId.New(),
      new FakeKnowledgeDocumentRepository(),
      new FakeKnowledgeRevisionRepository(),
      new FakeKnowledgeRelationshipRepository(),
      new FakeKnowledgeCitationRepository(),
      new FakeUnitOfWork(operationLog),
      new FakeClock(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero)),
      new FakeCurrentUser("knowledge.reviewer@example.invalid"),
      new FakeAuditRecorder(operationLog));
  }

  private sealed record KnowledgeFixture(
    ProjectId ProjectId,
    FakeKnowledgeDocumentRepository KnowledgeDocumentRepository,
    FakeKnowledgeRevisionRepository KnowledgeRevisionRepository,
    FakeKnowledgeRelationshipRepository KnowledgeRelationshipRepository,
    FakeKnowledgeCitationRepository KnowledgeCitationRepository,
    FakeUnitOfWork UnitOfWork,
    FakeClock Clock,
    FakeCurrentUser CurrentUser,
    FakeAuditRecorder AuditRecorder);
}
