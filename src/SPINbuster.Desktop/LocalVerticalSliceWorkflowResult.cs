using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.StartInspectionSession;

namespace SPINbuster.Desktop;

public sealed record LocalVerticalSliceWorkflowResult(
  CreateProjectResult CreatedProject,
  StartInspectionSessionResult StartedInspectionSession,
  CaptureFieldNoteResult CapturedFieldNote,
  LoadInspectionWorkflowSnapshotResult PersistedSnapshot);
