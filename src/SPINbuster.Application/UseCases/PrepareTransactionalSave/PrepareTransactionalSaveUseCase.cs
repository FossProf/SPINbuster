using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.PrepareTransactionalSave;

public sealed class PrepareTransactionalSaveUseCase
  : ICommandHandler<PrepareTransactionalSaveCommand, PrepareTransactionalSaveResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IReportRepository _reportRepository;
  private readonly ISaveTransactionRepository _saveTransactionRepository;
  private readonly IUnitOfWork _unitOfWork;

  public PrepareTransactionalSaveUseCase(
    IReportRepository reportRepository,
    ISaveTransactionRepository saveTransactionRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _reportRepository = reportRepository;
    _saveTransactionRepository = saveTransactionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<PrepareTransactionalSaveResult> HandleAsync(
    PrepareTransactionalSaveCommand command,
    CancellationToken cancellationToken = default)
  {
    var report = await _reportRepository.GetByIdAsync(command.ReportId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Report), command.ReportId.ToString());

    var saveTransaction = new SaveTransaction(
      SaveTransactionId.New(),
      report.Id,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    var auditStart = saveTransaction.AuditTrail.Count;
    saveTransaction.Prepare(_currentUser.UserId.Value, _clock.UtcNow);

    await _saveTransactionRepository.AddAsync(saveTransaction, cancellationToken);
    StageAuditEvents(AuditTrailSlice.GetNewEvents(saveTransaction, auditStart));
    await _unitOfWork.CommitAsync(cancellationToken);

    return new PrepareTransactionalSaveResult(
      saveTransaction.Id,
      saveTransaction.ReportId,
      saveTransaction.State,
      saveTransaction.PreparedAtUtc!.Value);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
