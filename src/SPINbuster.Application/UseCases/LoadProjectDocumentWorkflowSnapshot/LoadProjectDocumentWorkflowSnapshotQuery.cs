using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;

public sealed record LoadProjectDocumentWorkflowSnapshotQuery(
  ProjectId ProjectId,
  int MaxImportSessions,
  int MaxSources,
  int MaxProcessingAttemptsPerSource,
  int MaxCandidatesPerSource,
  int MaxAuditEntriesPerSubject) : IQuery<LoadProjectDocumentWorkflowSnapshotResult>;
