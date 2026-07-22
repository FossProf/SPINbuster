using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.Logging;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.RequestDocumentParsing;

public sealed class RequestDocumentParsingUseCase : ICommandHandler<RequestDocumentParsingCommand, RequestDocumentParsingResult>
{
  private const int MaxFragmentCandidates = 10_000;
  private const int MaxDiagnostics = 100;

  private readonly IAuditRecorder _auditRecorder;
  private readonly IClock _clock;
  private readonly ICurrentUser _currentUser;
  private readonly IDocumentParserRegistry _parserRegistry;
  private readonly IFragmentCandidateRepository _fragmentCandidateRepository;
  private readonly IImmutableContentStore _immutableContentStore;
  private readonly IImportedDocumentSourceRepository _importedSourceRepository;
  private readonly ILogger<RequestDocumentParsingUseCase> _logger;
  private readonly IParserDiagnosticRepository _parserDiagnosticRepository;
  private readonly IParserRunRepository _parserRunRepository;
  private readonly IProjectRepository _projectRepository;
  private readonly IUnitOfWork _unitOfWork;

  public RequestDocumentParsingUseCase(
    IProjectRepository projectRepository,
    IImportedDocumentSourceRepository importedSourceRepository,
    IImmutableContentStore immutableContentStore,
    IDocumentParserRegistry parserRegistry,
    IParserRunRepository parserRunRepository,
    IFragmentCandidateRepository fragmentCandidateRepository,
    IParserDiagnosticRepository parserDiagnosticRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IAuditRecorder auditRecorder,
    ILogger<RequestDocumentParsingUseCase> logger)
  {
    _projectRepository = projectRepository;
    _importedSourceRepository = importedSourceRepository;
    _immutableContentStore = immutableContentStore;
    _parserRegistry = parserRegistry;
    _parserRunRepository = parserRunRepository;
    _fragmentCandidateRepository = fragmentCandidateRepository;
    _parserDiagnosticRepository = parserDiagnosticRepository;
    _unitOfWork = unitOfWork;
    _clock = clock;
    _currentUser = currentUser;
    _auditRecorder = auditRecorder;
    _logger = logger;
  }

  public async Task<RequestDocumentParsingResult> HandleAsync(RequestDocumentParsingCommand command, CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();
    var useCaseName = nameof(RequestDocumentParsingUseCase);
    var importedSourceId = command.ImportedSourceId.ToString();
    var projectId = command.ProjectId.ToString();

    using (_logger.BeginScope(new Dictionary<string, object>
    {
      [LogProperties.UseCase] = useCaseName,
      [LogProperties.ImportedSourceId] = importedSourceId,
      [LogProperties.ProjectId] = projectId,
      [LogProperties.ParserKey] = command.ParserKey,
    }))
    {
      _logger.LogInformation(LogEvents.ParserRunStarting,
        "{UseCase} starting for imported source {ImportedSourceId}, project {ProjectId}, parser {ParserKey}",
        useCaseName, importedSourceId, projectId, command.ParserKey);

      var project = await _projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(Project), command.ProjectId.ToString());

      var source = await _importedSourceRepository.GetByIdAsync(command.ImportedSourceId, cancellationToken)
        ?? throw new ApplicationEntityNotFoundException(nameof(ImportedDocumentSource), command.ImportedSourceId.ToString());

      if (source.ProjectId != command.ProjectId)
      {
        throw new DomainInvariantException("Imported source does not belong to the specified project.");
      }

      var documentParser = _parserRegistry.GetRequired(command.ParserKey);
      var descriptor = documentParser.Describe();

      if (descriptor.Determinism != ParserDeterminism.Deterministic)
      {
        throw new DomainInvariantException($"Parser '{command.ParserKey}' is non-deterministic. Only deterministic parsers are supported.");
      }

      if (!string.Equals(command.ParserKey, descriptor.ParserKey, StringComparison.Ordinal))
      {
        throw new DomainInvariantException($"Parser key '{command.ParserKey}' does not match the resolved parser identity '{descriptor.ParserKey}'.");
      }

      if (!string.Equals(command.ParserContractVersion, descriptor.ContractVersion, StringComparison.Ordinal))
      {
        throw new DomainInvariantException($"Parser contract version '{command.ParserContractVersion}' does not match the resolved parser contract version '{descriptor.ContractVersion}'.");
      }

      var existingRun = await _parserRunRepository.GetBySourceAndParserAsync(
        command.ImportedSourceId, descriptor.ParserKey, descriptor.ParserVersion, descriptor.ContractVersion, descriptor.ContractHash, cancellationToken);
      if (existingRun is not null)
      {
        stopwatch.Stop();
        _logger.LogInformation(LogEvents.ParserRunCompleted,
          "{UseCase} idempotent replay in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}, state {State}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, existingRun.Id, existingRun.State);

        var replayCandidateIds = existingRun.State is ParserRunState.Completed
          ? (await _fragmentCandidateRepository.GetByParserRunAsync(existingRun.Id, MaxFragmentCandidates, cancellationToken))
              .Select(c => c.Id).ToArray()
          : Array.Empty<FragmentCandidateId>();

        return new RequestDocumentParsingResult(
          existingRun.Id,
          existingRun.State,
          MapFailureClassification(existingRun.FailureReason),
          existingRun.FailureReason,
          replayCandidateIds);
      }

      var parserRun = CreateParserRun(command, source, descriptor);
      await _parserRunRepository.AddAsync(parserRun, cancellationToken);
      Internal.DocumentAuditStager.Stage(_auditRecorder, parserRun.AuditTrail);
      await _unitOfWork.CommitAsync(cancellationToken);

      OpenImmutableContentResult openResult;
      try
      {
        openResult = await _immutableContentStore.OpenReadAsync(source.StorageReference.StorageObjectId, cancellationToken);
      }
      catch (Exception)
      {
        parserRun.Fail(_clock.UtcNow, "Source content is not currently available.");
        await PersistTerminalRunStateAsync(parserRun, [], [], CancellationToken.None);
        stopwatch.Stop();
        _logger.LogWarning(LogEvents.ParserRunFailed,
          "{UseCase} source open failure in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id);
        return CreateResult(parserRun, []);
      }

      if (openResult.AvailabilityState != StorageAvailabilityState.Available)
      {
        parserRun.Fail(_clock.UtcNow, "Source content is not currently available.");
        await PersistTerminalRunStateAsync(parserRun, [], [], CancellationToken.None);
        stopwatch.Stop();
        _logger.LogWarning(LogEvents.ParserRunFailed,
          "{UseCase} source unavailable in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id);
        return CreateResult(parserRun, []);
      }

      if (!string.Equals(openResult.ContentHash, source.ContentHash, StringComparison.Ordinal)
        || !string.Equals(openResult.HashAlgorithm, source.HashAlgorithm, StringComparison.Ordinal)
        || openResult.HashAlgorithmVersion != source.HashAlgorithmVersion
        || openResult.ContentLength != source.ContentLength)
      {
        parserRun.Fail(_clock.UtcNow, "Stored content no longer matches the authoritative immutable identity.");
        await PersistTerminalRunStateAsync(parserRun, [], [], CancellationToken.None);
        stopwatch.Stop();
        _logger.LogWarning(LogEvents.ParserRunFailed,
          "{UseCase} integrity mismatch in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id);
        return CreateResult(parserRun, []);
      }

      try
      {
        await using var content = openResult.Content;
        parserRun.Start(_clock.UtcNow);

        var parserResult = await documentParser.ParseAsync(new ParserInput(
          source.Id,
          source.ProjectId,
          source.OriginalFileName,
          source.DeclaredMediaType,
          source.DetectedMediaType,
          source.ContentHash,
          source.HashAlgorithm,
          source.HashAlgorithmVersion,
          source.ContentLength,
          content), cancellationToken);

        var createdCandidates = new List<FragmentCandidate>();
        var createdDiagnostics = new List<ParserDiagnostic>();

        if (parserResult.Status == ParserExecutionStatus.Failed)
        {
          if (parserResult.FailureClassification == ParserRunFailureClassification.Cancelled)
          {
            parserRun.Cancel(_clock.UtcNow, parserResult.FailureDetails ?? "Parser run was cancelled.");
          }
          else
          {
            parserRun.Fail(_clock.UtcNow, parserResult.FailureDetails ?? "Parser run failed.");
          }
        }
        else
        {
          foreach (var fragment in parserResult.Fragments.Take(MaxFragmentCandidates))
          {
            var locator = new FragmentLocator(fragment.LocatorType, fragment.LocatorValue);
            var candidate = new FragmentCandidate(
              FragmentCandidateId.New(),
              parserRun.Id,
              source.ProjectId,
              source.Id,
              source.ContentHash,
              locator,
              fragment.Ordinal,
              fragment.ContentKind,
              fragment.ExtractedText,
              fragment.ConfidenceBand,
              command.ParserKey,
              command.ParserContractVersion,
              _clock.UtcNow);
            createdCandidates.Add(candidate);
          }

          // Map parser diagnostics to durable ParserDiagnostic entities.
          // Resolve candidate references by ordinal lookup within the same run's output.
          var candidateByOrdinal = createdCandidates.ToDictionary(c => c.Ordinal);
          var diagnosticCount = 0;

          foreach (var diagnostic in parserResult.Diagnostics)
          {
            if (diagnosticCount >= MaxDiagnostics)
            {
              break;
            }

            // Drop diagnostics with Error severity on successful results —
            // Error is reserved for Failed status per the severity contract.
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
              continue;
            }

            string? resolvedRefValue = null;
            DiagnosticRefType? resolvedRefType = null;
            FragmentLocatorType? resolvedLocatorType = null;
            string? resolvedLocatorValue = null;

            if (diagnostic.CandidateRefType.HasValue && !string.IsNullOrWhiteSpace(diagnostic.CandidateRefValue))
            {
              FragmentCandidate? matchedCandidate = null;

              if (diagnostic.CandidateRefType == DiagnosticRefType.Ordinal
                && int.TryParse(diagnostic.CandidateRefValue, out var refOrdinal)
                && candidateByOrdinal.TryGetValue(refOrdinal, out matchedCandidate))
              {
                resolvedRefType = DiagnosticRefType.Ordinal;
                resolvedRefValue = matchedCandidate.IdentityKey;
              }
              else if (diagnostic.CandidateRefType == DiagnosticRefType.NormalizedLocator)
              {
                matchedCandidate = createdCandidates.FirstOrDefault(c =>
                  string.Equals(c.Locator.NormalizedValue, diagnostic.CandidateRefValue, StringComparison.Ordinal));

                if (matchedCandidate is not null)
                {
                  resolvedRefType = DiagnosticRefType.NormalizedLocator;
                  resolvedRefValue = matchedCandidate.IdentityKey;
                }
              }

              // If no candidate matched, drop the diagnostic — do not persist orphaned references.
              if (matchedCandidate is null)
              {
                _logger.LogDebug(
                  "{UseCase} dropping diagnostic {DiagnosticCode} with unresolved candidate reference {RefType}:{RefValue}",
                  useCaseName, diagnostic.Code, diagnostic.CandidateRefType, diagnostic.CandidateRefValue);
                continue;
              }
            }

            if (diagnostic.LocatorType.HasValue && !string.IsNullOrWhiteSpace(diagnostic.LocatorValue))
            {
              resolvedLocatorType = diagnostic.LocatorType;
              resolvedLocatorValue = diagnostic.LocatorValue;
            }

            createdDiagnostics.Add(new ParserDiagnostic(
              ParserDiagnosticId.New(),
              parserRun.Id,
              diagnostic.Severity,
              diagnostic.Code,
              diagnostic.Message,
              _clock.UtcNow,
              resolvedRefType,
              resolvedRefValue,
              resolvedLocatorType,
              resolvedLocatorValue));

            diagnosticCount++;
          }

          parserRun.Complete(_clock.UtcNow);
        }

        foreach (var candidate in createdCandidates)
        {
          await _fragmentCandidateRepository.AddAsync(candidate, cancellationToken);
        }

        if (createdDiagnostics.Count > 0)
        {
          await _parserDiagnosticRepository.AddRangeAsync(createdDiagnostics, cancellationToken);
        }

        await _parserRunRepository.UpdateAsync(parserRun, cancellationToken);

        foreach (var candidate in createdCandidates)
        {
          Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail);
        }

        Internal.DocumentAuditStager.Stage(_auditRecorder, parserRun.AuditTrail.Skip(1));
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();
        if (parserRun.State is ParserRunState.Failed)
        {
          _logger.LogWarning(LogEvents.ParserRunFailed,
            "{UseCase} failed in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}, failure {FailureReason}",
            useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id, parserRun.FailureReason);
        }
        else if (parserRun.State is ParserRunState.Cancelled)
        {
          _logger.LogWarning(LogEvents.ParserRunCancelled,
            "{UseCase} cancelled in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}",
            useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id);
        }
        else
        {
          var warningCount = createdDiagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
          _logger.LogInformation(LogEvents.ParserRunCompleted,
            "{UseCase} completed in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}, state {ParserRunState}, candidates {CandidateCount}, diagnostics {DiagnosticCount}, warnings {WarningCount}",
            useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id, parserRun.State, createdCandidates.Count, createdDiagnostics.Count, warningCount);
        }

        return CreateResult(parserRun, createdCandidates);
      }
      catch (OperationCanceledException)
      {
        parserRun.Cancel(_clock.UtcNow, "Parser run was cancelled.");
        await PersistTerminalRunStateAsync(parserRun, [], [], CancellationToken.None);
        stopwatch.Stop();
        _logger.LogWarning(LogEvents.ParserRunCancelled,
          "{UseCase} cancelled in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id);
        return CreateResult(parserRun, []);
      }
      catch (Exception exception)
      {
        if (parserRun.State is not ParserRunState.Completed
          and not ParserRunState.Failed
          and not ParserRunState.Cancelled)
        {
          parserRun.Fail(_clock.UtcNow, $"Unexpected parser failure: {exception.Message}");
        }

        await PersistTerminalRunStateAsync(parserRun, [], [], CancellationToken.None);
        stopwatch.Stop();
        _logger.LogError(LogEvents.ParserRunFailed,
          exception,
          "{UseCase} unexpected failure in {DurationMs}ms for imported source {ImportedSourceId}, parser run {ParserRunId}",
          useCaseName, stopwatch.ElapsedMilliseconds, importedSourceId, parserRun.Id);
        return CreateResult(parserRun, []);
      }
    }
  }

  private ParserRun CreateParserRun(
    RequestDocumentParsingCommand command,
    ImportedDocumentSource source,
    ParserDescriptor descriptor)
  {
    var run = new ParserRun(
      ParserRunId.New(),
      command.ProjectId,
      command.ImportedSourceId,
      command.ParserKey,
      descriptor.ParserVersion,
      command.ParserContractVersion,
      descriptor.ContractHash,
      source.ContentHash,
      source.HashAlgorithm,
      source.HashAlgorithmVersion,
      _currentUser.UserId.Value,
      _clock.UtcNow);

    return run;
  }

  private async Task PersistTerminalRunStateAsync(
    ParserRun parserRun,
    List<FragmentCandidate> createdCandidates,
    List<ParserDiagnostic> createdDiagnostics,
    CancellationToken cancellationToken)
  {
    foreach (var candidate in createdCandidates)
    {
      await _fragmentCandidateRepository.AddAsync(candidate, cancellationToken);
    }

    if (createdDiagnostics.Count > 0)
    {
      await _parserDiagnosticRepository.AddRangeAsync(createdDiagnostics, cancellationToken);
    }

    await _parserRunRepository.UpdateAsync(parserRun, cancellationToken);

    foreach (var candidate in createdCandidates)
    {
      Internal.DocumentAuditStager.Stage(_auditRecorder, candidate.AuditTrail);
    }

    Internal.DocumentAuditStager.Stage(_auditRecorder, parserRun.AuditTrail.Skip(1));
    await _unitOfWork.CommitAsync(cancellationToken);
  }

  private static ParserRunFailureClassification MapFailureClassification(string? failureReason)
  {
    if (string.IsNullOrWhiteSpace(failureReason))
    {
      return ParserRunFailureClassification.None;
    }

    return failureReason.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
      ? ParserRunFailureClassification.Cancelled
      : ParserRunFailureClassification.ParserFailure;
  }

  private static RequestDocumentParsingResult CreateResult(
    ParserRun parserRun,
    List<FragmentCandidate> createdCandidates)
  {
    return new RequestDocumentParsingResult(
      parserRun.Id,
      parserRun.State,
      MapFailureClassification(parserRun.FailureReason),
      parserRun.FailureReason,
      createdCandidates.Select(c => c.Id).ToArray());
  }
}
