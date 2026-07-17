using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
using SPINbuster.Application.UseCases.RecordDocumentCandidateReview;
using SPINbuster.Application.UseCases.RequestDocumentProcessing;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed class DocumentEngineExecutableWorkflowRunner
{
  private readonly ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> _beginDocumentImportSession;
  private readonly ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> _completeDocumentImportSession;
  private readonly ICommandHandler<CreateProjectCommand, CreateProjectResult> _createProject;
  private readonly ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> _importDocumentSource;
  private readonly IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult> _loadProjectDocumentWorkflowSnapshot;
  private readonly ICommandHandler<RecordDocumentCandidateReviewCommand, RecordDocumentCandidateReviewResult> _recordDocumentCandidateReview;
  private readonly ICommandHandler<RequestDocumentProcessingCommand, RequestDocumentProcessingResult> _requestDocumentProcessing;

  public DocumentEngineExecutableWorkflowRunner(
    ICommandHandler<CreateProjectCommand, CreateProjectResult> createProject,
    ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> beginDocumentImportSession,
    ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> importDocumentSource,
    ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> completeDocumentImportSession,
    ICommandHandler<RequestDocumentProcessingCommand, RequestDocumentProcessingResult> requestDocumentProcessing,
    ICommandHandler<RecordDocumentCandidateReviewCommand, RecordDocumentCandidateReviewResult> recordDocumentCandidateReview,
    IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult> loadProjectDocumentWorkflowSnapshot)
  {
    _createProject = createProject;
    _beginDocumentImportSession = beginDocumentImportSession;
    _importDocumentSource = importDocumentSource;
    _completeDocumentImportSession = completeDocumentImportSession;
    _requestDocumentProcessing = requestDocumentProcessing;
    _recordDocumentCandidateReview = recordDocumentCandidateReview;
    _loadProjectDocumentWorkflowSnapshot = loadProjectDocumentWorkflowSnapshot;
  }

  public async Task<DocumentEngineExecutableWorkflowResult> RunAsync(CancellationToken cancellationToken = default)
  {
    var createdProjectA = await _createProject.HandleAsync(new CreateProjectCommand("Project A"), cancellationToken);
    var beganProjectAImportSession = await _beginDocumentImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(createdProjectA.ProjectId),
      cancellationToken);

    var sourceABytes = CreateContentBytes("Section 03 30 00 requires concrete curing in accordance with the approved project specifications.");
    var sourceBBytes = CreateContentBytes("Field observation: concrete placement was completed and wet curing was initiated.");

    var importedSourceA = await ImportAsync(
      beganProjectAImportSession.ImportSessionId,
      createdProjectA.ProjectId,
      "concrete-specification.txt",
      "text/plain",
      sourceABytes,
      cancellationToken);
    var importedSourceB = await ImportAsync(
      beganProjectAImportSession.ImportSessionId,
      createdProjectA.ProjectId,
      "field-observation.txt",
      "text/plain",
      sourceBBytes,
      cancellationToken);
    var importedDuplicateSourceA = await ImportAsync(
      beganProjectAImportSession.ImportSessionId,
      createdProjectA.ProjectId,
      "duplicate-concrete-specification.txt",
      "text/plain",
      sourceABytes,
      cancellationToken);
    var completedProjectAImportSession = await _completeDocumentImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(beganProjectAImportSession.ImportSessionId),
      cancellationToken);

    var requestedSourceAProcessing = await _requestDocumentProcessing.HandleAsync(
      new RequestDocumentProcessingCommand(importedSourceA.ImportedSourceId, createdProjectA.ProjectId),
      cancellationToken);

    var projectASnapshotBeforeReview = await LoadSnapshotAsync(createdProjectA.ProjectId, cancellationToken);
    var reviewableCandidates = projectASnapshotBeforeReview.ImportedSources
      .Single(source => source.ImportedSourceId == importedSourceA.ImportedSourceId)
      .Candidates
      .Where(candidate => candidate.Status == DocumentCandidateStatus.ReadyForReview)
      .OrderBy(candidate => candidate.CreatedAtUtc)
      .ToArray();
    var humanAcceptedCandidate = await _recordDocumentCandidateReview.HandleAsync(
      new RecordDocumentCandidateReviewCommand(
        reviewableCandidates[0].DocumentCandidateId,
        DocumentCandidateReviewDisposition.HumanAccepted,
        "Deterministic document review acceptance."),
      cancellationToken);
    var rejectedCandidate = await _recordDocumentCandidateReview.HandleAsync(
      new RecordDocumentCandidateReviewCommand(
        reviewableCandidates[1].DocumentCandidateId,
        DocumentCandidateReviewDisposition.Rejected,
        "Deterministic document review rejection."),
      cancellationToken);

    var projectASnapshot = await LoadSnapshotAsync(createdProjectA.ProjectId, cancellationToken);
    var replayedProjectASnapshot = await LoadSnapshotAsync(createdProjectA.ProjectId, cancellationToken);

    var createdProjectB = await _createProject.HandleAsync(new CreateProjectCommand("Project B"), cancellationToken);
    var beganProjectBImportSession = await _beginDocumentImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(createdProjectB.ProjectId),
      cancellationToken);
    var importedProjectBCopy = await ImportAsync(
      beganProjectBImportSession.ImportSessionId,
      createdProjectB.ProjectId,
      "shared-copy.txt",
      "text/plain",
      sourceABytes,
      cancellationToken);
    var completedProjectBImportSession = await _completeDocumentImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(beganProjectBImportSession.ImportSessionId),
      cancellationToken);
    var projectBSnapshot = await LoadSnapshotAsync(createdProjectB.ProjectId, cancellationToken);

    var processingScenarios = await RunProcessingScenariosAsync(createdProjectA.ProjectId, cancellationToken);
    var failurePresentations = await RunExpectedFailureScenariosAsync(createdProjectA.ProjectId, cancellationToken);

    return new DocumentEngineExecutableWorkflowResult(
      createdProjectA,
      beganProjectAImportSession,
      importedSourceA,
      importedSourceB,
      importedDuplicateSourceA,
      completedProjectAImportSession,
      requestedSourceAProcessing,
      humanAcceptedCandidate,
      rejectedCandidate,
      projectASnapshot,
      replayedProjectASnapshot,
      createdProjectB,
      beganProjectBImportSession,
      importedProjectBCopy,
      completedProjectBImportSession,
      projectBSnapshot,
      processingScenarios,
      failurePresentations);
  }

  private async Task<IReadOnlyList<DocumentEngineProcessingScenarioResult>> RunProcessingScenariosAsync(
    ProjectId projectId,
    CancellationToken cancellationToken)
  {
    var scenarios = new List<DocumentEngineProcessingScenarioResult>();

    async Task AddScenarioAsync(string scenario, string fileName, string content, CancellationToken token = default)
    {
      var effectiveToken = token == default ? cancellationToken : token;
      var source = await CreateStandaloneSourceAsync(projectId, fileName, content, cancellationToken);
      try
      {
        await _requestDocumentProcessing.HandleAsync(
          new RequestDocumentProcessingCommand(source.ImportedSourceId, projectId),
          effectiveToken);
      }
      catch (OperationCanceledException)
      {
      }

      var snapshot = await LoadSnapshotAsync(projectId, cancellationToken);
      var sourceSnapshot = snapshot.ImportedSources.Single(item => item.ImportedSourceId == source.ImportedSourceId);
      var latestAttempt = sourceSnapshot.ProcessingAttempts
        .OrderByDescending(item => item.AttemptNumber)
        .FirstOrDefault();
      if (latestAttempt is null)
      {
        throw new InvalidOperationException($"Processing scenario '{scenario}' did not persist a durable processing attempt.");
      }

      scenarios.Add(new DocumentEngineProcessingScenarioResult(
        scenario,
        source.ImportedSourceId,
        latestAttempt.State,
        latestAttempt.FailureClassification,
        sourceSnapshot.Candidates.Select(candidate => candidate.Status).ToArray()));
    }

    await AddScenarioAsync("storage-read-unavailable", "storage-read-unavailable.txt", "SIMULATE_READ_UNAVAILABLE", cancellationToken);
    await AddScenarioAsync("storage-read-throws", "storage-read-throws.txt", "SIMULATE_READ_FAILURE", cancellationToken);
    await AddScenarioAsync("structured-failure", "structured-failure.txt", "processor structured failure", cancellationToken);
    await AddScenarioAsync("processor-throws", "processor-throws.txt", "processor throws", cancellationToken);
    await AddScenarioAsync("processor-cancelled", "processor-cancel.txt", "processor cancellation", cancellationToken);

    await AddScenarioAsync("malformed-candidate", "malformed.txt", "malformed candidate", cancellationToken);
    await AddScenarioAsync("schema-rejected", "schema-rejected.txt", "schema rejected candidate", cancellationToken);
    await AddScenarioAsync("policy-rejected", "policy-rejected.txt", "policy rejected candidate", cancellationToken);

    return scenarios;
  }

  private async Task<IReadOnlyList<DesktopWorkflowFailurePresentation>> RunExpectedFailureScenariosAsync(
    ProjectId projectId,
    CancellationToken cancellationToken)
  {
    var failures = new List<DesktopWorkflowFailurePresentation>();

    failures.Add(await CaptureExpectedFailureAsync(
      "unsupported-media-type",
      async () =>
      {
        var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
        await ImportAsync(session.ImportSessionId, projectId, "unsupported.bin", "application/octet-stream", CreateContentBytes("binary"), cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "maximum-size-exceeded",
      async () =>
      {
        var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
        await ImportAsync(session.ImportSessionId, projectId, "too-large.txt", "text/plain", new byte[11 * 1024 * 1024], cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "storage-write-failure",
      async () =>
      {
        var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
        await ImportAsync(session.ImportSessionId, projectId, "storage-write-failure.txt", "text/plain", CreateContentBytes("SIMULATE_WRITE_FAILURE"), cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "duplicate-into-completed-session",
      async () =>
      {
        var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
        await _completeDocumentImportSession.HandleAsync(new CompleteDocumentImportSessionCommand(session.ImportSessionId), cancellationToken);
        await ImportAsync(session.ImportSessionId, projectId, "late-import.txt", "text/plain", CreateContentBytes("late import"), cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "invalid-candidate-review-transition",
      async () =>
      {
        var source = await CreateStandaloneSourceAsync(projectId, "review-invalid.txt", "review invalid", cancellationToken);
        await _requestDocumentProcessing.HandleAsync(new RequestDocumentProcessingCommand(source.ImportedSourceId, projectId), cancellationToken);
        var snapshot = await LoadSnapshotAsync(projectId, cancellationToken);
        var candidate = snapshot.ImportedSources.Single(item => item.ImportedSourceId == source.ImportedSourceId).Candidates[0];
        await _recordDocumentCandidateReview.HandleAsync(
          new RecordDocumentCandidateReviewCommand(candidate.DocumentCandidateId, DocumentCandidateReviewDisposition.HumanAccepted, "first pass"),
          cancellationToken);
        await _recordDocumentCandidateReview.HandleAsync(
          new RecordDocumentCandidateReviewCommand(candidate.DocumentCandidateId, DocumentCandidateReviewDisposition.Rejected, "invalid second pass"),
          cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "snapshot-invalid-bounds",
      async () => await _loadProjectDocumentWorkflowSnapshot.HandleAsync(
        new LoadProjectDocumentWorkflowSnapshotQuery(projectId, 0, 10, 10, 10, 10),
        cancellationToken)));

    return failures;
  }

  private async Task<ImportDocumentSourceResult> CreateStandaloneSourceAsync(
    ProjectId projectId,
    string fileName,
    string content,
    CancellationToken cancellationToken)
  {
    var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
    var importResult = await ImportAsync(session.ImportSessionId, projectId, fileName, "text/plain", CreateContentBytes(content), cancellationToken);
    await _completeDocumentImportSession.HandleAsync(new CompleteDocumentImportSessionCommand(session.ImportSessionId), cancellationToken);
    return importResult;
  }

  private async Task<LoadProjectDocumentWorkflowSnapshotResult> LoadSnapshotAsync(ProjectId projectId, CancellationToken cancellationToken)
  {
    return await _loadProjectDocumentWorkflowSnapshot.HandleAsync(
      new LoadProjectDocumentWorkflowSnapshotQuery(projectId, 50, 100, 20, 50, 50),
      cancellationToken);
  }

  private async Task<ImportDocumentSourceResult> ImportAsync(
    DocumentImportSessionId importSessionId,
    ProjectId projectId,
    string fileName,
    string declaredMediaType,
    byte[] bytes,
    CancellationToken cancellationToken)
  {
    await using var content = new MemoryStream(bytes, writable: false);
    return await _importDocumentSource.HandleAsync(
      new ImportDocumentSourceCommand(
        importSessionId,
        projectId,
        fileName,
        declaredMediaType,
        ImportedSourceOrigin.LocalFile,
        null,
        content),
      cancellationToken);
  }

  private static byte[] CreateContentBytes(string text) => Encoding.UTF8.GetBytes(text);

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
      || exception is ApplicationEntityNotFoundException
      || exception is IOException)
    {
      return new DesktopWorkflowFailurePresentation(scenario, exception.GetType().Name, exception.Message);
    }

    throw new InvalidOperationException($"Expected failure scenario '{scenario}' completed successfully.");
  }
}
