using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.GenerateReportDraftRequest;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class GenerateReportDraftRequestUseCaseTests
{
  [Fact]
  public async Task GenerateReportDraftRequestReturnsSessionSourceMaterial()
  {
    var projectRepository = new FakeProjectRepository();
    var inspectionSessionRepository = new FakeInspectionSessionRepository();
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    project.Activate("owner@example.invalid", new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));

    var inspectionSession = new InspectionSession(
      InspectionSessionId.New(),
      project.Id,
      "Initial Walkdown",
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
    inspectionSession.Start("inspector@example.invalid", new DateTimeOffset(2026, 7, 15, 10, 5, 0, TimeSpan.Zero));
    inspectionSession.RecordFieldNote(
      FieldNoteId.New(),
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 10, 10, 0, TimeSpan.Zero),
      new FieldNoteRawText("Observed leak at gasket seam."));
    var evidenceAttachment = inspectionSession.AttachEvidence(
      EvidenceAttachmentId.New(),
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 10, 15, 0, TimeSpan.Zero),
      new RawEvidenceReference("photo.jpg", "image/jpeg", "evidence/photo.jpg", "sha256:def"));
    inspectionSession.InterpretEvidence(
      evidenceAttachment.Id,
      new EvidenceInterpretation(
        "Leak visible in lower-right quadrant.",
        "reviewer@example.invalid",
        new DateTimeOffset(2026, 7, 15, 10, 20, 0, TimeSpan.Zero)));

    await projectRepository.AddAsync(project);
    await inspectionSessionRepository.AddAsync(inspectionSession);

    var useCase = new GenerateReportDraftRequestUseCase(projectRepository, inspectionSessionRepository);

    var result = await useCase.HandleAsync(new GenerateReportDraftRequestQuery(
      project.Id,
      inspectionSession.Id,
      "Draft inspection report"));

    Assert.Equal("Project Falcon", result.ProjectName);
    Assert.Single(result.FieldNotes);
    Assert.Single(result.EvidenceAttachments);
    Assert.Equal(
      "Leak visible in lower-right quadrant.",
      result.EvidenceAttachments.Single().InterpretationSummary);
  }

  [Fact]
  public async Task GenerateReportDraftRequestRejectsSessionFromDifferentProject()
  {
    var projectRepository = new FakeProjectRepository();
    var inspectionSessionRepository = new FakeInspectionSessionRepository();
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var otherProject = new Project(
      ProjectId.New(),
      "Project Viper",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 30, 0, TimeSpan.Zero));
    var inspectionSession = new InspectionSession(
      InspectionSessionId.New(),
      otherProject.Id,
      "Other Session",
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));

    await projectRepository.AddAsync(project);
    await inspectionSessionRepository.AddAsync(inspectionSession);

    var useCase = new GenerateReportDraftRequestUseCase(projectRepository, inspectionSessionRepository);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      useCase.HandleAsync(new GenerateReportDraftRequestQuery(
        project.Id,
        inspectionSession.Id,
        "Draft inspection report")));

    Assert.Contains("does not belong to project", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public async Task GenerateReportDraftRequestDoesNotMutateStateOrCommit()
  {
    var projectRepository = new FakeProjectRepository();
    var inspectionSessionRepository = new FakeInspectionSessionRepository();
    var unitOfWork = new FakeUnitOfWork();
    var auditRecorder = new FakeAuditRecorder();
    var project = new Project(
      ProjectId.New(),
      "Project Falcon",
      "owner@example.invalid",
      new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var inspectionSession = new InspectionSession(
      InspectionSessionId.New(),
      project.Id,
      "Initial Walkdown",
      "inspector@example.invalid",
      new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));

    await projectRepository.AddAsync(project);
    await inspectionSessionRepository.AddAsync(inspectionSession);

    var useCase = new GenerateReportDraftRequestUseCase(projectRepository, inspectionSessionRepository);

    _ = await useCase.HandleAsync(new GenerateReportDraftRequestQuery(
      project.Id,
      inspectionSession.Id,
      "Draft inspection report"));

    Assert.Empty(projectRepository.UpdatedProjects);
    Assert.Empty(inspectionSessionRepository.UpdatedInspectionSessions);
    Assert.Empty(auditRecorder.StagedEvents);
    Assert.Equal(0, unitOfWork.CommitCount);
  }
}
