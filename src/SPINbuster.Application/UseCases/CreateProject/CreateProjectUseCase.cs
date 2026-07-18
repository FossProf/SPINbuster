using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateProject;

public sealed class CreateProjectUseCase : ICommandHandler<CreateProjectCommand, CreateProjectResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly ILogger<CreateProjectUseCase> _logger;
  private readonly IProjectRepository _projectRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CreateProjectUseCase(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder,
    ILogger<CreateProjectUseCase> logger)
  {
    _projectRepository = projectRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
    _logger = logger;
  }

  public async Task<CreateProjectResult> HandleAsync(
    CreateProjectCommand command,
    CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    var useCaseName = nameof(CreateProjectUseCase);

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.ApplicationUserId] = _currentUser.UserId.Value,
    }))
    {
      _logger.LogInformation(LogEvents.UseCaseStarting,
        "{UseCase} starting for user {ApplicationUserId}",
        useCaseName, _currentUser.UserId.Value);

      try
      {
        var project = new Project(
          ProjectId.New(),
          command.Name,
          _currentUser.UserId.Value,
          _clock.UtcNow);

        await _projectRepository.AddAsync(project, cancellationToken);
        StageAuditEvents(project.AuditTrail);
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(LogEvents.UseCaseCompleted,
          "{UseCase} completed in {DurationMs}ms for project {ProjectId}",
          useCaseName, stopwatch.ElapsedMilliseconds, project.Id);

        return new CreateProjectResult(
          project.Id,
          project.Name,
          project.Lifecycle,
          project.CreatedAtUtc);
      }
      catch (OperationCanceledException)
      {
        stopwatch.Stop();
        _logger.LogWarning(LogEvents.UseCaseCancelled,
          "{UseCase} cancelled after {DurationMs}ms",
          useCaseName, stopwatch.ElapsedMilliseconds);
        throw;
      }
      catch (Exception exception)
      {
        stopwatch.Stop();
        _logger.LogError(LogEvents.UseCaseFailed,
          exception,
          "{UseCase} failed after {DurationMs}ms: {FailureClassification}",
          useCaseName, stopwatch.ElapsedMilliseconds, exception.GetType().Name);
        throw;
      }
    }
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    // Stage audit facts before the unit of work commits so Infrastructure can
    // persist state and audit records inside one logical transaction.
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
