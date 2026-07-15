using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;

public sealed class LoadInspectionWorkflowSnapshotUseCase
  : IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult>
{
  private readonly IInspectionSessionRepository _inspectionSessionRepository;
  private readonly IProjectRepository _projectRepository;

  public LoadInspectionWorkflowSnapshotUseCase(
    IProjectRepository projectRepository,
    IInspectionSessionRepository inspectionSessionRepository)
  {
    _projectRepository = projectRepository;
    _inspectionSessionRepository = inspectionSessionRepository;
  }

  public async Task<LoadInspectionWorkflowSnapshotResult> HandleAsync(
    LoadInspectionWorkflowSnapshotQuery query,
    CancellationToken cancellationToken = default)
  {
    var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(Project), query.ProjectId.ToString());
    var inspectionSession = await _inspectionSessionRepository.GetByIdAsync(query.InspectionSessionId, cancellationToken)
      ?? throw new ApplicationEntityNotFoundException(nameof(InspectionSession), query.InspectionSessionId.ToString());

    if (inspectionSession.ProjectId != project.Id)
    {
      throw new InvalidOperationException(
        $"Inspection session {inspectionSession.Id} does not belong to project {project.Id}.");
    }

    // This query intentionally reloads the aggregates after the write workflow
    // has committed so the host sees persisted state and persisted audit facts,
    // not in-memory command results.
    return new LoadInspectionWorkflowSnapshotResult(
      new PersistedProjectSnapshot(
        project.Id,
        project.Name,
        project.Lifecycle,
        project.AuditTrail.Select(ToPersistedAuditEntry).ToArray()),
      new PersistedInspectionSessionSnapshot(
        inspectionSession.Id,
        inspectionSession.ProjectId,
        inspectionSession.Name,
        inspectionSession.Lifecycle,
        inspectionSession.FieldNotes
          .Select(fieldNote => new PersistedFieldNote(
            fieldNote.Id,
            fieldNote.RawText.Value,
            fieldNote.CapturedAtUtc))
          .ToArray(),
        inspectionSession.AuditTrail.Select(ToPersistedAuditEntry).ToArray()));
  }

  private static PersistedAuditEntry ToPersistedAuditEntry(AuditEvent auditEvent)
  {
    return new PersistedAuditEntry(
      auditEvent.Id,
      auditEvent.EventType,
      auditEvent.Actor,
      auditEvent.OccurredAtUtc,
      auditEvent.Description);
  }
}
