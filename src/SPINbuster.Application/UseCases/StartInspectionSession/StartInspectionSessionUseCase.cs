using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.StartInspectionSession;

public sealed class StartInspectionSessionUseCase
  : ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IProjectRepository _projectRepository;
  private readonly IUnitOfWork _unitOfWork;

  public StartInspectionSessionUseCase(
    IProjectRepository projectRepository,
    IInspectionSessionRepository inspectionSessionRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _projectRepository = projectRepository;
    _inspectionSessionRepository = inspectionSessionRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<StartInspectionSessionResult> HandleAsync(
    StartInspectionSessionCommand command,
    CancellationToken cancellationToken = default)
  {
    var project = await _projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Project), command.ProjectId.ToString());

    var projectAuditStart = project.AuditTrail.Count;
    if (project.Lifecycle == ProjectLifecycle.Draft)
    {
      // Session creation is the first operational activity for a draft project,
      // so the application layer promotes it before opening the session.
      project.Activate(_currentUser.UserId.Value, _clock.UtcNow);
    }
    else if (project.Lifecycle is ProjectLifecycle.Completed or ProjectLifecycle.Archived)
    {
      throw new InvalidOperationException(
        $"Project {project.Id} is {project.Lifecycle} and cannot start a new inspection session.");
    }

    var inspectionSession = new InspectionSession(
      InspectionSessionId.New(),
      project.Id,
      command.SessionName,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    inspectionSession.Start(_currentUser.UserId.Value, _clock.UtcNow);

    await _projectRepository.UpdateAsync(project, cancellationToken);
    await _inspectionSessionRepository.AddAsync(inspectionSession, cancellationToken);
    var newAuditEvents = AuditTrailSlice.GetNewEvents(project, projectAuditStart)
      // New inspection sessions are staged as brand-new aggregates, so both the
      // creation event and the start event must be persisted in the same commit.
      .Concat(AuditTrailSlice.GetNewEvents(inspectionSession, 0))
      .ToArray();
    StageAuditEvents(newAuditEvents);
    await _unitOfWork.CommitAsync(cancellationToken);

    return new StartInspectionSessionResult(
      inspectionSession.Id,
      inspectionSession.ProjectId,
      inspectionSession.Lifecycle,
      inspectionSession.StartedAtUtc!.Value);
  }

  private void StageAuditEvents(IEnumerable<AuditEvent> auditEvents)
  {
    foreach (var auditEvent in auditEvents)
    {
      _auditRecorder.Stage(auditEvent);
    }
  }
}
