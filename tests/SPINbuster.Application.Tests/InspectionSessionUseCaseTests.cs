using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.AddInterpretation;
using SPINbuster.Application.UseCases.AttachEvidence;
using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.StartInspectionSession;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class InspectionSessionUseCaseTests
{
  [Fact]
  public async Task StartInspectionSessionActivatesDraftProjectAndStartsSession()
  {
    var projectRepository = new FakeProjectRepository();
    var inspectionSessionRepository = new FakeInspectionSessionRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("inspector@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    await projectRepository.AddAsync(project);

    var useCase = new StartInspectionSessionUseCase(
      projectRepository,
      inspectionSessionRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder);

    var result = await useCase.HandleAsync(new StartInspectionSessionCommand(project.Id, "Initial Walkdown"));
    var storedProject = await projectRepository.GetByIdAsync(project.Id);
    var storedSession = await inspectionSessionRepository.GetByIdAsync(result.InspectionSessionId);

    Assert.NotNull(storedProject);
    Assert.Equal(ProjectLifecycle.Active, storedProject!.Lifecycle);
    Assert.NotNull(storedSession);
    Assert.Equal(InspectionSessionLifecycle.InProgress, storedSession!.Lifecycle);
    Assert.Single(projectRepository.UpdatedProjects);
    Assert.Equal(ProjectLifecycle.Active, projectRepository.UpdatedProjects[0].Lifecycle);
    Assert.Equal(1, unitOfWork.CommitCount);
    Assert.Equal(3, auditRecorder.StagedEvents.Count);
    Assert.Equal(
      ["ProjectActivated", "InspectionSessionCreated", "InspectionSessionStarted"],
      auditRecorder.StagedEvents.Select(auditEvent => auditEvent.EventType));
  }

  [Fact]
  public async Task CaptureFieldNoteAddsImmutableRawTextToSession()
  {
    var fixture = await ApplicationFixture.CreateStartedInspectionSessionAsync();
    var useCase = new CaptureFieldNoteUseCase(
      fixture.InspectionSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var result = await useCase.HandleAsync(new CaptureFieldNoteCommand(
      fixture.InspectionSession.Id,
      "  observed pressure drop at valve A  "));

    var storedSession = await fixture.InspectionSessionRepository.GetByIdAsync(fixture.InspectionSession.Id);

    Assert.NotNull(storedSession);
    Assert.Single(storedSession!.FieldNotes);
    Assert.Equal(result.FieldNoteId, storedSession.FieldNotes[0].Id);
    Assert.Equal("  observed pressure drop at valve A  ", storedSession.FieldNotes[0].RawText.Value);
  }

  [Fact]
  public async Task AttachEvidenceAndInterpretationFlowThroughInspectionSessionOwnership()
  {
    var fixture = await ApplicationFixture.CreateStartedInspectionSessionAsync();
    var attachEvidenceUseCase = new AttachEvidenceUseCase(
      fixture.InspectionSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var attachedEvidence = await attachEvidenceUseCase.HandleAsync(new AttachEvidenceCommand(
      fixture.InspectionSession.Id,
      "photo-01.jpg",
      "image/jpeg",
      "evidence/photo-01.jpg",
      "sha256:abc"));

    fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(5);

    var addInterpretationUseCase = new AddInterpretationUseCase(
      fixture.InspectionSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var interpretedEvidence = await addInterpretationUseCase.HandleAsync(new AddInterpretationCommand(
      fixture.InspectionSession.Id,
      attachedEvidence.EvidenceAttachmentId,
      "Corrosion visible near seam."));

    var storedSession = await fixture.InspectionSessionRepository.GetByIdAsync(fixture.InspectionSession.Id);

    Assert.NotNull(storedSession);
    Assert.Single(storedSession!.EvidenceAttachments);
    Assert.Equal(interpretedEvidence.Summary, storedSession.EvidenceAttachments[0].Interpretation!.Summary);
    Assert.Equal(2, fixture.InspectionSessionRepository.UpdatedInspectionSessions.Count);
    Assert.Equal(2, fixture.UnitOfWork.CommitCount);
  }

  [Fact]
  public async Task StartInspectionSessionRejectsCompletedProject()
  {
    var projectRepository = new FakeProjectRepository();
    var inspectionSessionRepository = new FakeInspectionSessionRepository();
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
    project.Complete(currentUser.UserId.Value, clock.UtcNow.AddMinutes(1));
    await projectRepository.AddAsync(project);

    var useCase = new StartInspectionSessionUseCase(
      projectRepository,
      inspectionSessionRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      useCase.HandleAsync(new StartInspectionSessionCommand(project.Id, "Late Session")));

    Assert.Contains("cannot start a new inspection session", exception.Message, StringComparison.Ordinal);
    Assert.Equal(0, unitOfWork.CommitCount);
    Assert.Empty(auditRecorder.StagedEvents);
  }

  [Fact]
  public async Task AddInterpretationDoesNotAllowReplacingExistingInterpretation()
  {
    var fixture = await ApplicationFixture.CreateStartedInspectionSessionAsync();
    var attachEvidenceUseCase = new AttachEvidenceUseCase(
      fixture.InspectionSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    var attachedEvidence = await attachEvidenceUseCase.HandleAsync(new AttachEvidenceCommand(
      fixture.InspectionSession.Id,
      "photo-01.jpg",
      "image/jpeg",
      "evidence/photo-01.jpg",
      "sha256:abc"));

    var addInterpretationUseCase = new AddInterpretationUseCase(
      fixture.InspectionSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    await addInterpretationUseCase.HandleAsync(new AddInterpretationCommand(
      fixture.InspectionSession.Id,
      attachedEvidence.EvidenceAttachmentId,
      "First interpretation."));

    var exception = await Assert.ThrowsAsync<DomainInvariantException>(() =>
      addInterpretationUseCase.HandleAsync(new AddInterpretationCommand(
        fixture.InspectionSession.Id,
        attachedEvidence.EvidenceAttachmentId,
        "Replacement interpretation.")));

    Assert.Contains("already recorded", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public async Task CaptureFieldNoteStagesAuditAndAggregateUpdateBeforeCommit()
  {
    var fixture = await ApplicationFixture.CreateStartedInspectionSessionAsync();
    var useCase = new CaptureFieldNoteUseCase(
      fixture.InspectionSessionRepository,
      fixture.UnitOfWork,
      fixture.Clock,
      fixture.CurrentUser,
      fixture.AuditRecorder);

    await useCase.HandleAsync(new CaptureFieldNoteCommand(
      fixture.InspectionSession.Id,
      "Observed label mismatch."));

    Assert.Single(fixture.InspectionSessionRepository.UpdatedInspectionSessions);
    Assert.Single(fixture.AuditRecorder.StagedEvents);
    Assert.Equal(1, fixture.UnitOfWork.CommitCount);
  }

  private sealed class ApplicationFixture
  {
    private ApplicationFixture(
      FakeProjectRepository projectRepository,
      FakeInspectionSessionRepository inspectionSessionRepository,
      FakeUnitOfWork unitOfWork,
      FakeClock clock,
      FakeCurrentUser currentUser,
      FakeAuditRecorder auditRecorder,
      InspectionSession inspectionSession)
    {
      ProjectRepository = projectRepository;
      InspectionSessionRepository = inspectionSessionRepository;
      UnitOfWork = unitOfWork;
      Clock = clock;
      CurrentUser = currentUser;
      AuditRecorder = auditRecorder;
      InspectionSession = inspectionSession;
    }

    public FakeProjectRepository ProjectRepository { get; }

    public FakeInspectionSessionRepository InspectionSessionRepository { get; }

    public FakeUnitOfWork UnitOfWork { get; }

    public FakeClock Clock { get; }

    public FakeCurrentUser CurrentUser { get; }

    public FakeAuditRecorder AuditRecorder { get; }

    public InspectionSession InspectionSession { get; }

    public static async Task<ApplicationFixture> CreateStartedInspectionSessionAsync()
    {
      var projectRepository = new FakeProjectRepository();
      var inspectionSessionRepository = new FakeInspectionSessionRepository();
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
      inspectionSession.Start(currentUser.UserId.Value, clock.UtcNow);

      await projectRepository.AddAsync(project);
      await inspectionSessionRepository.AddAsync(inspectionSession);

      return new ApplicationFixture(
        projectRepository,
        inspectionSessionRepository,
        unitOfWork,
        clock,
        currentUser,
        auditRecorder,
        inspectionSession);
    }
  }
}
