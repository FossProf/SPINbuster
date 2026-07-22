using Microsoft.Extensions.Logging;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadParsingSnapshot;

public sealed class LoadParsingSnapshotUseCase : IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult>
{
  private const int MaxParserRuns = 100;
  private const int MaxFragmentCandidates = 10_000;
  private const int MaxAuditEvents = 500;

  private readonly IFragmentCandidateRepository _fragmentCandidateRepository;
  private readonly IImportedDocumentSourceRepository _importedSourceRepository;
  private readonly ILogger<LoadParsingSnapshotUseCase> _logger;
  private readonly IParserDiagnosticRepository _parserDiagnosticRepository;
  private readonly IParserRunRepository _parserRunRepository;
  private readonly IProjectRepository _projectRepository;

  public LoadParsingSnapshotUseCase(
    IProjectRepository projectRepository,
    IImportedDocumentSourceRepository importedSourceRepository,
    IParserRunRepository parserRunRepository,
    IFragmentCandidateRepository fragmentCandidateRepository,
    IParserDiagnosticRepository parserDiagnosticRepository,
    ILogger<LoadParsingSnapshotUseCase> logger)
  {
    _projectRepository = projectRepository;
    _importedSourceRepository = importedSourceRepository;
    _parserRunRepository = parserRunRepository;
    _fragmentCandidateRepository = fragmentCandidateRepository;
    _parserDiagnosticRepository = parserDiagnosticRepository;
    _logger = logger;
  }

  public async Task<LoadParsingSnapshotResult> HandleAsync(LoadParsingSnapshotQuery query, CancellationToken cancellationToken = default)
  {
    var useCaseName = nameof(LoadParsingSnapshotUseCase);
    var importedSourceId = query.ImportedSourceId.ToString();
    var projectId = query.ProjectId.ToString();

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.ImportedSourceId] = importedSourceId,
      [LogProperties.ProjectId] = projectId,
    }))
    {
      _logger.LogInformation(LogEvents.UseCaseStarting,
        "{UseCase} starting for imported source {ImportedSourceId}, project {ProjectId}",
        useCaseName, importedSourceId, projectId);

      var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(Project), query.ProjectId.ToString());

      var source = await _importedSourceRepository.GetByIdAsync(query.ImportedSourceId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(ImportedDocumentSource), query.ImportedSourceId.ToString());

      if (source.ProjectId != query.ProjectId)
      {
        throw new DomainInvariantException("Imported source does not belong to the specified project.");
      }

      var runs = await _parserRunRepository.GetByImportedSourceAsync(query.ImportedSourceId, MaxParserRuns, cancellationToken);
      var parserRunSnapshots = new List<ParserRunSnapshot>();

      foreach (var run in runs)
      {
        var candidates = await _fragmentCandidateRepository.GetByParserRunAsync(run.Id, MaxFragmentCandidates, cancellationToken);
        var candidateSnapshots = candidates.Select(c => new FragmentCandidateSnapshot(
          c.Id,
          c.Locator.LocatorType,
          c.Locator.RawValue,
          c.Locator.NormalizedValue,
          c.Ordinal,
          c.ContentKind,
          c.TextLength,
          c.ConfidenceBand,
          c.IdentityKeyHash,
          c.CreatedAtUtc)).ToArray();

        var runDiagnostics = await _parserDiagnosticRepository.GetByParserRunAsync(run.Id, cancellationToken);
        var diagnosticSnapshots = runDiagnostics.Select(d => new ParserDiagnosticSnapshot(
          d.Id,
          d.Severity,
          d.Code,
          d.Message,
          d.CandidateRefType,
          d.CandidateRefValue,
          d.LocatorType,
          d.LocatorValue,
          d.CreatedAtUtc)).ToArray();

        var auditSnapshots = run.AuditTrail
          .Take(MaxAuditEvents)
          .Select(a => new AuditEventSnapshot(
            a.EventType,
            a.Actor,
            a.OccurredAtUtc,
            a.Description)).ToArray();

        parserRunSnapshots.Add(new ParserRunSnapshot(
          run.Id,
          run.ParserKey,
          run.ParserVersion,
          run.ParserContractVersion,
          run.ParserContractHash,
          run.State,
          run.ExecutionStatus,
          run.FailureReason,
          run.CreatedAtUtc,
          run.StartedAtUtc,
          run.CompletedAtUtc,
          candidateSnapshots,
          diagnosticSnapshots,
          auditSnapshots));
      }

      _logger.LogInformation(LogEvents.UseCaseCompleted,
        "{UseCase} completed for imported source {ImportedSourceId}, parser runs {ParserRunCount}",
        useCaseName, importedSourceId, parserRunSnapshots.Count);

      return new LoadParsingSnapshotResult(
        source.Id,
        source.ProjectId,
        source.ContentHash,
        source.HashAlgorithm,
        source.HashAlgorithmVersion,
        source.ContentLength,
        parserRunSnapshots);
    }
  }
}
