using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.ActivateProject;

public sealed class ActivateProjectUseCase
  : ICommandHandler<ActivateProjectCommand, ActivateProjectResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly ILogger<ActivateProjectUseCase> _logger;
  private readonly IProjectRepository _projectRepository;
  private readonly IUnitOfWork _unitOfWork;

  public ActivateProjectUseCase(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder,
    ILogger<ActivateProjectUseCase> logger)
  {
    _projectRepository = projectRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
    _logger = logger;
  }

  public async Task<ActivateProjectResult> HandleAsync(
    ActivateProjectCommand command,
    CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    var useCaseName = nameof(ActivateProjectUseCase);

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.ApplicationUserId] = _currentUser.UserId.Value,
    }))
    {
      _logger.LogInformation(
        "{UseCase} starting for project {ProjectId}",
        useCaseName, command.ProjectId);

      try
      {
        var project = await _projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
          ?? throw new ApplicationEntityNotFoundException(nameof(Project), command.ProjectId.ToString());

        var projectAuditStart = project.AuditTrail.Count;

        if (project.Lifecycle == ProjectLifecycle.Active)
        {
          stopwatch.Stop();
          _logger.LogInformation(
            "{UseCase} completed (already active) in {DurationMs}ms for project {ProjectId}",
            useCaseName, stopwatch.ElapsedMilliseconds, command.ProjectId);

          return new ActivateProjectResult(
            project.Id,
            project.Name,
            project.Lifecycle);
        }

        if (project.Lifecycle is not ProjectLifecycle.Draft)
        {
          throw new DomainInvariantException(
            $"Project {project.Id} is {project.Lifecycle} and cannot be activated. Only Draft projects can be activated.");
        }

        project.Activate(_currentUser.UserId.Value, _clock.UtcNow);

        await _projectRepository.UpdateAsync(project, cancellationToken);
        StageAuditEvents(AuditTrailSlice.GetNewEvents(project, projectAuditStart));
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
          "{UseCase} completed in {DurationMs}ms for project {ProjectId}",
          useCaseName, stopwatch.ElapsedMilliseconds, command.ProjectId);

        return new ActivateProjectResult(
          project.Id,
          project.Name,
          project.Lifecycle);
      }
      catch (OperationCanceledException)
      {
        stopwatch.Stop();
        throw;
      }
      catch (ApplicationEntityNotFoundException)
      {
        stopwatch.Stop();
        throw;
      }
      catch (Exception exception)
      {
        stopwatch.Stop();
        _logger.LogError(
          exception,
          "{UseCase} failed in {DurationMs}ms for project {ProjectId}",
          useCaseName, stopwatch.ElapsedMilliseconds, command.ProjectId);
        throw;
      }
    }
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
