using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class CreateProjectUseCaseTests
{
  [Fact]
  public async Task CreateProjectCreatesDraftProjectAndStagesAuditBeforeCommit()
  {
    var operationLog = new List<string>();
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork(operationLog);
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder(operationLog);
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      NullLogger<CreateProjectUseCase>.Instance);

    var result = await useCase.HandleAsync(new CreateProjectCommand("Project Falcon"));
    var storedProject = await projectRepository.GetByIdAsync(result.ProjectId);

    Assert.NotNull(storedProject);
    Assert.Equal(ProjectLifecycle.Draft, result.Lifecycle);
    Assert.Equal(1, unitOfWork.CommitCount);
    Assert.Single(auditRecorder.StagedEvents);
    Assert.Equal(["audit-stage", "commit"], operationLog);
  }

  [Fact]
  public async Task CreateProjectDoesNotReturnSuccessWhenCommitFails()
  {
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork { ThrowOnCommit = true };
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      NullLogger<CreateProjectUseCase>.Instance);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      useCase.HandleAsync(new CreateProjectCommand("Project Falcon")));

    Assert.Equal("Commit failed.", exception.Message);
    Assert.Single(auditRecorder.StagedEvents);
    Assert.Equal(0, unitOfWork.CommitCount);
  }

  [Fact]
  public async Task CreateProjectDoesNotCommitWhenAuditStagingFails()
  {
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder { ThrowOnStage = true };
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      NullLogger<CreateProjectUseCase>.Instance);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      useCase.HandleAsync(new CreateProjectCommand("Project Falcon")));

    Assert.Equal("Audit staging failed.", exception.Message);
    Assert.Equal(0, unitOfWork.CommitCount);
    Assert.Empty(auditRecorder.StagedEvents);
  }
}
