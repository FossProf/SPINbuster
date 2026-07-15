using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.StartInspectionSession;

namespace SPINbuster.Desktop;

public sealed class LocalVerticalSliceWorkflowRunner
{
  private readonly ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult> _captureFieldNote;
  private readonly ICommandHandler<CreateProjectCommand, CreateProjectResult> _createProject;
  private readonly IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult> _loadWorkflowSnapshot;
  private readonly DesktopWorkflowSettings _settings;
  private readonly ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult> _startInspectionSession;

  public LocalVerticalSliceWorkflowRunner(
    ICommandHandler<CreateProjectCommand, CreateProjectResult> createProject,
    ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult> startInspectionSession,
    ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult> captureFieldNote,
    IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult> loadWorkflowSnapshot,
    DesktopWorkflowSettings settings)
  {
    _createProject = createProject;
    _startInspectionSession = startInspectionSession;
    _captureFieldNote = captureFieldNote;
    _loadWorkflowSnapshot = loadWorkflowSnapshot;
    _settings = settings;
  }

  public async Task<LocalVerticalSliceWorkflowResult> RunAsync(CancellationToken cancellationToken = default)
  {
    var createdProject = await _createProject.HandleAsync(
      new CreateProjectCommand(_settings.ProjectName),
      cancellationToken);
    var startedInspectionSession = await _startInspectionSession.HandleAsync(
      new StartInspectionSessionCommand(createdProject.ProjectId, _settings.SessionName),
      cancellationToken);
    var capturedFieldNote = await _captureFieldNote.HandleAsync(
      new CaptureFieldNoteCommand(startedInspectionSession.InspectionSessionId, _settings.FieldNoteText),
      cancellationToken);
    var persistedSnapshot = await _loadWorkflowSnapshot.HandleAsync(
      new LoadInspectionWorkflowSnapshotQuery(
        createdProject.ProjectId,
        startedInspectionSession.InspectionSessionId),
      cancellationToken);

    return new LocalVerticalSliceWorkflowResult(
      createdProject,
      startedInspectionSession,
      capturedFieldNote,
      persistedSnapshot);
  }
}
