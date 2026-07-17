using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.RecordDocumentCandidateReview;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class DocumentWorkflowSnapshotUseCaseTests
{
  [Fact]
  public async Task SnapshotQueryReturnsProjectScopedDocumentWorkflowWithoutSideEffects()
  {
    var projectId = ProjectId.New();
    var auditRecorder = new FakeAuditRecorder();
    var importSessionRepository = new FakeDocumentImportSessionRepository();
    var importedSourceRepository = new FakeImportedDocumentSourceRepository();
    var storageObjectRepository = new FakeStorageObjectRepository();
    var processingAttemptRepository = new FakeDocumentProcessingAttemptRepository();
    var candidateRepository = new FakeDocumentCandidateRepository();
    var knowledgeDocumentRepository = new FakeKnowledgeDocumentRepository();
    var knowledgeRelationshipRepository = new FakeKnowledgeRelationshipRepository();
    var reportRepository = new FakeReportRepository();
    var aiProposalRepository = new FakeAiProposalRepository();
    var snapshotUseCase = new LoadProjectDocumentWorkflowSnapshotUseCase(
      importSessionRepository,
      importedSourceRepository,
      processingAttemptRepository,
      candidateRepository,
      storageObjectRepository,
      new FakeAuditEventQueryRepository(auditRecorder),
      knowledgeDocumentRepository,
      knowledgeRelationshipRepository,
      reportRepository,
      aiProposalRepository,
      new FakeDocumentImportPolicy());

    var storageObject = new StorageObject(
      StorageObjectId.New(),
      "document-engine-foundation",
      "storage-1",
      12,
      "ABC",
      "SHA-256",
      1,
      new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero),
      null,
      StorageAvailabilityState.Available);
    var importSession = new DocumentImportSession(
      DocumentImportSessionId.New(),
      projectId,
      "importer@example.invalid",
      new DateTimeOffset(2026, 7, 17, 12, 1, 0, TimeSpan.Zero));
    importSession.BeginValidation("importer@example.invalid", new DateTimeOffset(2026, 7, 17, 12, 2, 0, TimeSpan.Zero));
    importSession.BeginImporting("importer@example.invalid", new DateTimeOffset(2026, 7, 17, 12, 3, 0, TimeSpan.Zero));
    var importedSource = new ImportedDocumentSource(
      ImportedSourceId.New(),
      importSession.Id,
      projectId,
      "source-a.txt",
      "text/plain",
      "text/plain",
      12,
      "ABC",
      "SHA-256",
      1,
      storageObject.ToReference(),
      ImportedSourceOrigin.LocalFile,
      "importer@example.invalid",
      new DateTimeOffset(2026, 7, 17, 12, 4, 0, TimeSpan.Zero),
      ImportedDocumentSourceStatus.Available,
      null);
    importSession.RecordAcceptedSource(importedSource.Id, "importer@example.invalid", new DateTimeOffset(2026, 7, 17, 12, 4, 0, TimeSpan.Zero));
    importSession.Complete("importer@example.invalid", new DateTimeOffset(2026, 7, 17, 12, 5, 0, TimeSpan.Zero));
    var processingAttempt = new DocumentProcessingAttempt(
      DocumentProcessingAttemptId.New(),
      importedSource.Id,
      projectId,
      "deterministic-fixture",
      "document-fixture",
      "1.0.0",
      new DateTimeOffset(2026, 7, 17, 12, 6, 0, TimeSpan.Zero),
      1,
      importedSource.ContentHash);
    processingAttempt.Start(new DateTimeOffset(2026, 7, 17, 12, 7, 0, TimeSpan.Zero));
    processingAttempt.MarkOutputReceived(new DateTimeOffset(2026, 7, 17, 12, 8, 0, TimeSpan.Zero), importedSource.ContentHash);
    processingAttempt.BeginValidation(new DateTimeOffset(2026, 7, 17, 12, 9, 0, TimeSpan.Zero));
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(),
      projectId,
      importedSource.Id,
      processingAttempt.Id,
      DocumentCandidateType.FragmentCandidate,
      "document-fragment-candidate",
      "1.0.0",
      """{"fragmentType":"TextFragment"}""",
      importedSource.ContentHash,
      "line:1",
      ConfidenceBand.Medium,
      [],
      new DateTimeOffset(2026, 7, 17, 12, 10, 0, TimeSpan.Zero));
    candidate.MarkValidated(new DateTimeOffset(2026, 7, 17, 12, 11, 0, TimeSpan.Zero));
    candidate.MarkReadyForReview(new DateTimeOffset(2026, 7, 17, 12, 12, 0, TimeSpan.Zero));
    processingAttempt.Complete(new DateTimeOffset(2026, 7, 17, 12, 13, 0, TimeSpan.Zero));

    await storageObjectRepository.AddAsync(storageObject);
    await importSessionRepository.AddAsync(importSession);
    await importedSourceRepository.AddAsync(importedSource);
    await processingAttemptRepository.AddAsync(processingAttempt);
    await candidateRepository.AddAsync(candidate);
    foreach (var auditEvent in importSession.AuditTrail.Concat(importedSource.AuditTrail).Concat(processingAttempt.AuditTrail).Concat(candidate.AuditTrail))
    {
      auditRecorder.Stage(auditEvent);
    }

    var result = await snapshotUseCase.HandleAsync(new LoadProjectDocumentWorkflowSnapshotQuery(projectId, 10, 10, 10, 10, 20));

    Assert.Equal(projectId, result.ProjectId);
    Assert.Single(result.ImportSessions);
    Assert.Single(result.ImportedSources);
    Assert.Equal(0, result.AuthorityIsolation.KnowledgeDocumentCount);
    Assert.Equal(0, result.AuthorityIsolation.ReportCount);
    Assert.Equal(0, result.AuthorityIsolation.AiProposalCount);
    Assert.DoesNotContain(result.ImportedSources.Single().Storage.ImmutableObjectKey, char.IsControl);
    Assert.Equal(0, auditRecorder.StagedEvents.Count(auditEvent => auditEvent.EventType == "UnexpectedSideEffect"));
  }

  [Fact]
  public async Task SnapshotQueryRejectsInvalidBounds()
  {
    var useCase = new LoadProjectDocumentWorkflowSnapshotUseCase(
      new FakeDocumentImportSessionRepository(),
      new FakeImportedDocumentSourceRepository(),
      new FakeDocumentProcessingAttemptRepository(),
      new FakeDocumentCandidateRepository(),
      new FakeStorageObjectRepository(),
      new FakeAuditEventQueryRepository(new FakeAuditRecorder()),
      new FakeKnowledgeDocumentRepository(),
      new FakeKnowledgeRelationshipRepository(),
      new FakeReportRepository(),
      new FakeAiProposalRepository(),
      new FakeDocumentImportPolicy());

    await Assert.ThrowsAsync<DomainInvariantException>(() => useCase.HandleAsync(
      new LoadProjectDocumentWorkflowSnapshotQuery(ProjectId.New(), 0, 10, 10, 10, 10)));
  }

  [Fact]
  public async Task RecordDocumentCandidateReviewRejectsInvalidTerminalTransition()
  {
    var candidateRepository = new FakeDocumentCandidateRepository();
    var candidate = new DocumentCandidate(
      DocumentCandidateId.New(),
      ProjectId.New(),
      ImportedSourceId.New(),
      DocumentProcessingAttemptId.New(),
      DocumentCandidateType.MetadataCandidate,
      "document-metadata-candidate",
      "1.0.0",
      """{"title":"candidate"}""",
      "hash",
      null,
      ConfidenceBand.High,
      [],
      new DateTimeOffset(2026, 7, 17, 13, 0, 0, TimeSpan.Zero));
    candidate.MarkValidated(new DateTimeOffset(2026, 7, 17, 13, 1, 0, TimeSpan.Zero));
    candidate.MarkReadyForReview(new DateTimeOffset(2026, 7, 17, 13, 2, 0, TimeSpan.Zero));
    candidate.Accept("reviewer@example.invalid", new DateTimeOffset(2026, 7, 17, 13, 3, 0, TimeSpan.Zero), "accepted");
    await candidateRepository.AddAsync(candidate);

    var useCase = new RecordDocumentCandidateReviewUseCase(
      candidateRepository,
      new FakeUnitOfWork(),
      new FakeClock(new DateTimeOffset(2026, 7, 17, 13, 4, 0, TimeSpan.Zero)),
      new FakeCurrentUser("reviewer@example.invalid"),
      new FakeAuditRecorder());

    await Assert.ThrowsAsync<LifecycleTransitionException>(() => useCase.HandleAsync(
      new RecordDocumentCandidateReviewCommand(candidate.Id, DocumentCandidateReviewDisposition.Rejected, "second review")));
  }
}
