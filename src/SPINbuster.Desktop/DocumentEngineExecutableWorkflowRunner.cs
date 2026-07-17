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
  private const string DocumentCandidateTypeSchemaId = "document-metadata-candidate";
  private const string FragmentCandidateTypeSchemaId = "document-fragment-candidate";
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
    var runScope = WorkflowRunScope.Create();
    var createdProjectA = await _createProject.HandleAsync(new CreateProjectCommand(runScope.ProjectAName), cancellationToken);
    var beganProjectAImportSession = await _beginDocumentImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(createdProjectA.ProjectId),
      cancellationToken);

    var sourceABytes = CreateContentBytes(runScope, "Section 03 30 00 requires concrete curing in accordance with the approved project specifications.");
    var sourceBBytes = CreateContentBytes(runScope, "Field observation: concrete placement was completed and wet curing was initiated.");

    var importedSourceA = await ImportAsync(
      beganProjectAImportSession.ImportSessionId,
      createdProjectA.ProjectId,
      runScope.PrimarySourceFileName,
      "text/plain",
      sourceABytes,
      cancellationToken);
    var importedSourceB = await ImportAsync(
      beganProjectAImportSession.ImportSessionId,
      createdProjectA.ProjectId,
      runScope.SecondarySourceFileName,
      "text/plain",
      sourceBBytes,
      cancellationToken);
    var importedDuplicateSourceA = await ImportAsync(
      beganProjectAImportSession.ImportSessionId,
      createdProjectA.ProjectId,
      runScope.DuplicateSourceFileName,
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
    var primarySourceBeforeReview = GetImportedSourceSnapshot(projectASnapshotBeforeReview, importedSourceA.ImportedSourceId);
    var createdPrimarySourceCandidates = requestedSourceAProcessing.CandidateIds
      .Select(candidateId => GetCandidateSnapshot(primarySourceBeforeReview, candidateId))
      .ToArray();
    var acceptedCandidateToReview = GetReadyForReviewCandidate(
      createdPrimarySourceCandidates,
      DocumentCandidateType.MetadataCandidate,
      DocumentCandidateTypeSchemaId,
      "current-run metadata review candidate");
    var rejectedCandidateToReview = GetReadyForReviewCandidate(
      createdPrimarySourceCandidates,
      DocumentCandidateType.FragmentCandidate,
      FragmentCandidateTypeSchemaId,
      "current-run fragment review candidate");
    var humanAcceptedCandidate = await _recordDocumentCandidateReview.HandleAsync(
      new RecordDocumentCandidateReviewCommand(
        acceptedCandidateToReview.DocumentCandidateId,
        DocumentCandidateReviewDisposition.HumanAccepted,
        "Deterministic document review acceptance."),
      cancellationToken);
    var rejectedCandidate = await _recordDocumentCandidateReview.HandleAsync(
      new RecordDocumentCandidateReviewCommand(
        rejectedCandidateToReview.DocumentCandidateId,
        DocumentCandidateReviewDisposition.Rejected,
        "Deterministic document review rejection."),
      cancellationToken);

    var projectASnapshot = await LoadSnapshotAsync(createdProjectA.ProjectId, cancellationToken);
    var replayedProjectASnapshot = await LoadSnapshotAsync(createdProjectA.ProjectId, cancellationToken);

    var createdProjectB = await _createProject.HandleAsync(new CreateProjectCommand(runScope.ProjectBName), cancellationToken);
    var beganProjectBImportSession = await _beginDocumentImportSession.HandleAsync(
      new BeginDocumentImportSessionCommand(createdProjectB.ProjectId),
      cancellationToken);
    var importedProjectBCopy = await ImportAsync(
      beganProjectBImportSession.ImportSessionId,
      createdProjectB.ProjectId,
      runScope.ProjectBCopyFileName,
      "text/plain",
      sourceABytes,
      cancellationToken);
    var completedProjectBImportSession = await _completeDocumentImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(beganProjectBImportSession.ImportSessionId),
      cancellationToken);
    var projectBSnapshot = await LoadSnapshotAsync(createdProjectB.ProjectId, cancellationToken);

    var processingScenarios = await RunProcessingScenariosAsync(createdProjectA.ProjectId, runScope, cancellationToken);
    var failurePresentations = await RunExpectedFailureScenariosAsync(createdProjectA.ProjectId, runScope, cancellationToken);

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
    WorkflowRunScope runScope,
    CancellationToken cancellationToken)
  {
    var scenarios = new List<DocumentEngineProcessingScenarioResult>();

    async Task AddScenarioAsync(string scenario, string fileName, string content, CancellationToken token = default)
    {
      var effectiveToken = token == default ? cancellationToken : token;
      var source = await CreateStandaloneSourceAsync(projectId, WithRunScope(fileName, runScope), content, cancellationToken);
      RequestDocumentProcessingResult processingResult;
      try
      {
        processingResult = await _requestDocumentProcessing.HandleAsync(
          new RequestDocumentProcessingCommand(source.ImportedSourceId, projectId),
          effectiveToken);
      }
      catch (OperationCanceledException)
      {
        var snapshotAfterCancellation = await LoadSnapshotAsync(projectId, cancellationToken);
        var sourceSnapshotAfterCancellation = GetImportedSourceSnapshot(snapshotAfterCancellation, source.ImportedSourceId);
        var cancelledAttempt = RequireSingleMatch(
          sourceSnapshotAfterCancellation.ProcessingAttempts,
          attempt => attempt.State == DocumentProcessingAttemptState.Cancelled,
          $"cancelled processing attempt for scenario '{scenario}'");
        scenarios.Add(new DocumentEngineProcessingScenarioResult(
          scenario,
          source.ImportedSourceId,
          cancelledAttempt.State,
          cancelledAttempt.FailureClassification,
          []));
        return;
      }

      var snapshot = await LoadSnapshotAsync(projectId, cancellationToken);
      var sourceSnapshot = GetImportedSourceSnapshot(snapshot, source.ImportedSourceId);
      var scopedAttempt = GetProcessingAttemptSnapshot(sourceSnapshot, processingResult.ProcessingAttemptId);
      var scopedCandidateStatuses = processingResult.CandidateIds
        .Select(candidateId => GetCandidateSnapshot(sourceSnapshot, candidateId).Status)
        .ToArray();

      scenarios.Add(new DocumentEngineProcessingScenarioResult(
        scenario,
        source.ImportedSourceId,
        scopedAttempt.State,
        scopedAttempt.FailureClassification,
        scopedCandidateStatuses));
    }

    await AddScenarioAsync("structured-failure", "structured-failure.txt", CreateScopedContent(runScope, "processor structured failure"), cancellationToken);
    await AddScenarioAsync("processor-throws", "processor-throws.txt", CreateScopedContent(runScope, "processor throws"), cancellationToken);
    await AddScenarioAsync("processor-cancelled", "processor-cancel.txt", CreateScopedContent(runScope, "processor cancellation"), cancellationToken);

    await AddScenarioAsync("malformed-candidate", "malformed.txt", CreateScopedContent(runScope, "malformed candidate"), cancellationToken);
    await AddScenarioAsync("schema-rejected", "schema-rejected.txt", CreateScopedContent(runScope, "schema rejected candidate"), cancellationToken);
    await AddScenarioAsync("policy-rejected", "policy-rejected.txt", CreateScopedContent(runScope, "policy rejected candidate"), cancellationToken);

    return scenarios;
  }

  private async Task<IReadOnlyList<DesktopWorkflowFailurePresentation>> RunExpectedFailureScenariosAsync(
    ProjectId projectId,
    WorkflowRunScope runScope,
    CancellationToken cancellationToken)
  {
    var failures = new List<DesktopWorkflowFailurePresentation>();

    failures.Add(await CaptureExpectedFailureAsync(
      "unsupported-media-type",
      async () =>
      {
        var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
        await ImportAsync(
          session.ImportSessionId,
          projectId,
          WithRunScope("unsupported.bin", runScope),
          "application/octet-stream",
          CreateContentBytes(runScope, "binary"),
          cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "maximum-size-exceeded",
      async () =>
      {
        var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
        await ImportAsync(
          session.ImportSessionId,
          projectId,
          WithRunScope("too-large.txt", runScope),
          "text/plain",
          CreateLargeScopedContent(runScope),
          cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "duplicate-into-completed-session",
      async () =>
      {
        var session = await _beginDocumentImportSession.HandleAsync(new BeginDocumentImportSessionCommand(projectId), cancellationToken);
        await _completeDocumentImportSession.HandleAsync(new CompleteDocumentImportSessionCommand(session.ImportSessionId), cancellationToken);
        await ImportAsync(
          session.ImportSessionId,
          projectId,
          WithRunScope("late-import.txt", runScope),
          "text/plain",
          CreateContentBytes(runScope, "late import"),
          cancellationToken);
      }));

    failures.Add(await CaptureExpectedFailureAsync(
      "invalid-candidate-review-transition",
      async () =>
      {
        var source = await CreateStandaloneSourceAsync(projectId, WithRunScope("review-invalid.txt", runScope), CreateScopedContent(runScope, "review invalid"), cancellationToken);
        var processingResult = await _requestDocumentProcessing.HandleAsync(new RequestDocumentProcessingCommand(source.ImportedSourceId, projectId), cancellationToken);
        var snapshot = await LoadSnapshotAsync(projectId, cancellationToken);
        var sourceSnapshot = GetImportedSourceSnapshot(snapshot, source.ImportedSourceId);
        var candidate = GetReadyForReviewCandidate(
          processingResult.CandidateIds.Select(candidateId => GetCandidateSnapshot(sourceSnapshot, candidateId)).ToArray(),
          DocumentCandidateType.MetadataCandidate,
          DocumentCandidateTypeSchemaId,
          "invalid-transition review candidate");
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
    var importResult = await ImportAsync(session.ImportSessionId, projectId, fileName, "text/plain", Encoding.UTF8.GetBytes(content), cancellationToken);
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

  private static ProjectImportedDocumentSourceSnapshot GetImportedSourceSnapshot(
    LoadProjectDocumentWorkflowSnapshotResult snapshot,
    ImportedSourceId importedSourceId)
  {
    return RequireSingleMatch(
      snapshot.ImportedSources,
      source => source.ImportedSourceId == importedSourceId,
      $"imported source {importedSourceId}");
  }

  private static ProjectDocumentProcessingAttemptSnapshot GetProcessingAttemptSnapshot(
    ProjectImportedDocumentSourceSnapshot sourceSnapshot,
    DocumentProcessingAttemptId processingAttemptId)
  {
    return RequireSingleMatch(
      sourceSnapshot.ProcessingAttempts,
      attempt => attempt.ProcessingAttemptId == processingAttemptId,
      $"processing attempt {processingAttemptId} for source {sourceSnapshot.ImportedSourceId}");
  }

  private static ProjectDocumentCandidateSnapshot GetCandidateSnapshot(
    ProjectImportedDocumentSourceSnapshot sourceSnapshot,
    DocumentCandidateId documentCandidateId)
  {
    return RequireSingleMatch(
      sourceSnapshot.Candidates,
      candidate => candidate.DocumentCandidateId == documentCandidateId,
      $"candidate {documentCandidateId} for source {sourceSnapshot.ImportedSourceId}");
  }

  private static ProjectDocumentCandidateSnapshot GetReadyForReviewCandidate(
    IReadOnlyList<ProjectDocumentCandidateSnapshot> candidates,
    DocumentCandidateType candidateType,
    string schemaId,
    string description)
  {
    return RequireSingleMatch(
      candidates,
      candidate => candidate.CandidateType == candidateType
        && string.Equals(candidate.SchemaId, schemaId, StringComparison.Ordinal)
        && candidate.Status == DocumentCandidateStatus.ReadyForReview,
      description);
  }

  private static T RequireSingleMatch<T>(
    IEnumerable<T> values,
    Func<T, bool> predicate,
    string description)
  {
    var matches = values.Where(predicate).Take(2).ToArray();
    return matches.Length switch
    {
      1 => matches[0],
      0 => throw new InvalidOperationException($"Expected exactly one {description}, but none were found."),
      _ => throw new InvalidOperationException($"Expected exactly one {description}, but multiple matches were found."),
    };
  }

  private static byte[] CreateContentBytes(WorkflowRunScope runScope, string text) => Encoding.UTF8.GetBytes(CreateScopedContent(runScope, text));

  private static string CreateScopedContent(WorkflowRunScope runScope, string text) => $"run-scope={runScope.Suffix}{Environment.NewLine}{text}";

  private static byte[] CreateLargeScopedContent(WorkflowRunScope runScope)
  {
    return Encoding.UTF8.GetBytes(CreateScopedContent(runScope, new string('x', 11 * 1024 * 1024)));
  }

  private static string WithRunScope(string fileName, WorkflowRunScope runScope)
  {
    var extension = Path.GetExtension(fileName);
    var baseName = Path.GetFileNameWithoutExtension(fileName);
    return $"{baseName}-{runScope.Suffix}{extension}";
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
      || exception is ApplicationEntityNotFoundException
      || exception is IOException)
    {
      return new DesktopWorkflowFailurePresentation(scenario, exception.GetType().Name, exception.Message);
    }

    throw new InvalidOperationException($"Expected failure scenario '{scenario}' completed successfully.");
  }

  private sealed record WorkflowRunScope(
    string Suffix,
    string ProjectAName,
    string ProjectBName,
    string PrimarySourceFileName,
    string SecondarySourceFileName,
    string DuplicateSourceFileName,
    string ProjectBCopyFileName)
  {
    public static WorkflowRunScope Create()
    {
      var suffix = Guid.NewGuid().ToString("N")[..8];
      return new WorkflowRunScope(
        suffix,
        $"Project A {suffix}",
        $"Project B {suffix}",
        $"concrete-specification-{suffix}.txt",
        $"field-observation-{suffix}.txt",
        $"duplicate-concrete-specification-{suffix}.txt",
        $"shared-copy-{suffix}.txt");
    }
  }
}
