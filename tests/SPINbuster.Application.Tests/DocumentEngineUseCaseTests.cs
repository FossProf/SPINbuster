using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadDocumentCandidates;
using SPINbuster.Application.UseCases.RecordDocumentCandidateReview;
using SPINbuster.Application.UseCases.RequestDocumentProcessing;
using SPINbuster.Domain;
using System.Text;

namespace SPINbuster.Application.Tests;

public sealed class DocumentEngineUseCaseTests
{
  [Fact]
  public async Task BeginDocumentImportSessionStagesAuditBeforeCommit()
  {
    var operationLog = new List<string>();
    var fixture = CreateFixture(operationLog);
    var useCase = new BeginDocumentImportSessionUseCase(
      fixture.ImportSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new BeginDocumentImportSessionCommand(fixture.ProjectId));

    Assert.NotEqual(default, result.ImportSessionId);
    Assert.Equal(["audit-stage", "commit"], operationLog);
  }

  [Fact]
  public async Task ImportDocumentSourceStoresNewImmutableSourceAndKeepsSessionActive()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);

    var result = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));

    Assert.False(result.ReusedExistingProjectSource);
    Assert.Single(fixture.ImportedSourceRepository.AddedSources);
    Assert.Single(fixture.StorageObjectRepository.AddedStorageObjects);
    Assert.Equal(DocumentImportSessionState.Importing, fixture.ImportSessionRepository.UpdatedSessions.Last().State);
  }

  [Fact]
  public async Task ImportDocumentSourceReusesExactDuplicateWithinProject()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var useCase = CreateImportUseCase(fixture);
    var content = Encoding.UTF8.GetBytes("hello world");
    await useCase.HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "a.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(content)));

    var secondSession = await StartSessionAsync(fixture);
    var duplicateResult = await useCase.HandleAsync(new ImportDocumentSourceCommand(
      secondSession,
      fixture.ProjectId,
      "renamed.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      new MemoryStream(content)));

    Assert.True(duplicateResult.ReusedExistingProjectSource);
    Assert.Single(fixture.ImportedSourceRepository.AddedSources);
    Assert.Single(fixture.StorageObjectRepository.AddedStorageObjects);
    Assert.Equal(DocumentImportSessionState.Importing, fixture.ImportSessionRepository.UpdatedSessions.Last().State);
  }

  [Fact]
  public async Task CompleteDocumentImportSessionClosesBatchExplicitly()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    await CreateImportUseCase(fixture).HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));

    var result = await new CompleteDocumentImportSessionUseCase(
      fixture.ImportSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new CompleteDocumentImportSessionCommand(importSession));

    Assert.Equal(DocumentImportSessionState.Completed, result.State);
    Assert.NotNull(result.CompletedAtUtc);
    Assert.Equal(DocumentImportSessionState.Completed, fixture.ImportSessionRepository.UpdatedSessions.Last().State);
  }

  [Fact]
  public async Task RequestDocumentProcessingCommitsAttemptBeforeProcessorExecution()
  {
    var operationLog = new List<string>();
    var fixture = CreateFixture(operationLog);
    var importSession = await StartSessionAsync(fixture);
    var importResult = await CreateImportUseCase(fixture).HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));
    var commitsBeforeProcessing = fixture.UnitOfWork.CommitCount;
    fixture.DocumentProcessor.SequenceLog.Clear();

    var useCase = new RequestDocumentProcessingUseCase(
      fixture.ImportedSourceRepository,
      fixture.ProcessingAttemptRepository,
      fixture.CandidateRepository,
      fixture.ImmutableContentStore,
      fixture.DocumentProcessor,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new RequestDocumentProcessingCommand(importResult.ImportedSourceId, fixture.ProjectId));

    Assert.Equal(DocumentProcessingAttemptState.Completed, result.State);
    Assert.Single(fixture.CandidateRepository.AddedCandidates);
    Assert.Contains("commit", operationLog);
    Assert.Equal("processor-run", fixture.DocumentProcessor.SequenceLog.Single());
    Assert.True(operationLog.IndexOf("commit") < operationLog.IndexOf("processor-run"));
  }

  [Fact]
  public async Task RequestDocumentProcessingPersistsFailureWithoutCandidates()
  {
    var fixture = CreateFixture();
    fixture.DocumentProcessor.ProcessAsyncCore = (_, _) => Task.FromResult(new DocumentProcessorExecutionResult(
      false,
      null,
      DocumentProcessingFailureClassification.ProviderUnavailable,
      "Provider unavailable.",
      []));
    var importSession = await StartSessionAsync(fixture);
    var importResult = await CreateImportUseCase(fixture).HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));

    var useCase = new RequestDocumentProcessingUseCase(
      fixture.ImportedSourceRepository,
      fixture.ProcessingAttemptRepository,
      fixture.CandidateRepository,
      fixture.ImmutableContentStore,
      fixture.DocumentProcessor,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new RequestDocumentProcessingCommand(importResult.ImportedSourceId, fixture.ProjectId));

    Assert.Equal(DocumentProcessingAttemptState.Failed, result.State);
    Assert.Empty(fixture.CandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task RequestDocumentProcessingPersistsFailedAttemptWhenContentOpenThrows()
  {
    var fixture = CreateFixture();
    fixture.ImmutableContentStore.ThrowOnOpenRead = true;
    var importSession = await StartSessionAsync(fixture);
    var importResult = await CreateImportUseCase(fixture).HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));
    var commitsBeforeProcessing = fixture.UnitOfWork.CommitCount;

    var result = await new RequestDocumentProcessingUseCase(
      fixture.ImportedSourceRepository,
      fixture.ProcessingAttemptRepository,
      fixture.CandidateRepository,
      fixture.ImmutableContentStore,
      fixture.DocumentProcessor,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.AuditRecorder).HandleAsync(new RequestDocumentProcessingCommand(importResult.ImportedSourceId, fixture.ProjectId));

    Assert.Equal(DocumentProcessingAttemptState.Failed, result.State);
    Assert.Equal(DocumentProcessingFailureClassification.Unknown, result.FailureClassification);
    Assert.Equal(commitsBeforeProcessing + 2, fixture.UnitOfWork.CommitCount);
    Assert.Empty(fixture.CandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task RequestDocumentProcessingPersistsFailedAttemptWhenProcessorThrows()
  {
    var fixture = CreateFixture();
    fixture.DocumentProcessor.ProcessAsyncCore = (_, _) => throw new InvalidOperationException("Processor exploded.");
    var importSession = await StartSessionAsync(fixture);
    var importResult = await CreateImportUseCase(fixture).HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));

    var result = await new RequestDocumentProcessingUseCase(
      fixture.ImportedSourceRepository,
      fixture.ProcessingAttemptRepository,
      fixture.CandidateRepository,
      fixture.ImmutableContentStore,
      fixture.DocumentProcessor,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.AuditRecorder).HandleAsync(new RequestDocumentProcessingCommand(importResult.ImportedSourceId, fixture.ProjectId));

    Assert.Equal(DocumentProcessingAttemptState.Failed, result.State);
    Assert.Equal(DocumentProcessingFailureClassification.Unknown, result.FailureClassification);
    Assert.Equal(DocumentProcessingAttemptState.Failed, fixture.ProcessingAttemptRepository.UpdatedAttempts.Last().State);
    Assert.Empty(fixture.CandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task RequestDocumentProcessingPersistsFailedAttemptWhenCandidatePersistenceThrows()
  {
    var fixture = CreateFixture();
    fixture.CandidateRepository.ThrowOnAdd = true;
    var importSession = await StartSessionAsync(fixture);
    var importResult = await CreateImportUseCase(fixture).HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));

    var result = await new RequestDocumentProcessingUseCase(
      fixture.ImportedSourceRepository,
      fixture.ProcessingAttemptRepository,
      fixture.CandidateRepository,
      fixture.ImmutableContentStore,
      fixture.DocumentProcessor,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.AuditRecorder).HandleAsync(new RequestDocumentProcessingCommand(importResult.ImportedSourceId, fixture.ProjectId));

    Assert.Equal(DocumentProcessingAttemptState.Failed, result.State);
    Assert.Equal(DocumentProcessingFailureClassification.Unknown, result.FailureClassification);
    Assert.Empty(fixture.CandidateRepository.AddedCandidates);
  }

  [Fact]
  public async Task RecordDocumentCandidateReviewMarksHumanAcceptedWithoutAuthoritativePromotion()
  {
    var fixture = CreateFixture();
    var importSession = await StartSessionAsync(fixture);
    var importResult = await CreateImportUseCase(fixture).HandleAsync(new ImportDocumentSourceCommand(
      importSession,
      fixture.ProjectId,
      "detail.pdf",
      "application/pdf",
      ImportedSourceOrigin.LocalFile,
      null,
      CreateContent("hello world")));
    await new RequestDocumentProcessingUseCase(
      fixture.ImportedSourceRepository,
      fixture.ProcessingAttemptRepository,
      fixture.CandidateRepository,
      fixture.ImmutableContentStore,
      fixture.DocumentProcessor,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.AuditRecorder).HandleAsync(new RequestDocumentProcessingCommand(importResult.ImportedSourceId, fixture.ProjectId));

    var candidate = fixture.CandidateRepository.AddedCandidates.Single();
    var useCase = new RecordDocumentCandidateReviewUseCase(
      fixture.CandidateRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new RecordDocumentCandidateReviewCommand(
      candidate.Id,
      DocumentCandidateReviewDisposition.HumanAccepted,
      "Looks reasonable."));

    Assert.Equal(DocumentCandidateStatus.HumanAccepted, result.Status);
    var loaded = await new LoadDocumentCandidatesUseCase(fixture.CandidateRepository, fixture.ImportPolicy)
      .HandleAsync(new LoadDocumentCandidatesQuery(importResult.ImportedSourceId, null, 10));
    Assert.Equal(DocumentCandidateStatus.HumanAccepted, loaded.Candidates.Single().Status);
  }

  private static MemoryStream CreateContent(string text) => new(Encoding.UTF8.GetBytes(text));

  private static ImportDocumentSourceUseCase CreateImportUseCase(DocumentFixture fixture)
  {
    return new ImportDocumentSourceUseCase(
      fixture.ImportSessionRepository,
      fixture.ImportedSourceRepository,
      fixture.StorageObjectRepository,
      fixture.ImmutableContentStore,
      fixture.ContentHashService,
      fixture.ContentInspector,
      fixture.ImportPolicy,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
  }

  private static async Task<DocumentImportSessionId> StartSessionAsync(DocumentFixture fixture)
  {
    var result = await new BeginDocumentImportSessionUseCase(
      fixture.ImportSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder).HandleAsync(new BeginDocumentImportSessionCommand(fixture.ProjectId));
    return result.ImportSessionId;
  }

  private static DocumentFixture CreateFixture(List<string>? operationLog = null)
  {
    return new DocumentFixture(
      ProjectId.New(),
      new FakeDocumentImportSessionRepository(),
      new FakeImportedDocumentSourceRepository(),
      new FakeStorageObjectRepository(),
      new FakeDocumentProcessingAttemptRepository(),
      new FakeDocumentCandidateRepository(),
      new FakeImmutableContentStore(),
      new FakeContentHashService(),
      new FakeImportedContentInspector(),
      new FakeDocumentImportPolicy(),
      new FakeDocumentProcessor(operationLog),
      new FakeUnitOfWork(operationLog),
      new FakeClock(new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero)),
      new FakeCurrentUser("document.reviewer@example.invalid"),
      new FakeAuditRecorder(operationLog));
  }

  private sealed record DocumentFixture(
    ProjectId ProjectId,
    FakeDocumentImportSessionRepository ImportSessionRepository,
    FakeImportedDocumentSourceRepository ImportedSourceRepository,
    FakeStorageObjectRepository StorageObjectRepository,
    FakeDocumentProcessingAttemptRepository ProcessingAttemptRepository,
    FakeDocumentCandidateRepository CandidateRepository,
    FakeImmutableContentStore ImmutableContentStore,
    FakeContentHashService ContentHashService,
    FakeImportedContentInspector ContentInspector,
    FakeDocumentImportPolicy ImportPolicy,
    FakeDocumentProcessor DocumentProcessor,
    FakeUnitOfWork UnitOfWork,
    FakeClock Clock,
    FakeCurrentUser CurrentUser,
    FakeAuditRecorder AuditRecorder);
}
