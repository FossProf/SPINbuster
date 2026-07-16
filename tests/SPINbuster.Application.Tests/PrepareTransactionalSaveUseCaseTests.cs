using SPINbuster.Application;
using SPINbuster.Application.Tests.Fakes;
using SPINbuster.Application.UseCases.PrepareTransactionalSave;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class PrepareTransactionalSaveUseCaseTests
{
  [Fact]
  public async Task PrepareTransactionalSaveCreatesPreparedSaveTransaction()
  {
    var reportRepository = new FakeReportRepository();
    var saveTransactionRepository = new FakeSaveTransactionRepository();
    var unitOfWork = new FakeUnitOfWork();
    var clock = new FakeClock(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero));
    var currentUser = new FakeCurrentUser("system@example.invalid");
    var auditRecorder = new FakeAuditRecorder();
    var report = new Report(
      ReportId.New(),
      ProjectId.New(),
      InspectionSessionId.New(),
      new ReportTitle("Draft Report"),
      [new ReportDraftSection("Summary", "Findings pending review.")],
      [FieldNoteId.New()],
      [],
      "author@example.invalid",
      new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero));
    await reportRepository.AddAsync(report, OperationId.New());

    var useCase = new PrepareTransactionalSaveUseCase(
      reportRepository,
      saveTransactionRepository,
      unitOfWork,
      clock,
      currentUser,
      auditRecorder);

    var result = await useCase.HandleAsync(new PrepareTransactionalSaveCommand(report.Id));
    var storedSaveTransaction = await saveTransactionRepository.GetByIdAsync(result.SaveTransactionId);

    Assert.NotNull(storedSaveTransaction);
    Assert.Equal(SaveTransactionState.Prepared, result.State);
    Assert.Equal(1, unitOfWork.CommitCount);
    Assert.Single(auditRecorder.StagedEvents);
  }
}
