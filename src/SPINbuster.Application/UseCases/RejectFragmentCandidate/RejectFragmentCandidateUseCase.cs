using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RejectFragmentCandidate;

public sealed class RejectFragmentCandidateUseCase : ICommandHandler<RejectFragmentCandidateCommand, RejectFragmentCandidateResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IFragmentCandidateRepository _fragmentCandidateRepository;
  private readonly ILogger<RejectFragmentCandidateUseCase> _logger;
  private readonly IUnitOfWork _unitOfWork;

  public RejectFragmentCandidateUseCase(
    IFragmentCandidateRepository fragmentCandidateRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder,
    ILogger<RejectFragmentCandidateUseCase> logger)
  {
    _fragmentCandidateRepository = fragmentCandidateRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
    _logger = logger;
  }

  public async Task<RejectFragmentCandidateResult> HandleAsync(RejectFragmentCandidateCommand command, CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    var useCaseName = nameof(RejectFragmentCandidateUseCase);
    var fragmentCandidateId = command.FragmentCandidateId.ToString();

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.FragmentCandidateId] = fragmentCandidateId,
    }))
    {
      _logger.LogInformation(LogEvents.FragmentReviewStarting,
        "{UseCase} starting for fragment candidate {FragmentCandidateId}",
        useCaseName, fragmentCandidateId);

      try
      {
        var candidate = await _fragmentCandidateRepository.GetByIdAsync(command.FragmentCandidateId, cancellationToken)
          ?? throw new ApplicationEntityNotFoundException(nameof(FragmentCandidate), command.FragmentCandidateId.ToString());

        var priorAuditCount = candidate.AuditTrail.Count;
        candidate.Reject(_currentUser.UserId.Value, _clock.UtcNow, command.ReviewNotes);

        await _fragmentCandidateRepository.UpdateAsync(candidate, cancellationToken);
        Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail.Skip(priorAuditCount));
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(LogEvents.FragmentReviewCompleted,
          "{UseCase} completed in {DurationMs}ms for fragment candidate {FragmentCandidateId}, review state {ReviewState}",
          useCaseName, stopwatch.ElapsedMilliseconds, fragmentCandidateId, candidate.ReviewState);

        return new RejectFragmentCandidateResult(
          candidate.Id,
          candidate.ReviewState,
          _currentUser.UserId.Value,
          candidate.ReviewedAtUtc!.Value);
      }
      catch (OperationCanceledException)
      {
        stopwatch.Stop();
        _logger.LogWarning(LogEvents.FragmentReviewCancelled,
          "{UseCase} cancelled in {DurationMs}ms for fragment candidate {FragmentCandidateId}",
          useCaseName, stopwatch.ElapsedMilliseconds, fragmentCandidateId);
        throw;
      }
      catch (Exception exception)
      {
        stopwatch.Stop();
        _logger.LogError(LogEvents.FragmentReviewFailed,
          exception,
          "{UseCase} failed in {DurationMs}ms for fragment candidate {FragmentCandidateId}",
          useCaseName, stopwatch.ElapsedMilliseconds, fragmentCandidateId);
        throw;
      }
    }
  }
}
