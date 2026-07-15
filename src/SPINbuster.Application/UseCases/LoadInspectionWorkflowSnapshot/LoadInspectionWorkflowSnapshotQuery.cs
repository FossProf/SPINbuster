using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;

public sealed record LoadInspectionWorkflowSnapshotQuery(
  ProjectId ProjectId,
  InspectionSessionId InspectionSessionId) : IQuery<LoadInspectionWorkflowSnapshotResult>;
