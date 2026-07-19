using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadParsingSnapshot;

public sealed record LoadParsingSnapshotQuery(
  ProjectId ProjectId,
  ImportedSourceId ImportedSourceId) : IQuery<LoadParsingSnapshotResult>;
