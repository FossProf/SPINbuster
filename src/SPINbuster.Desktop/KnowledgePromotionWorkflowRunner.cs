using System.Text;
using SPINbuster.Application;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.ActivateProject;
using SPINbuster.Application.UseCases.AcceptFragmentCandidate;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadFragmentReviewSnapshot;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;
using SPINbuster.Application.UseCases.LoadPromotionDiagnostic;
using SPINbuster.Application.UseCases.PromoteFragmentCandidate;
using SPINbuster.Application.UseCases.RejectFragmentCandidate;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed class KnowledgePromotionWorkflowRunner
{
  private const string ParserKey = "plain-text-deterministic";
  private const string ParserContractVersion = "1.0.0";
  private const int ReviewSnapshotMaxResults = 100;

  private readonly ICommandHandler<ActivateProjectCommand, ActivateProjectResult> _activateProject;
  private readonly ICommandHandler<AcceptFragmentCandidateCommand, AcceptFragmentCandidateResult> _acceptFragmentCandidate;
  private readonly ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> _beginImportSession;
  private readonly ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> _completeImportSession;
  private readonly ICommandHandler<CreateProjectCommand, CreateProjectResult> _createProject;
  private readonly ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> _importSource;
  private readonly IQueryHandler<LoadPromotionDiagnosticQuery, LoadPromotionDiagnosticResult> _loadPromotionDiagnostic;
  private readonly IQueryHandler<LoadFragmentReviewSnapshotQuery, LoadFragmentReviewSnapshotResult> _loadFragmentReviewSnapshot;
  private readonly IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult> _loadParsingSnapshot;
  private readonly IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult> _loadProjectKnowledgeSnapshot;
  private readonly ICommandHandler<PromoteFragmentCandidateCommand, PromoteFragmentCandidateResult> _promoteFragmentCandidate;
  private readonly ICommandHandler<RejectFragmentCandidateCommand, RejectFragmentCandidateResult> _rejectFragmentCandidate;
  private readonly ICommandHandler<RequestDocumentParsingCommand, RequestDocumentParsingResult> _requestDocumentParsing;

  public KnowledgePromotionWorkflowRunner(
    ICommandHandler<CreateProjectCommand, CreateProjectResult> createProject,
    ICommandHandler<ActivateProjectCommand, ActivateProjectResult> activateProject,
    ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> beginImportSession,
    ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> importSource,
    ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> completeImportSession,
    ICommandHandler<RequestDocumentParsingCommand, RequestDocumentParsingResult> requestDocumentParsing,
    IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult> loadParsingSnapshot,
    ICommandHandler<AcceptFragmentCandidateCommand, AcceptFragmentCandidateResult> acceptFragmentCandidate,
    ICommandHandler<RejectFragmentCandidateCommand, RejectFragmentCandidateResult> rejectFragmentCandidate,
    IQueryHandler<LoadFragmentReviewSnapshotQuery, LoadFragmentReviewSnapshotResult> loadFragmentReviewSnapshot,
    ICommandHandler<PromoteFragmentCandidateCommand, PromoteFragmentCandidateResult> promoteFragmentCandidate,
    IQueryHandler<LoadPromotionDiagnosticQuery, LoadPromotionDiagnosticResult> loadPromotionDiagnostic,
    IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult> loadProjectKnowledgeSnapshot)
  {
    _createProject = createProject;
    _activateProject = activateProject;
    _beginImportSession = beginImportSession;
    _importSource = importSource;
    _completeImportSession = completeImportSession;
    _requestDocumentParsing = requestDocumentParsing;
    _loadParsingSnapshot = loadParsingSnapshot;
    _acceptFragmentCandidate = acceptFragmentCandidate;
    _rejectFragmentCandidate = rejectFragmentCandidate;
    _loadFragmentReviewSnapshot = loadFragmentReviewSnapshot;
    _promoteFragmentCandidate = promoteFragmentCandidate;
    _loadPromotionDiagnostic = loadPromotionDiagnostic;
    _loadProjectKnowledgeSnapshot = loadProjectKnowledgeSnapshot;
  }

  public async Task<KnowledgePromotionWorkflowResult> RunAsync(CancellationToken cancellationToken = default)
  {
    var runScope = WorkflowRunScope.Create();

    var createdProject = await _createProject.HandleAsync(
      new CreateProjectCommand(runScope.ProjectName),
      cancellationToken);

    var activatedProject = await _activateProject.HandleAsync(
      new ActivateProjectCommand(createdProject.ProjectId),
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

    var candidatesToReview = firstSnapshot.ParserRuns
      .SelectMany(r => r.FragmentCandidates)
      .ToArray();

    var acceptedCandidateA = await _acceptFragmentCandidate.HandleAsync(
      new AcceptFragmentCandidateCommand(
        candidatesToReview[0].FragmentCandidateId,
        "Deterministic promotion review acceptance."),
      cancellationToken);

    var rejectedCandidateA = await _rejectFragmentCandidate.HandleAsync(
      new RejectFragmentCandidateCommand(
        candidatesToReview[^1].FragmentCandidateId,
        "Deterministic promotion review rejection."),
      cancellationToken);

    var reviewSnapshotAfterAccept = await LoadReviewSnapshotAsync(
      createdProject.ProjectId,
      FragmentCandidateReviewState.HumanAccepted,
      cancellationToken);

    var reviewSnapshotAfterReject = await LoadReviewSnapshotAsync(
      createdProject.ProjectId,
      FragmentCandidateReviewState.Rejected,
      cancellationToken);

    var firstPromotion = await _promoteFragmentCandidate.HandleAsync(
      new PromoteFragmentCandidateCommand(
        acceptedCandidateA.FragmentCandidateId,
        runScope.DocumentType,
        runScope.CanonicalTitle,
        runScope.ExternalReference,
        runScope.Discipline),
      cancellationToken);

    var idempotentReplay = await _promoteFragmentCandidate.HandleAsync(
      new PromoteFragmentCandidateCommand(
        acceptedCandidateA.FragmentCandidateId,
        runScope.DocumentType,
        runScope.CanonicalTitle,
        runScope.ExternalReference,
        runScope.Discipline),
      cancellationToken);

    var firstDiagnostic = await _loadPromotionDiagnostic.HandleAsync(
      new LoadPromotionDiagnosticQuery(firstPromotion.PromotionDiagnosticId),
      cancellationToken);

    var replayDiagnostic = await _loadPromotionDiagnostic.HandleAsync(
      new LoadPromotionDiagnosticQuery(idempotentReplay.PromotionDiagnosticId),
      cancellationToken);

    var sourceBParseResult = await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        createdProject.ProjectId,
        importedSourceB.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);

    var sourceBSnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      importedSourceB.ImportedSourceId,
      cancellationToken);

    var candidatesFromSourceB = sourceBSnapshot.ParserRuns
      .SelectMany(r => r.FragmentCandidates)
      .ToArray();

    var acceptedCandidateB = await _acceptFragmentCandidate.HandleAsync(
      new AcceptFragmentCandidateCommand(
        candidatesFromSourceB[0].FragmentCandidateId,
        "Deterministic supersession review acceptance."),
      cancellationToken);

    var supersedingPromotion = await _promoteFragmentCandidate.HandleAsync(
      new PromoteFragmentCandidateCommand(
        acceptedCandidateB.FragmentCandidateId,
        runScope.DocumentType,
        runScope.CanonicalTitle,
        runScope.ExternalReference,
        runScope.Discipline),
      cancellationToken);

    var supersessionIdempotentReplay = await _promoteFragmentCandidate.HandleAsync(
      new PromoteFragmentCandidateCommand(
        acceptedCandidateB.FragmentCandidateId,
        runScope.DocumentType,
        runScope.CanonicalTitle,
        runScope.ExternalReference,
        runScope.Discipline),
      cancellationToken);

    var knowledgeSnapshot = await _loadProjectKnowledgeSnapshot.HandleAsync(
      new LoadProjectKnowledgeSnapshotQuery(createdProject.ProjectId),
      cancellationToken);

    var failurePresentations = await RunExpectedFailureScenariosAsync(
      createdProject.ProjectId,
      rejectedCandidateA.FragmentCandidateId,
      runScope,
      cancellationToken);

    return new KnowledgePromotionWorkflowResult(
      createdProject,
      beganImportSession,
      importedSourceA,
      importedSourceB,
      completedImportSession,
      firstParseResult,
      firstSnapshot,
      replayParseResult,
      replaySnapshot,
      acceptedCandidateA,
      rejectedCandidateA,
      reviewSnapshotAfterAccept,
      reviewSnapshotAfterReject,
      firstPromotion,
      idempotentReplay,
      firstDiagnostic,
      replayDiagnostic,
      sourceBParseResult,
      sourceBSnapshot,
      supersedingPromotion,
      supersessionIdempotentReplay,
      knowledgeSnapshot,
      [firstPromotion, idempotentReplay, supersedingPromotion, supersessionIdempotentReplay],
      failurePresentations);
  }

  private async Task<IReadOnlyList<DesktopWorkflowFailurePresentation>> RunExpectedFailureScenariosAsync(
    ProjectId projectId,
    FragmentCandidateId rejectedCandidateId,
    WorkflowRunScope runScope,
    CancellationToken cancellationToken)
  {
    var failures = new List<DesktopWorkflowFailurePresentation>();

    var rejectedResult = await _promoteFragmentCandidate.HandleAsync(
      new PromoteFragmentCandidateCommand(
        rejectedCandidateId,
        runScope.DocumentType,
        runScope.CanonicalTitle,
        runScope.ExternalReference,
        runScope.Discipline),
      cancellationToken);

    if (rejectedResult.Status != PromotionDiagnosticStatus.Failed)
    {
      throw new InvalidOperationException(
        $"Expected failure scenario 'promote-rejected-candidate' completed with status {rejectedResult.Status} instead of Failed.");
    }

    failures.Add(new DesktopWorkflowFailurePresentation(
      "promote-rejected-candidate",
      "PromotionFailed",
      rejectedResult.FailureReason ?? "Promotion rejected."));

    failures.Add(await CaptureExpectedFailureAsync(
      "promote-missing-candidate",
      () => _promoteFragmentCandidate.HandleAsync(
        new PromoteFragmentCandidateCommand(
          FragmentCandidateId.New(),
          runScope.DocumentType,
          runScope.CanonicalTitle,
          runScope.ExternalReference,
          runScope.Discipline),
        cancellationToken)));

    return failures;
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
      return new DesktopWorkflowFailurePresentation(
        scenario,
        exception.GetType().Name,
        exception.Message);
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
    KnowledgeDocumentType DocumentType,
    string CanonicalTitle,
    string ExternalReference,
    string Discipline)
  {
    public static WorkflowRunScope Create()
    {
      var suffix = Guid.NewGuid().ToString("N")[..8];
      return new WorkflowRunScope(
        suffix,
        $"Promotion Proof {suffix}",
        $"specification-{suffix}.txt",
        $"run-scope={suffix}\nSection 03 30 00 requires concrete curing in accordance with the approved project specifications.\n\nProvide curing protection immediately after finishing.",
        $"field-observation-{suffix}.txt",
        $"run-scope={suffix}\nInspection of concrete placement on level 3 confirmed curing was initiated within specified time limits.\n\nMoisture retention blankets were placed as required.",
        KnowledgeDocumentType.Specification,
        "Section 03 30 00 - Cast-in-Place Concrete",
        "03 30 00",
        "Concrete");
    }
  }
}
