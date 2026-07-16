using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Internal;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.CreateProject;

public sealed class CreateProjectUseCase : ICommandHandler<CreateProjectCommand, CreateProjectResult>
{
  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IProjectRepository _projectRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CreateProjectUseCase(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder)
  {
    _projectRepository = projectRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
  }

  public async Task<CreateProjectResult> HandleAsync(
    CreateProjectCommand command,
    CancellationToken cancellationToken = default)
  {
    var project = new Project(
      ProjectId.New(),
      command.Name,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    await _projectRepository.AddAsync(project, cancellationToken);
    StageAuditEvents(project.AuditTrail);
    await _unitOfWork.CommitAsync(cancellationToken);

    return new CreateProjectResult(
      project.Id,
      project.Name,
      project.Lifecycle,
      project.CreatedAtUtc);
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
