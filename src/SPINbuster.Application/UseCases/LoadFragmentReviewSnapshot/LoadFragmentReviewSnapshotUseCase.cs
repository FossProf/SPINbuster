using Microsoft.Extensions.Logging;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadFragmentReviewSnapshot;

public sealed class LoadFragmentReviewSnapshotUseCase : IQueryHandler<LoadFragmentReviewSnapshotQuery, LoadFragmentReviewSnapshotResult>
{
  private const int MaxTextPreviewLength = 200;

  private readonly IFragmentCandidateRepository _fragmentCandidateRepository;
  private readonly IParserRunRepository _parserRunRepository;
  private readonly ILogger<LoadFragmentReviewSnapshotUseCase> _logger;

  public LoadFragmentReviewSnapshotUseCase(
    IFragmentCandidateRepository fragmentCandidateRepository,
    IParserRunRepository parserRunRepository,
    ILogger<LoadFragmentReviewSnapshotUseCase> logger)
  {
    _fragmentCandidateRepository = fragmentCandidateRepository;
    _parserRunRepository = parserRunRepository;
    _logger = logger;
  }

  public async Task<LoadFragmentReviewSnapshotResult> HandleAsync(LoadFragmentReviewSnapshotQuery query, CancellationToken cancellationToken = default)
  {
    var useCaseName = nameof(LoadFragmentReviewSnapshotUseCase);
    var projectId = query.ProjectId.ToString();

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.ProjectId] = projectId,
    }))
    {
      _logger.LogInformation(LogEvents.UseCaseStarting,
        "{UseCase} starting for project {ProjectId}",
        useCaseName, projectId);

      IReadOnlyCollection<FragmentCandidate> candidates;

      if (query.ParserRunFilter.HasValue)
      {
        candidates = await _fragmentCandidateRepository.GetByParserRunAsync(
          query.ParserRunFilter.Value, query.MaxResults, cancellationToken);
      }
      else if (query.SourceFilter.HasValue)
      {
        candidates = await _fragmentCandidateRepository.GetByImportedSourceAsync(
          query.SourceFilter.Value, query.MaxResults, cancellationToken);
      }
      else
      {
        candidates = await _fragmentCandidateRepository.GetByProjectFilteredAsync(
          query.ProjectId, query.MaxResults, query.ReviewStateFilter, cancellationToken);
      }

      var parserRunCache = new Dictionary<ParserRunId, ParserRun?>(capacity: 0);

      var filtered = candidates
        .Where(c => c.ProjectId == query.ProjectId)
        .AsEnumerable();

      if (query.ReviewStateFilter.HasValue && !query.ParserRunFilter.HasValue && !query.SourceFilter.HasValue)
      {
        // Review state already filtered at repository level for project-scoped queries
      }
      else if (query.ReviewStateFilter.HasValue)
      {
        filtered = filtered.Where(c => c.ReviewState == query.ReviewStateFilter.Value);
      }

      if (query.ContentKindFilter.HasValue)
      {
        filtered = filtered.Where(c => c.ContentKind == query.ContentKindFilter.Value);
      }

      var entries = filtered
        .Take(query.MaxResults)
        .Select(c =>
        {
          if (!parserRunCache.TryGetValue(c.ParserRunId, out var run))
          {
            run = _parserRunRepository.GetByIdAsync(c.ParserRunId, cancellationToken).GetAwaiter().GetResult();
            parserRunCache[c.ParserRunId] = run;
          }

          return new FragmentReviewSnapshotEntry(
            c.Id,
            c.ParserRunId,
            c.ImportedSourceId,
            run?.ParserKey ?? string.Empty,
            run?.ParserVersion ?? string.Empty,
            run?.ParserContractVersion ?? string.Empty,
            c.Locator.LocatorType,
            c.Locator.RawValue,
            c.Locator.NormalizedValue,
            c.Ordinal,
            c.ContentKind,
            c.TextLength,
            c.ConfidenceBand,
            c.IdentityKeyHash,
            c.ReviewState,
            c.ReviewedBy,
            c.ReviewedAtUtc,
            c.ReviewNotes,
            c.ExtractedText.Length > MaxTextPreviewLength
              ? string.Concat(c.ExtractedText.AsSpan(0, MaxTextPreviewLength), "...")
              : c.ExtractedText,
            c.CreatedAtUtc);
        })
        .ToArray();

      var totalMatchingCount = filtered.Count();

      _logger.LogInformation(LogEvents.UseCaseCompleted,
        "{UseCase} completed for project {ProjectId}, entries {EntryCount}",
        useCaseName, projectId, entries.Length);

      return new LoadFragmentReviewSnapshotResult(entries, totalMatchingCount);
    }
  }
}
