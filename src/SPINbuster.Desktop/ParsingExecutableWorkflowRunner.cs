using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadParsingSnapshot;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
using SPINbuster.Application.UseCases.RequestDocumentParsing;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed class ParsingExecutableWorkflowRunner
{
  private const string ParserKey = "plain-text-deterministic";
  private const string ParserContractVersion = "1.0.0";
  private const int SnapshotMaxSessions = 50;
  private const int SnapshotMaxSources = 100;
  private const int SnapshotMaxAttempts = 20;
  private const int SnapshotMaxCandidates = 50;
  private const int SnapshotMaxAudit = 50;

  private readonly ICommandHandler<CreateProjectCommand, CreateProjectResult> _createProject;
  private readonly ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> _beginImportSession;
  private readonly ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> _importSource;
  private readonly ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> _completeImportSession;
  private readonly ICommandHandler<RequestDocumentParsingCommand, RequestDocumentParsingResult> _requestDocumentParsing;
  private readonly IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult> _loadParsingSnapshot;
  private readonly IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult> _loadDocumentWorkflowSnapshot;

  public ParsingExecutableWorkflowRunner(
    ICommandHandler<CreateProjectCommand, CreateProjectResult> createProject,
    ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult> beginImportSession,
    ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult> importSource,
    ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult> completeImportSession,
    ICommandHandler<RequestDocumentParsingCommand, RequestDocumentParsingResult> requestDocumentParsing,
    IQueryHandler<LoadParsingSnapshotQuery, LoadParsingSnapshotResult> loadParsingSnapshot,
    IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult> loadDocumentWorkflowSnapshot)
  {
    _createProject = createProject;
    _beginImportSession = beginImportSession;
    _importSource = importSource;
    _completeImportSession = completeImportSession;
    _requestDocumentParsing = requestDocumentParsing;
    _loadParsingSnapshot = loadParsingSnapshot;
    _loadDocumentWorkflowSnapshot = loadDocumentWorkflowSnapshot;
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

    var textBytes = Encoding.UTF8.GetBytes(runScope.TextContent);
    await using var textStream = new MemoryStream(textBytes, writable: false);
    var importedSource = await _importSource.HandleAsync(
      new ImportDocumentSourceCommand(
        beganImportSession.ImportSessionId,
        createdProject.ProjectId,
        runScope.TextFileName,
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        textStream),
      cancellationToken);

    var completedImportSession = await _completeImportSession.HandleAsync(
      new CompleteDocumentImportSessionCommand(beganImportSession.ImportSessionId),
      cancellationToken);

    var firstParseResult = await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        createdProject.ProjectId,
        importedSource.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);

    var firstSnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      importedSource.ImportedSourceId,
      cancellationToken);

    var replayParseResult = await _requestDocumentParsing.HandleAsync(
      new RequestDocumentParsingCommand(
        createdProject.ProjectId,
        importedSource.ImportedSourceId,
        ParserKey,
        ParserContractVersion),
      cancellationToken);

    var replaySnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      importedSource.ImportedSourceId,
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

    var finalSnapshot = await LoadParsingSnapshotAsync(
      createdProject.ProjectId,
      importedSource.ImportedSourceId,
      cancellationToken);

    return new ParsingExecutableWorkflowResult(
      createdProject,
      beganImportSession,
      importedSource,
      completedImportSession,
      firstParseResult,
      firstSnapshot,
      replayParseResult,
      replaySnapshot,
      unsupportedMediaResult,
      cancelledParseResult,
      malformedOutputResult,
      finalSnapshot);
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

  private sealed record WorkflowRunScope(
    string Suffix,
    string ProjectName,
    string TextFileName,
    string TextContent,
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
        $"unsupported-{suffix}.pdf",
        $"cancel-{suffix}.txt",
        $"run-scope={suffix}\nThis content will be parsed before cancellation.",
        $"whitespace-{suffix}.txt");
    }
  }
}
