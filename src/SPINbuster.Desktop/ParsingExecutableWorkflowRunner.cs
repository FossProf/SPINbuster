using System.Text;
using SPINbuster.Application;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.AcceptFragmentCandidate;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadFragmentReviewSnapshot;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.RejectFragmentCandidate;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed class ParsingExecutableWorkflowRunner
{
  private const string ParserKey = "plain-text-deterministic";
  private const string StructuredParserKey = "structured-text-deterministic";
  private const string ParserContractVersion = "1.0.0";
  private const int ReviewSnapshotMaxResults = 100;

  private readonly ICommandHandler<AcceptFragmentCandidateCommand, AcceptFragmentCandidateResult> _acceptFragmentCandidate;
  private readonly ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> _beginImportSession;
  private readonly ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> _completeImportSession;
  private readonly ICommandHandler<CreateProjectCommand, CreateProjectResult> _createProject;
  private readonly ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> _importSource;
  private readonly ICommandHandler<RejectFragmentCandidateCommand, RejectFragmentCandidateResult> _rejectFragmentCandidate;
  private readonly ICommandHandler<RequestDocumentParsingCommand, RequestDocumentParsingResult> _requestDocumentParsing;
  private readonly IQueryHandler<LoadFragmentReviewSnapshotQuery, LoadFragmentReviewSnapshotResult> _loadFragmentReviewSnapshot;
  private readonly IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult> _loadParsingSnapshot;

  public ParsingExecutableWorkflowRunner(
    ICommandHandler<CreateProjectCommand, CreateProjectResult> createProject,
    ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> beginImportSession,
    ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> importSource,
    ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> completeImportSession,
    ICommandHandler<RequestDocumentParsingCommand, RequestDocumentParsingResult> requestDocumentParsing,
    IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult> loadParsingSnapshot,
    ICommandHandler<AcceptFragmentCandidateCommand, AcceptFragmentCandidateResult> acceptFragmentCandidate,
    ICommandHandler<RejectFragmentCandidateCommand, RejectFragmentCandidateResult> rejectFragmentCandidate,
    IQueryHandler<LoadFragmentReviewSnapshotQuery, LoadFragmentReviewSnapshotResult> loadFragmentReviewSnapshot)
  {
    _createProject = createProject;
    _beginImportSession = beginImportSession;
    _importSource = importSource;
    _completeImportSession = completeImportSession;
    _requestDocumentParsing = requestDocumentParsing;
    _loadParsingSnapshot = loadParsingSnapshot;
    _acceptFragmentCandidate = acceptFragmentCandidate;
    _rejectFragmentCandidate = rejectFragmentCandidate;
    _loadFragmentReviewSnapshot = loadFragmentReviewSnapshot;
  }

  public async Task<ParsingExecutableWorkflowResult> RunAsync(CancellationToken cancellationToken = default)
  {
    var runScope = WorkflowRunScope.Create();

    var createdProject = await _createProject.HandleAsync(
      new CreateProjectCommand(runScope.ProjectName),
      cancellationToken);

    var beganImportSession = await _beginImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(createdProject.ProjectId),
      cancellationToken);

    var sourceABytes = Encoding.UTF8.GetBytes(runScope.SourceAContent);
    await using var sourceAStream = new MemoryStream(sourceABytes, writable: false);
    var importedSourceA = await _importSource.HandleAsync(
      new ImportDocumentSourceCommand(
        beganImportSession.ImportSessionId,
        createdProject.ProjectId,
        runScope.SourceAFileName,
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        sourceAStream),
      cancellationToken);

    var sourceBBytes = Encoding.UTF8.GetBytes(runScope.SourceBContent);
    await using var sourceBStream = new MemoryStream(sourceBBytes, writable: false);
    var importedSourceB = await _importSource.HandleAsync(
      new ImportDocumentSourceCommand(
        beganImportSession.ImportSessionId,
        createdProject.ProjectId,
        runScope.SourceBFileName,
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        sourceBStream),
      cancellationToken);

    var structuredTextBytes = Encoding.UTF8.GetBytes(runScope.StructuredTextContent);
    await using var structuredTextStream = new MemoryStream(structuredTextBytes, writable: false);
    var structuredTextSource = await _importSource.HandleAsync(
      new ImportDocumentSourceCommand(
        beganImportSession.ImportSessionId,
        createdProject.ProjectId,
        runScope.StructuredTextFileName,
        "text/markdown",
        ImportedSourceOrigin.LocalFile,
        null,
        structuredTextStream),
      cancellationToken);

    var completedImportSession = await _completeImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(beganImportSession.ImportSessionId),
      cancellationToken);

    var firstParseResult = await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        createdProject.ProjectId,
        importedSourceA.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);

    var firstSnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      importedSourceA.ImportedSourceId,
      cancellationToken);

    var replayParseResult = await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        createdProject.ProjectId,
        importedSourceA.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);

    var replaySnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      importedSourceA.ImportedSourceId,
      cancellationToken);

    var sourceBParseResult = await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        createdProject.ProjectId,
        importedSourceB.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);

    var structuredTextParseResult = await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        createdProject.ProjectId,
        structuredTextSource.ImportedSourceId,
        StructuredParserKey,
        ParserContractVersion),
      cancellationToken);

    var structuredTextSnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      structuredTextSource.ImportedSourceId,
      cancellationToken);

    var candidatesToReview = firstSnapshot.ParserRuns
      .SelectMany(r => r.FragmentCandidates)
      .ToArray();

    var acceptedCandidate = await _acceptFragmentCandidate.HandleAsync(
      new AcceptFragmentCandidateCommand(
        candidatesToReview[0].FragmentCandidateId,
        "Deterministic fragment review acceptance."),
      cancellationToken);

    var rejectedCandidate = await _rejectFragmentCandidate.HandleAsync(
      new RejectFragmentCandidateCommand(
        candidatesToReview[^1].FragmentCandidateId,
        "Deterministic fragment review rejection."),
      cancellationToken);

    var reviewSnapshotAfterAccept = await LoadReviewSnapshotAsync(
      createdProject.ProjectId,
      FragmentCandidateReviewState.HumanAccepted,
      cancellationToken);

    var reviewSnapshotAfterReject = await LoadReviewSnapshotAsync(
      createdProject.ProjectId,
      FragmentCandidateReviewState.Rejected,
      cancellationToken);

    var unsupportedMediaResult = await RunUnsupportedMediaScenarioAsync(
      createdProject.ProjectId,
      runScope,
      cancellationToken);

    var cancelledParseResult = await RunCancelledParseScenarioAsync(
      createdProject.ProjectId,
      runScope,
      cancellationToken);

    var malformedOutputResult = await RunMalformedOutputScenarioAsync(
      createdProject.ProjectId,
      runScope,
      cancellationToken);

    var failurePresentations = await RunExpectedFailureScenariosAsync(
      createdProject.ProjectId,
      candidatesToReview,
      runScope,
      cancellationToken);

    var finalSnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      importedSourceA.ImportedSourceId,
      cancellationToken);

    return new ParsingExecutableWorkflowResult(
      createdProject,
      beganImportSession,
      importedSourceA,
      importedSourceB,
      structuredTextSource,
      completedImportSession,
      firstParseResult,
      firstSnapshot,
      replayParseResult,
      replaySnapshot,
      sourceBParseResult,
      structuredTextParseResult,
      structuredTextSnapshot,
      unsupportedMediaResult,
      cancelledParseResult,
      malformedOutputResult,
      acceptedCandidate,
      rejectedCandidate,
      reviewSnapshotAfterAccept,
      reviewSnapshotAfterReject,
      finalSnapshot,
      failurePresentations);
  }

  private async Task<IReadOnlyList<DesktopWorkflowFailurePresentation>> RunExpectedFailureScenariosAsync(
    ProjectId projectId,
    FragmentCandidateSnapshot[] candidatesToReview,
    WorkflowRunScope runScope,
    CancellationToken cancellationToken)
  {
    var failures = new List<DesktopWorkflowFailurePresentation>();

    failures.Add(await CaptureExpectedFailureAsync(
      "wrong-project-review",
      async () =>
      {
        var otherProject = await _createProject.HandleAsync(
          new CreateProjectCommand($"Other {runScope.Suffix}"),
          cancellationToken);
        var otherSession = await _beginImportSession.HandleAsync(
          new BeginDocumentImportSessionCommand(otherProject.ProjectId),
          cancellationToken);
        var otherBytes = Encoding.UTF8.GetBytes("other content");
        await using var otherStream = new MemoryStream(otherBytes, writable: false);
        var otherSource = await _importSource.HandleAsync(
          new ImportDocumentSourceCommand(
            otherSession.ImportSessionId,
            otherProject.ProjectId,
            "other.txt",
            "text/plain",
            ImportedSourceOrigin.LocalFile,
            null,
            otherStream),
          cancellationToken);
        await _completeImportSession.HandleAsync(
          new CompleteDocumentImportSessionCommand(otherSession.ImportSessionId),
          cancellationToken);
        await _requestDocumentParsing.HandleAsync(
          new RequestDocumentParsingCommand(
            otherProject.ProjectId,
            otherSource.ImportedSourceId,
            ParserKey,
            ParserContractVersion),
          cancellationToken);
        await _acceptFragmentCandidate.HandleAsync(
          new AcceptFragmentCandidateCommand(
            candidatesToReview[0].FragmentCandidateId,
            "Cross-project review."),
          cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "already-reviewed-candidate",
      async () =>
      {
        await _acceptFragmentCandidate.HandleAsync(
          new AcceptFragmentCandidateCommand(
            candidatesToReview[0].FragmentCandidateId,
            "Double accept."),
          cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "conflicting-accept-after-reject",
      async () =>
      {
        await _acceptFragmentCandidate.HandleAsync(
          new AcceptFragmentCandidateCommand(
            candidatesToReview[^1].FragmentCandidateId,
            "Conflict accept."),
          cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "missing-candidate",
      async () =>
      {
        await _acceptFragmentCandidate.HandleAsync(
          new AcceptFragmentCandidateCommand(
            FragmentCandidateId.New(),
            "Ghost candidate."),
          cancellationToken);
      }));

    return failures;
  }

  private async Task<RequestDocumentParsingResult> RunUnsupportedMediaScenarioAsync(
    ProjectId projectId,
    WorkflowRunScope runScope,
    CancellationToken cancellationToken)
  {
    var session = await _beginImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(projectId),
      cancellationToken);

    var imageBytes = Encoding.UTF8.GetBytes("fake pdf bytes");
    await using var imageStream = new MemoryStream(imageBytes, writable: false);
    var imageSource = await _importSource.HandleAsync(
      new ImportDocumentSourceCommand(
        session.ImportSessionId,
        projectId,
        runScope.UnsupportedMediaFileName,
        "application/pdf",
        ImportedSourceOrigin.LocalFile,
        null,
        imageStream),
      cancellationToken);

    await _completeImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(session.ImportSessionId),
      cancellationToken);

    return await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        projectId,
        imageSource.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);
  }

  private async Task<RequestDocumentParsingResult> RunCancelledParseScenarioAsync(
    ProjectId projectId,
    WorkflowRunScope runScope,
    CancellationToken cancellationToken)
  {
    var session = await _beginImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(projectId),
      cancellationToken);

    var cancelBytes = Encoding.UTF8.GetBytes(runScope.CancelContent);
    await using var cancelStream = new MemoryStream(cancelBytes, writable: false);
    var cancelSource = await _importSource.HandleAsync(
      new ImportDocumentSourceCommand(
        session.ImportSessionId,
        projectId,
        runScope.CancelFileName,
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        cancelStream),
      cancellationToken);

    await _completeImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(session.ImportSessionId),
      cancellationToken);

    using var cts = new CancellationTokenSource();
    await cts.CancelAsync();

    try
    {
      return await _requestDocumentParsing.HandleAsync(
        new RequestDocumentParsingCommand(
          projectId,
          cancelSource.ImportedSourceId,
          ParserKey,
          ParserContractVersion),
        cts.Token);
    }
    catch (OperationCanceledException)
    {
      return new RequestDocumentParsingResult(
        ParserRunId.New(),
        ParserRunState.Cancelled,
        ParserRunFailureClassification.Cancelled,
        "Parser run was cancelled.",
        []);
    }
  }

  private async Task<RequestDocumentParsingResult> RunMalformedOutputScenarioAsync(
    ProjectId projectId,
    WorkflowRunScope runScope,
    CancellationToken cancellationToken)
  {
    var session = await _beginImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(projectId),
      cancellationToken);

    var whitespaceBytes = Encoding.UTF8.GetBytes("   \n\r\n  ");
    await using var whitespaceStream = new MemoryStream(whitespaceBytes, writable: false);
    var whitespaceSource = await _importSource.HandleAsync(
      new ImportDocumentSourceCommand(
        session.ImportSessionId,
        projectId,
        runScope.MalformedFileName,
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        whitespaceStream),
      cancellationToken);

    await _completeImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(session.ImportSessionId),
      cancellationToken);

    return await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        projectId,
        whitespaceSource.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);
  }

  private async Task<LoadParsingSnapshotResult> LoadParsingSnapshotAsync(
    ProjectId projectId,
    ImportedSourceId importedSourceId,
    CancellationToken cancellationToken)
  {
    return await _loadParsingSnapshot.HandleAsync(
      new LoadParsingSnapshotQuery(projectId, importedSourceId),
      cancellationToken);
  }

  private async Task<LoadFragmentReviewSnapshotResult> LoadReviewSnapshotAsync(
    ProjectId projectId,
    FragmentCandidateReviewState reviewState,
    CancellationToken cancellationToken)
  {
    return await _loadFragmentReviewSnapshot.HandleAsync(
      new LoadFragmentReviewSnapshotQuery(
        projectId,
        reviewState,
        null,
        null,
        null,
        ReviewSnapshotMaxResults),
      cancellationToken);
  }

  private static async Task<DesktopWorkflowFailurePresentation> CaptureExpectedFailureAsync(
    string scenario,
    Func<Task> operation)
  {
    try
    {
      await operation();
    }
    catch (Exception exception) when (
      exception is DomainInvariantException
      || exception is LifecycleTransitionException
      || exception is InvalidOperationException
      || exception is ApplicationEntityNotFoundException)
    {
      return new DesktopWorkflowFailurePresentation(scenario, exception.GetType().Name, exception.Message);

    }

    throw new InvalidOperationException($"Expected failure scenario '{scenario}' completed successfully.");
  }

  private sealed record WorkflowRunScope(
    string Suffix,
    string ProjectName,
    string SourceAFileName,
    string SourceAContent,
    string SourceBFileName,
    string SourceBContent,
    string StructuredTextFileName,
    string StructuredTextContent,
    string UnsupportedMediaFileName,
    string CancelFileName,
    string CancelContent,
    string MalformedFileName)
  {
    public static WorkflowRunScope Create()
    {
      var suffix = Guid.NewGuid().ToString("N")[..8];
      return new WorkflowRunScope(
        suffix,
        $"Parsing Proof {suffix}",
        $"specification-{suffix}.txt",
        $"run-scope={suffix}\nSection 03 30 00 requires concrete curing in accordance with the approved project specifications.\n\nField observation: concrete placement was completed and wet curing was initiated.\n\nProvide curing protection immediately after finishing.",
        $"field-observation-{suffix}.txt",
        $"run-scope={suffix}\nInspection of concrete placement on level 3 confirmed curing was initiated within specified time limits.\n\nMoisture retention blankets were placed as required.",
        $"specification-{suffix}.md",
        $"# Section 03 30 00 - Cast-in-Place Concrete\n\n## 1. General\n\n1.1 This section covers cast-in-place concrete work and includes the following table:\n| Material | Standard |\n| --- | --- |\n| Cement | ASTM C150 |\n| Aggregate | ASTM C33 |\n\n1.2 All concrete shall comply with ASTM C150.\n\n## 2. Materials\n\n| Material | Standard |\n| --- | --- |\n| Cement | ASTM C150 |\n| Aggregate | ASTM C33 |",
        $"unsupported-{suffix}.pdf",
        $"cancel-{suffix}.txt",
        $"run-scope={suffix}\nThis content will be parsed before cancellation.",
        $"whitespace-{suffix}.txt");
    }
  }
}
