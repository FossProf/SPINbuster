using SPINbuster.Application;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.CreateReportDraft;
using SPINbuster.Application.UseCases.LoadReportDraftSnapshot;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class ReportDraftUseCaseTests
{
  [Fact]
  public async Task CreateReportDraftCreatesAuthoritativeDraftAndStagesAuditBeforeCommit()
  {
    var fixture = await ReportDraftFixture.CreateAsync();
    var useCase = new CreateReportDraftUseCase(
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository,
      fixture.ReportRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    var operationId = OperationId.New();

    var result = await useCase.HandleAsync(new CreateReportDraftCommand(
      operationId,
      fixture.Project.Id,
      fixture.InspectionSession.Id,
      "Initial Draft Report",
      [fixture.FieldNote.Id],
      [fixture.EvidenceAttachment.Id],
      [new CreateReportDraftSectionInput("Summary", "Deterministic draft summary.")]));

    var storedReport = await fixture.ReportRepository.GetByIdAsync(result.ReportId);

    Assert.NotNull(storedReport);
    Assert.Equal(ReportLifecycle.Draft, storedReport!.Lifecycle);
    Assert.Equal(1, storedReport.RevisionNumber);
    Assert.Single(storedReport.Sections);
    Assert.Single(storedReport.SourceFieldNoteIds);
    Assert.Single(storedReport.SourceEvidenceAttachmentIds);
    Assert.Single(fixture.ReportRepository.AddedReports);
    Assert.Single(fixture.AuditRecorder.StagedEvents);
    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task CreateReportDraftReusesExistingReportForDuplicateOperation()
  {
    var fixture = await ReportDraftFixture.CreateAsync();
    var useCase = new CreateReportDraftUseCase(
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository,
      fixture.ReportRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    var command = new CreateReportDraftCommand(
      OperationId.New(),
      fixture.Project.Id,
      fixture.InspectionSession.Id,
      "Initial Draft Report",
      [fixture.FieldNote.Id],
      [fixture.EvidenceAttachment.Id],
      [new CreateReportDraftSectionInput("Summary", "Deterministic draft summary.")]);

    var firstResult = await useCase.HandleAsync(command);
    var commitCountAfterFirstCreate = fixture.UnitOfWork.CommitCount;
    var stagedAuditCountAfterFirstCreate = fixture.AuditRecorder.StagedEvents.Count;

    var secondResult = await useCase.HandleAsync(command);

    Assert.Equal(firstResult.ReportId, secondResult.ReportId);
    Assert.True(secondResult.WasDuplicateOperation);
    Assert.Equal(commitCountAfterFirstCreate, fixture.UnitOfWork.CommitCount);
    Assert.Equal(stagedAuditCountAfterFirstCreate, fixture.AuditRecorder.StagedEvents.Count);
    Assert.Single(fixture.ReportRepository.AddedReports);
  }

  [Fact]
  public async Task CreateReportDraftRejectsSourceReferencesOutsideInspectionSession()
  {
    var fixture = await ReportDraftFixture.CreateAsync();
    var useCase = new CreateReportDraftUseCase(
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository,
      fixture.ReportRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      useCase.HandleAsync(new CreateReportDraftCommand(
        OperationId.New(),
        fixture.Project.Id,
        fixture.InspectionSession.Id,
        "Initial Draft Report",
        [FieldNoteId.New()],
        [fixture.EvidenceAttachment.Id],
        [new CreateReportDraftSectionInput("Summary", "Deterministic draft summary.")])));

    Assert.Contains("is not part of inspection session", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public async Task CreateReportDraftRejectsDuplicateSourceReferences()
  {
    var fixture = await ReportDraftFixture.CreateAsync();
    var useCase = new CreateReportDraftUseCase(
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository,
      fixture.ReportRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    await Assert.ThrowsAsync<DomainInvariantException>(() =>
      useCase.HandleAsync(new CreateReportDraftCommand(
        OperationId.New(),
        fixture.Project.Id,
        fixture.InspectionSession.Id,
        "Initial Draft Report",
        [fixture.FieldNote.Id, fixture.FieldNote.Id],
        [fixture.EvidenceAttachment.Id],
        [new CreateReportDraftSectionInput("Summary", "Deterministic draft summary.")])));
  }

  [Fact]
  public async Task CreateReportDraftRejectsChangedPayloadForDuplicateOperation()
  {
    var fixture = await ReportDraftFixture.CreateAsync();
    var useCase = new CreateReportDraftUseCase(
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository,
      fixture.ReportRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    var operationId = OperationId.New();

    await useCase.HandleAsync(new CreateReportDraftCommand(
      operationId,
      fixture.Project.Id,
      fixture.InspectionSession.Id,
      "Initial Draft Report",
      [fixture.FieldNote.Id],
      [fixture.EvidenceAttachment.Id],
      [new CreateReportDraftSectionInput("Summary", "Deterministic draft summary.")]));

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      useCase.HandleAsync(new CreateReportDraftCommand(
        operationId,
        fixture.Project.Id,
        fixture.InspectionSession.Id,
        "Changed Draft Report",
        [fixture.FieldNote.Id],
        [fixture.EvidenceAttachment.Id],
        [new CreateReportDraftSectionInput("Summary", "Changed draft summary.")])));

    Assert.Contains("already used for a different report-draft request", exception.Message, StringComparison.Ordinal);
    Assert.Single(fixture.ReportRepository.AddedReports);
    Assert.Single(fixture.AuditRecorder.StagedEvents);
    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task LoadReportDraftSnapshotReturnsPersistedProvenanceAndAuditHistory()
  {
    var fixture = await ReportDraftFixture.CreateAsync();
    var createReportDraft = new CreateReportDraftUseCase(
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository,
      fixture.ReportRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);
    var createResult = await createReportDraft.HandleAsync(new CreateReportDraftCommand(
      OperationId.New(),
      fixture.Project.Id,
      fixture.InspectionSession.Id,
      "Initial Draft Report",
      [fixture.FieldNote.Id],
      [fixture.EvidenceAttachment.Id],
      [
        new CreateReportDraftSectionInput("Summary", "Deterministic draft summary."),
        new CreateReportDraftSectionInput("Observations", "Deterministic draft observations.")
      ]));

    var loadReportDraft = new LoadReportDraftSnapshotUseCase(
      fixture.ReportRepository,
      fixture.ProjectRepository,
      fixture.InspectionSessionRepository);

    var result = await loadReportDraft.HandleAsync(new LoadReportDraftSnapshotQuery(createResult.ReportId));

    Assert.Equal("Initial Draft Report", result.Title);
    Assert.Equal(1, result.RevisionNumber);
    Assert.Equal(ReportLifecycle.Draft, result.Lifecycle);
    Assert.Equal(fixture.Project.Name, result.ProjectName);
    Assert.Equal(fixture.InspectionSession.Name, result.InspectionSessionName);
    Assert.Equal(["Summary", "Observations"], result.Sections.Select(section => section.Heading));
    Assert.Equal(fixture.FieldNote.Id, result.FieldNotes.Single().FieldNoteId);
    Assert.Equal(fixture.EvidenceAttachment.Id, result.EvidenceAttachments.Single().EvidenceAttachmentId);
    Assert.Single(result.AuditHistory);
    Assert.Equal("ReportCreated", result.AuditHistory.Single().EventType);
  }

  private sealed class ReportDraftFixture
  {
    private ReportDraftFixture(
      FakeProjectRepository projectRepository,
      FakeInspectionSessionRepository inspectionSessionRepository,
      FakeReportRepository reportRepository,
      FakeUnitOfWork unitOfWork,
      FakeClock clock,
      FakeCurrentUser currentUser,
      FakeAuditRecorder auditRecorder,
      Project project,
      InspectionSession inspectionSession,
      FieldNote fieldNote,
      EvidenceAttachment evidenceAttachment)
    {
      ProjectRepository = projectRepository;
      InspectionSessionRepository = inspectionSessionRepository;
      ReportRepository = reportRepository;
      UnitOfWork = unitOfWork;
      Clock = clock;
      CurrentUser = currentUser;
      AuditRecorder = auditRecorder;
      Project = project;
      InspectionSession = inspectionSession;
      FieldNote = fieldNote;
      EvidenceAttachment = evidenceAttachment;
    }

    public FakeProjectRepository ProjectRepository { get; }

    public FakeInspectionSessionRepository InspectionSessionRepository { get; }

    public FakeReportRepository ReportRepository { get; }

    public FakeUnitOfWork UnitOfWork { get; }

    public FakeClock Clock { get; }

    public FakeCurrentUser CurrentUser { get; }

    public FakeAuditRecorder AuditRecorder { get; }

    public Project Project { get; }

    public InspectionSession InspectionSession { get; }

    public FieldNote FieldNote { get; }

    public EvidenceAttachment EvidenceAttachment { get; }

    public static async Task<ReportDraftFixture> CreateAsync()
    {
      var projectRepository = new FakeProjectRepository();
      var inspectionSessionRepository = new FakeInspectionSessionRepository();
      var reportRepository = new FakeReportRepository();
      var unitOfWork = new FakeUnitOfWork();
      var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
      var currentUser = new FakeCurrentUser("inspector@example.invalid");
      var auditRecorder = new FakeAuditRecorder();
      var project = new Project(
        ProjectId.New(),
        "Project Falcon",
        "owner@example.invalid",
        new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
      project.Activate(currentUser.UserId.Value, clock.UtcNow);
      var inspectionSession = new InspectionSession(
        InspectionSessionId.New(),
        project.Id,
        "Initial Walkdown",
        currentUser.UserId.Value,
        clock.UtcNow);
      inspectionSession.Start(currentUser.UserId.Value, clock.UtcNow.AddMinutes(1));
      var fieldNote = inspectionSession.RecordFieldNote(
        FieldNoteId.New(),
        currentUser.UserId.Value,
        clock.UtcNow.AddMinutes(2),
        new FieldNoteRawText("Observed corrosion at lower seam."));
      var evidenceAttachment = inspectionSession.AttachEvidence(
        EvidenceAttachmentId.New(),
        currentUser.UserId.Value,
        clock.UtcNow.AddMinutes(3),
        new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));
      inspectionSession.InterpretEvidence(
        evidenceAttachment.Id,
        new EvidenceInterpretation(
          "Corrosion visible near lower seam.",
          currentUser.UserId.Value,
          clock.UtcNow.AddMinutes(4)));

      await projectRepository.AddAsync(project);
      await inspectionSessionRepository.AddAsync(inspectionSession);

      return new ReportDraftFixture(
        projectRepository,
        inspectionSessionRepository,
        reportRepository,
        unitOfWork,
        clock,
        currentUser,
        auditRecorder,
        project,
        inspectionSession,
        fieldNote,
        evidenceAttachment);
    }
  }
}
