using Microsoft.Extensions.Logging;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.CreateProject;

namespace SPINbuster.Application.Tests.Logging;

public sealed class CreateProjectUseCaseLoggingTests
{
  [Fact]
  public async Task SuccessfulCreateLogsStartAndCompletionWithEventIds()
  {
    var logSpy = new LogSpy<CreateProjectUseCase>();
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      logSpy);

    await useCase.HandleAsync(new CreateProjectCommand("Project Falcon"));

    var entries = logSpy.Entries.ToList();

    Assert.Equal(2, entries.Count);

    var startEntry = entries[0];
    Assert.Equal(LogEvents.UseCaseStarting, startEntry.EventId);
    Assert.Equal(LogLevel.Information, startEntry.LogLevel);

    var completionEntry = entries[1];
    Assert.Equal(LogEvents.UseCaseCompleted, completionEntry.EventId);
    Assert.Equal(LogLevel.Information, completionEntry.LogLevel);
  }

  [Fact]
  public async Task SuccessfulCreateLogsDurationMsInCompletionMessage()
  {
    var logSpy = new LogSpy<CreateProjectUseCase>();
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      logSpy);

    await useCase.HandleAsync(new CreateProjectCommand("Project Falcon"));

    var completionEntry = logSpy.Entries[^1];
    Assert.Contains("ms for", completionEntry.Message);
  }

  [Fact]
  public async Task FailedCreateLogsErrorWithExceptionAndEventId()
  {
    var logSpy = new LogSpy<CreateProjectUseCase>();
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
      logSpy);

    await Assert.ThrowsAsync<InvalidOperationException>(
      () => useCase.HandleAsync(new CreateProjectCommand("Project Falcon")));

    var errorEntry = logSpy.Entries[^1];
    Assert.Equal(LogEvents.UseCaseFailed, errorEntry.EventId);
    Assert.Equal(LogLevel.Error, errorEntry.LogLevel);
    Assert.NotNull(errorEntry.Exception);
  }

  [Fact]
  public async Task StructuredScopeContainsUseCaseAndApplicationUserId()
  {
    var logSpy = new LogSpy<CreateProjectUseCase>();
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      logSpy);

    await useCase.HandleAsync(new CreateProjectCommand("Project Falcon"));

    var scope = logSpy.GetLastScope();
    Assert.Equal(nameof(CreateProjectUseCase), scope[LogProperties.UseCase]);
    Assert.Equal("owner@example.invalid", scope[LogProperties.ApplicationUserId]);
  }

  [Fact]
  public async Task LogEntriesContainUseCaseNameInMessage()
  {
    var logSpy = new LogSpy<CreateProjectUseCase>();
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      logSpy);

    await useCase.HandleAsync(new CreateProjectCommand("Project Falcon"));

    var entries = logSpy.Entries.ToList();
    Assert.All(entries, entry => Assert.Contains(nameof(CreateProjectUseCase), entry.Message));
  }

  [Fact]
  public async Task NoSensitiveDataAppearsInLogMessages()
  {
    var logSpy = new LogSpy<CreateProjectUseCase>();
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      logSpy);

    await useCase.HandleAsync(new CreateProjectCommand("Project Falcon"));

    var allMessages = string.Join(" ", logSpy.Entries.Select(e => e.Message));
    Assert.DoesNotContain("Data Source=", allMessages);
    Assert.DoesNotContain(".sqlite", allMessages);
    Assert.DoesNotContain("password", allMessages, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task AllLogEntriesHaveCorrectEventIdNames()
  {
    var logSpy = new LogSpy<CreateProjectUseCase>();
    var projectRepository = new FakeProjectRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("owner@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var useCase = new CreateProjectUseCase(
      projectRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder,
      logSpy);

    await useCase.HandleAsync(new CreateProjectCommand("Project Falcon"));

    var entries = logSpy.Entries.ToList();
    Assert.All(entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.EventId.Name)));
  }
}
