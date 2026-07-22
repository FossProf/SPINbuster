using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeParserRunRepository : IParserRunRepository
{
  private readonly Dictionary<ParserRunId, ParserRun> _runs = [];

  public Task<ParserRun?> GetByIdAsync(ParserRunId parserRunId, CancellationToken cancellationToken = default)
  {
    _runs.TryGetValue(parserRunId, out var run);
    return Task.FromResult(run);
  }

  public Task<ParserRun?> GetBySourceAndParserAsync(
    ImportedSourceId importedSourceId,
    string parserKey,
    string parserVersion,
    string contractVersion,
    string contractHash,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_runs.Values.SingleOrDefault(r =>
      r.ImportedSourceId == importedSourceId
      && r.ParserKey == parserKey
      && r.ParserContractVersion == contractVersion));
  }

  public Task<IReadOnlyCollection<ParserRun>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<ParserRun>>(
      _runs.Values
        .Where(r => r.ProjectId == projectId)
        .OrderBy(r => r.CreatedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public Task<IReadOnlyCollection<ParserRun>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<ParserRun>>(
      _runs.Values
        .Where(r => r.ImportedSourceId == importedSourceId)
        .OrderBy(r => r.CreatedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public List<ParserRun> AddedRuns { get; } = [];

  public List<ParserRun> UpdatedRuns { get; } = [];

  public Task AddAsync(ParserRun parserRun, CancellationToken cancellationToken = default)
  {
    _runs[parserRun.Id] = parserRun;
    AddedRuns.Add(parserRun);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(ParserRun parserRun, CancellationToken cancellationToken = default)
  {
    _runs[parserRun.Id] = parserRun;
    UpdatedRuns.Add(parserRun);
    return Task.CompletedTask;
  }
}

internal sealed class FakeFragmentCandidateRepository : IFragmentCandidateRepository
{
  private readonly Dictionary<FragmentCandidateId, FragmentCandidate> _candidates = [];

  public Task<FragmentCandidate?> GetByIdAsync(FragmentCandidateId fragmentCandidateId, CancellationToken cancellationToken = default)
  {
    _candidates.TryGetValue(fragmentCandidateId, out var candidate);
    return Task.FromResult(candidate);
  }

  public Task<IReadOnlyCollection<FragmentCandidate>> GetByParserRunAsync(
    ParserRunId parserRunId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<FragmentCandidate>>(
      _candidates.Values
        .Where(c => c.ParserRunId == parserRunId)
        .OrderBy(c => c.Ordinal)
        .Take(maxResults)
        .ToArray());
  }

  public Task<IReadOnlyCollection<FragmentCandidate>> GetByImportedSourceAsync(
    ImportedSourceId importedSourceId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<FragmentCandidate>>(
      _candidates.Values
        .Where(c => c.ImportedSourceId == importedSourceId)
        .OrderBy(c => c.CreatedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public Task<IReadOnlyCollection<FragmentCandidate>> GetByProjectAsync(
    ProjectId projectId,
    int maxResults,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<FragmentCandidate>>(
      _candidates.Values
        .Where(c => c.ProjectId == projectId)
        .OrderBy(c => c.CreatedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public Task<IReadOnlyCollection<FragmentCandidate>> GetByProjectFilteredAsync(
    ProjectId projectId,
    int maxResults,
    FragmentCandidateReviewState? reviewStateFilter,
    CancellationToken cancellationToken = default)
  {
    var query = _candidates.Values.Where(c => c.ProjectId == projectId);
    if (reviewStateFilter.HasValue)
    {
      query = query.Where(c => c.ReviewState == reviewStateFilter.Value);
    }

    return Task.FromResult<IReadOnlyCollection<FragmentCandidate>>(
      query
        .OrderBy(c => c.CreatedAtUtc)
        .Take(maxResults)
        .ToArray());
  }

  public List<FragmentCandidate> AddedCandidates { get; } = [];

  public List<FragmentCandidate> UpdatedCandidates { get; } = [];

  public Task AddAsync(FragmentCandidate fragmentCandidate, CancellationToken cancellationToken = default)
  {
    _candidates[fragmentCandidate.Id] = fragmentCandidate;
    AddedCandidates.Add(fragmentCandidate);
    return Task.CompletedTask;
  }

  public Task UpdateAsync(FragmentCandidate fragmentCandidate, CancellationToken cancellationToken = default)
  {
    _candidates[fragmentCandidate.Id] = fragmentCandidate;
    UpdatedCandidates.Add(fragmentCandidate);
    return Task.CompletedTask;
  }
}

internal sealed class FakeDocumentParser : IDocumentParser
{
  private readonly List<string>? _sharedOperationLog;

  public FakeDocumentParser(List<string>? sharedOperationLog = null)
  {
    _sharedOperationLog = sharedOperationLog;
  }

  public List<string> SequenceLog { get; } = [];

  public Func<ParserInput, CancellationToken, Task<ParserExecutionResult>> ParseAsyncCore { get; set; } =
    (input, _) =>
    {
      return Task.FromResult(new ParserExecutionResult(
        true,
        ParserRunFailureClassification.None,
        null,
        [
          new ParserFragmentResult(
            FragmentLocatorType.WholeDocument,
            "*",
            1,
            ContentKind.PlainText,
            "Parsed text content.",
            ConfidenceBand.High,
            []),
        ]));
    };

  public ParserDeterminism Determinism { get; set; } = ParserDeterminism.Deterministic;

  public ParserDescriptor Describe()
  {
    return new ParserDescriptor(
      "test-parser",
      "1.0.0",
      "1.0.0",
      "contract-hash-sha256-abcdef",
      ["application/pdf", "text/plain"],
      ContentKind.PlainText,
      Determinism);
  }

  public Task<ParserExecutionResult> ParseAsync(ParserInput input, CancellationToken cancellationToken = default)
  {
    SequenceLog.Add("parser-run");
    _sharedOperationLog?.Add("parser-run");
    return ParseAsyncCore(input, cancellationToken);
  }
}
