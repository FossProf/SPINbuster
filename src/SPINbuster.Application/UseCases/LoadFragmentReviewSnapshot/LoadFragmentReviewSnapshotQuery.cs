using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadFragmentReviewSnapshot;

public sealed record LoadFragmentReviewSnapshotQuery(
  ProjectId ProjectId,
  FragmentCandidateReviewState? ReviewStateFilter,
  ParserRunId? ParserRunFilter,
  ImportedSourceId? SourceFilter,
  ContentKind? ContentKindFilter,
  int MaxResults) : IQuery<LoadFragmentReviewSnapshotResult>;
