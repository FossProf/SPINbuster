using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SPINbuster.Application;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
using SPINbuster.Domain;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Services;
using System.Globalization;

namespace SPINbuster.Desktop.Tests;

public sealed class DocumentEngineExecutableWorkflowTests
{
  [Fact]
  public async Task WorkflowRunsMultiSourceDocumentSliceAndPreservesAuthorityIsolation()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString);
      var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.True(File.Exists(databasePath));
      Assert.Equal(DocumentImportSessionState.Completed, result.CompletedProjectAImportSession.State);
      Assert.Equal(2, result.CompletedProjectAImportSession.AcceptedCount);
      Assert.Equal(1, result.CompletedProjectAImportSession.DuplicateCount);
      Assert.True(result.ImportedDuplicateSourceA.ReusedExistingProjectSource);
      Assert.Equal(result.ImportedSourceA.ImportedSourceId, result.ImportedDuplicateSourceA.ImportedSourceId);
      Assert.True(result.ImportedProjectBCopy.SameContentExistsInAnotherProject);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.KnowledgeDocumentCount);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.ReportCount);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.AiProposalCount);

      var projectASource = GetImportedSource(result.ProjectASnapshot, result.ImportedSourceA.ImportedSourceId);
      Assert.Equal(2, projectASource.Candidates.Count);
      Assert.Contains(projectASource.Candidates, candidate => candidate.Status == DocumentCandidateStatus.HumanAccepted);
      Assert.Contains(projectASource.Candidates, candidate => candidate.Status == DocumentCandidateStatus.Rejected);
      Assert.All(projectASource.Candidates, candidate => Assert.Equal(projectASource.ContentHash, candidate.SourceContentHash));
      Assert.DoesNotContain(result.ProjectBSnapshot.ImportedSources.Select(source => source.OriginalFileName), name => string.Equals(name, "concrete-specification.txt", StringComparison.Ordinal));
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowSnapshotReloadsFromFreshProviderWithoutMutation()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      DocumentEngineExecutableWorkflowResult initialResult;
      using (var firstProvider = CreateServiceProvider(connectionString))
      {
        initialResult = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(firstProvider);
      }

      using var secondProvider = CreateServiceProvider(connectionString);
      await DocumentEngineExecutableWorkflowBootstrapper.MigrateAsync(secondProvider);
      await using var scope = secondProvider.CreateAsyncScope();
      var query = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult>>();

      var reloadedSnapshot = await query.HandleAsync(new LoadProjectDocumentWorkflowSnapshotQuery(
        initialResult.CreatedProjectA.ProjectId,
        50,
        100,
        20,
        50,
        50));

      Assert.True(reloadedSnapshot.ImportSessions.Count >= initialResult.ReplayedProjectASnapshot.ImportSessions.Count);
      var reloadedImportSession = GetImportSession(reloadedSnapshot, initialResult.BeganProjectAImportSession.ImportSessionId);
      var originalImportSession = GetImportSession(initialResult.ReplayedProjectASnapshot, initialResult.BeganProjectAImportSession.ImportSessionId);
      Assert.Equal(originalImportSession.ImportSessionId, reloadedImportSession.ImportSessionId);
      Assert.Equal(originalImportSession.State, reloadedImportSession.State);

      var reloadedPrimarySource = GetImportedSource(reloadedSnapshot, initialResult.ImportedSourceA.ImportedSourceId);
      var originalPrimarySource = GetImportedSource(initialResult.ProjectASnapshot, initialResult.ImportedSourceA.ImportedSourceId);
      Assert.Equal(originalPrimarySource.ContentHash, reloadedPrimarySource.ContentHash);
      Assert.Equal(
        originalPrimarySource.Candidates.Select(candidate => (candidate.DocumentCandidateId, candidate.Status)),
        reloadedPrimarySource.Candidates.Select(candidate => (candidate.DocumentCandidateId, candidate.Status)));
      Assert.Equal(initialResult.ProjectASnapshot.AuthorityIsolation, reloadedSnapshot.AuthorityIsolation);
      Assert.True(reloadedSnapshot.AuditHistory.Count >= initialResult.ProjectASnapshot.AuditHistory.Count);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowAllowsExactByteReopenWithoutExposingPaths()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString);
      var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(serviceProvider);
      var source = GetImportedSource(result.ProjectASnapshot, result.ImportedSourceA.ImportedSourceId);
      var store = serviceProvider.GetRequiredService<IImmutableContentStore>();

      var reopened = await store.OpenReadAsync(source.Storage.StorageObjectId);
      await using var stream = reopened.Content;
      using var memory = new MemoryStream();
      await stream.CopyToAsync(memory);
      var reopenedText = System.Text.Encoding.UTF8.GetString(memory.ToArray());

      Assert.Equal(StorageAvailabilityState.Available, reopened.AvailabilityState);
      Assert.Equal(source.ContentHash, reopened.ContentHash);
      Assert.Contains("Section 03 30 00 requires concrete curing", reopenedText, StringComparison.Ordinal);
      Assert.DoesNotContain(source.Storage.ImmutableObjectKey, char.IsWhiteSpace);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowPersistsDeterministicProcessingTerminalScenarios()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString);
      var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Contains(result.ProcessingScenarios, scenario =>
        scenario.Scenario == "storage-read-unavailable"
        && scenario.State == DocumentProcessingAttemptState.Failed
        && scenario.FailureClassification == DocumentProcessingFailureClassification.StorageUnavailable);
      Assert.Contains(result.ProcessingScenarios, scenario =>
        scenario.Scenario == "storage-read-throws"
        && scenario.State == DocumentProcessingAttemptState.Failed
        && scenario.FailureClassification == DocumentProcessingFailureClassification.Unknown);
      Assert.Contains(result.ProcessingScenarios, scenario =>
        scenario.Scenario == "structured-failure"
        && scenario.State == DocumentProcessingAttemptState.Failed
        && scenario.FailureClassification == DocumentProcessingFailureClassification.ProviderUnavailable);
      Assert.Contains(result.ProcessingScenarios, scenario =>
        scenario.Scenario == "processor-cancelled"
        && scenario.State == DocumentProcessingAttemptState.Cancelled
        && scenario.FailureClassification == DocumentProcessingFailureClassification.Cancelled);
      Assert.Contains(result.ProcessingScenarios, scenario =>
        scenario.Scenario == "schema-rejected"
        && scenario.CandidateStatuses.Contains(DocumentCandidateStatus.SchemaRejected));
      Assert.Contains(result.ProcessingScenarios, scenario =>
        scenario.Scenario == "policy-rejected"
        && scenario.CandidateStatuses.Contains(DocumentCandidateStatus.PolicyRejected));
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowCapturesExpectedFailurePresentationsWithoutCrashing()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString);
      var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(serviceProvider);

      Assert.Equal(
        [
          "unsupported-media-type",
          "maximum-size-exceeded",
          "storage-write-failure",
          "duplicate-into-completed-session",
          "invalid-candidate-review-transition",
          "snapshot-invalid-bounds",
        ],
        result.FailurePresentations.Select(item => item.Scenario));
      Assert.All(result.FailurePresentations, item => Assert.False(string.IsNullOrWhiteSpace(item.Message)));
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task CommitFailureAfterContentStorageLeavesNoPartialDatabaseState()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString, services =>
      {
        services.RemoveAll<IUnitOfWork>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
          new ThrowingUnitOfWork(serviceProvider.GetRequiredService<SqliteUnitOfWork>(), 3));
      });

      await DocumentEngineExecutableWorkflowBootstrapper.MigrateAsync(serviceProvider);
      await using var scope = serviceProvider.CreateAsyncScope();
      var createProject = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProjectCommand, CreateProjectResult>>();
      var beginImport = scope.ServiceProvider.GetRequiredService<ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult>>();
      var import = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult>>();
      var store = scope.ServiceProvider.GetRequiredService<IImmutableContentStore>();

      var project = await createProject.HandleAsync(new CreateProjectCommand("Commit Failure Project"));
      var session = await beginImport.HandleAsync(new BeginDocumentImportSessionCommand(project.ProjectId));

      await Assert.ThrowsAsync<InvalidOperationException>(async () =>
      {
        await using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("orphan candidate"));
        await import.HandleAsync(new ImportDocumentSourceCommand(
          session.ImportSessionId,
          project.ProjectId,
          "commit-failure.txt",
          "text/plain",
          ImportedSourceOrigin.LocalFile,
          null,
          content));
      });

      await using var verificationScope = serviceProvider.CreateAsyncScope();
      var dbContext = verificationScope.ServiceProvider.GetRequiredService<SpinbusterDbContext>();
      await dbContext.Database.OpenConnectionAsync();
      await using var command = dbContext.Database.GetDbConnection().CreateCommand();
      command.CommandText = "SELECT COUNT(*) FROM imported_document_sources";
      var importedSourceCount = Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);

      Assert.Equal(0L, importedSourceCount);
      Assert.NotNull(await store.FindByHashAsync(
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("orphan candidate"))),
        "SHA-256",
        1));
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task SameFileNameWithDifferentBytesIsNotTreatedAsDuplicate()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString);
      await DocumentEngineExecutableWorkflowBootstrapper.MigrateAsync(serviceProvider);
      await using var scope = serviceProvider.CreateAsyncScope();
      var createProject = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProjectCommand, CreateProjectResult>>();
      var beginImport = scope.ServiceProvider.GetRequiredService<ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult>>();
      var import = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult>>();
      var complete = scope.ServiceProvider.GetRequiredService<ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult>>();

      var project = await createProject.HandleAsync(new CreateProjectCommand("Filename Duplicate Check"));
      var session = await beginImport.HandleAsync(new BeginDocumentImportSessionCommand(project.ProjectId));
      await using var firstContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("first bytes"));
      var first = await import.HandleAsync(new ImportDocumentSourceCommand(
        session.ImportSessionId,
        project.ProjectId,
        "same-name.txt",
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        firstContent));
      await using var secondContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("second bytes"));
      var second = await import.HandleAsync(new ImportDocumentSourceCommand(
        session.ImportSessionId,
        project.ProjectId,
        "same-name.txt",
        "text/plain",
        ImportedSourceOrigin.LocalFile,
        null,
        secondContent));
      await complete.HandleAsync(new CompleteDocumentImportSessionCommand(session.ImportSessionId));

      Assert.False(second.ReusedExistingProjectSource);
      Assert.NotEqual(first.ImportedSourceId, second.ImportedSourceId);
      Assert.NotEqual(first.ContentHash, second.ContentHash);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowCanRunTwiceAgainstTheSameDatabaseWithoutMutatingPriorData()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      DocumentEngineExecutableWorkflowResult firstRun;
      string firstRunSignature;
      using (var firstProvider = CreateServiceProvider(connectionString))
      {
        firstRun = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(firstProvider);
        var completedFirstRunSnapshot = await LoadSnapshotAsync(firstProvider, firstRun.CreatedProjectA.ProjectId);
        firstRunSignature = CreateProjectSnapshotSignature(completedFirstRunSnapshot);
      }

      using var secondProvider = CreateServiceProvider(connectionString);
      var secondRun = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(secondProvider);
      var preservedFirstRunSnapshot = await LoadSnapshotAsync(secondProvider, firstRun.CreatedProjectA.ProjectId);

      Assert.Equal(DocumentImportSessionState.Completed, secondRun.CompletedProjectAImportSession.State);
      Assert.Equal(2, GetImportedSource(secondRun.ProjectASnapshot, secondRun.ImportedSourceA.ImportedSourceId).Candidates.Count);
      Assert.Equal(firstRunSignature, CreateProjectSnapshotSignature(preservedFirstRunSnapshot));
      Assert.Equal(0, secondRun.ProjectASnapshot.AuthorityIsolation.KnowledgeDocumentCount);
      Assert.Equal(0, secondRun.ProjectASnapshot.AuthorityIsolation.ReportCount);
      Assert.Equal(0, secondRun.ProjectASnapshot.AuthorityIsolation.AiProposalCount);
      Assert.NotEqual(firstRun.CreatedProjectA.ProjectId, secondRun.CreatedProjectA.ProjectId);
      Assert.NotEqual(firstRun.ImportedSourceA.ImportedSourceId, secondRun.ImportedSourceA.ImportedSourceId);
      Assert.NotEqual(firstRun.ImportedSourceA.ContentHash, secondRun.ImportedSourceA.ContentHash);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowSucceedsWhenUnrelatedProjectsAndDocumentRecordsAlreadyExist()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString);
      await DocumentEngineExecutableWorkflowBootstrapper.MigrateAsync(serviceProvider);
      var seededProject = await SeedProjectWithImportedSourceAsync(
        serviceProvider,
        "Existing Project",
        "existing-specification.txt",
        "Existing deterministic specification content.",
        completeImportSession: true);
      var seededSnapshotBeforeRun = await LoadSnapshotAsync(serviceProvider, seededProject.ProjectId);

      var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(serviceProvider);
      var seededSnapshotAfterRun = await LoadSnapshotAsync(serviceProvider, seededProject.ProjectId);

      Assert.Equal(CreateProjectSnapshotSignature(seededSnapshotBeforeRun), CreateProjectSnapshotSignature(seededSnapshotAfterRun));
      Assert.Equal(DocumentImportSessionState.Completed, result.CompletedProjectAImportSession.State);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.KnowledgeDocumentCount);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.ReportCount);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.AiProposalCount);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  [Fact]
  public async Task WorkflowSucceedsWhenAPreviousIncompleteDocumentWorkflowExists()
  {
    var databasePath = CreateDatabasePath();
    var connectionString = $"Data Source={databasePath}";

    try
    {
      using var serviceProvider = CreateServiceProvider(connectionString);
      await DocumentEngineExecutableWorkflowBootstrapper.MigrateAsync(serviceProvider);
      var incompleteProject = await SeedProjectWithImportedSourceAsync(
        serviceProvider,
        "Incomplete Project",
        "incomplete-source.txt",
        "Incomplete deterministic import source.",
        completeImportSession: false);
      var incompleteSnapshotBeforeRun = await LoadSnapshotAsync(serviceProvider, incompleteProject.ProjectId);

      var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(serviceProvider);
      var incompleteSnapshotAfterRun = await LoadSnapshotAsync(serviceProvider, incompleteProject.ProjectId);

      Assert.Equal(CreateProjectSnapshotSignature(incompleteSnapshotBeforeRun), CreateProjectSnapshotSignature(incompleteSnapshotAfterRun));
      Assert.Equal(
        DocumentImportSessionState.Importing,
        GetImportSession(incompleteSnapshotAfterRun, incompleteProject.ImportSessionId).State);
      Assert.Equal(DocumentImportSessionState.Completed, result.CompletedProjectAImportSession.State);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.KnowledgeDocumentCount);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.ReportCount);
      Assert.Equal(0, result.ProjectASnapshot.AuthorityIsolation.AiProposalCount);
    }
    finally
    {
      DeleteIfPresent(databasePath);
    }
  }

  private static ProjectDocumentImportSessionSnapshot GetImportSession(
    LoadProjectDocumentWorkflowSnapshotResult snapshot,
    DocumentImportSessionId importSessionId)
  {
    return RequireSingleMatch(
      snapshot.ImportSessions,
      session => session.ImportSessionId == importSessionId,
      $"import session {importSessionId}");
  }

  private static ProjectImportedDocumentSourceSnapshot GetImportedSource(
    LoadProjectDocumentWorkflowSnapshotResult snapshot,
    ImportedSourceId importedSourceId)
  {
    return RequireSingleMatch(
      snapshot.ImportedSources,
      source => source.ImportedSourceId == importedSourceId,
      $"imported source {importedSourceId}");
  }

  private static T RequireSingleMatch<T>(
    IEnumerable<T> items,
    Func<T, bool> predicate,
    string description)
  {
    var matches = items.Where(predicate).Take(2).ToArray();
    return matches.Length switch
    {
      1 => matches[0],
      0 => throw new Xunit.Sdk.XunitException($"Expected exactly one {description}, but none were found."),
      _ => throw new Xunit.Sdk.XunitException($"Expected exactly one {description}, but multiple matches were found."),
    };
  }

  private static string CreateProjectSnapshotSignature(LoadProjectDocumentWorkflowSnapshotResult snapshot)
  {
    return string.Join(
      "|",
      snapshot.ImportSessions
        .OrderBy(session => session.ImportSessionId.Value)
        .Select(session => $"{session.ImportSessionId}:{session.State}:{session.SourceCount}:{session.AcceptedCount}:{session.DuplicateCount}:{session.RejectedCount}:{session.CompletedAtUtc:O}"),
      string.Join(
        "|",
        snapshot.ImportedSources
          .OrderBy(source => source.ImportedSourceId.Value)
          .Select(source =>
            $"{source.ImportedSourceId}:{source.ImportSessionId}:{source.OriginalFileName}:{source.ContentHash}:{source.Status}:{string.Join(",", source.ProcessingAttempts.OrderBy(attempt => attempt.ProcessingAttemptId.Value).Select(attempt => $"{attempt.ProcessingAttemptId}:{attempt.State}:{attempt.FailureClassification}"))}:{string.Join(",", source.Candidates.OrderBy(candidate => candidate.DocumentCandidateId.Value).Select(candidate => $"{candidate.DocumentCandidateId}:{candidate.CandidateType}:{candidate.Status}"))}")));
  }

  private static async Task<LoadProjectDocumentWorkflowSnapshotResult> LoadSnapshotAsync(
    IServiceProvider rootServiceProvider,
    ProjectId projectId)
  {
    await using var scope = rootServiceProvider.CreateAsyncScope();
    var query = scope.ServiceProvider.GetRequiredService<IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult>>();
    return await query.HandleAsync(new LoadProjectDocumentWorkflowSnapshotQuery(projectId, 50, 100, 20, 50, 50));
  }

  private static async Task<SeededDocumentProject> SeedProjectWithImportedSourceAsync(
    IServiceProvider rootServiceProvider,
    string projectName,
    string fileName,
    string content,
    bool completeImportSession)
  {
    await using var scope = rootServiceProvider.CreateAsyncScope();
    var createProject = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProjectCommand, CreateProjectResult>>();
    var beginImportSession = scope.ServiceProvider.GetRequiredService<ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult>>();
    var importSource = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult>>();
    var completeImportSessionCommand = scope.ServiceProvider.GetRequiredService<ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult>>();

    var project = await createProject.HandleAsync(new CreateProjectCommand(projectName));
    var importSession = await beginImportSession.HandleAsync(new BeginDocumentImportSessionCommand(project.ProjectId));
    await using var contentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    var importedSource = await importSource.HandleAsync(new ImportDocumentSourceCommand(
      importSession.ImportSessionId,
      project.ProjectId,
      fileName,
      "text/plain",
      ImportedSourceOrigin.LocalFile,
      null,
      contentStream));

    if (completeImportSession)
    {
      await completeImportSessionCommand.HandleAsync(new CompleteDocumentImportSessionCommand(importSession.ImportSessionId));
    }

    return new SeededDocumentProject(project.ProjectId, importSession.ImportSessionId, importedSource.ImportedSourceId);
  }

  private static string CreateDatabasePath()
  {
    return Path.Combine(Path.GetTempPath(), "spinbuster-tests", $"{Guid.NewGuid():N}.sqlite");
  }

  private static ServiceProvider CreateServiceProvider(
    string connectionString,
    Action<IServiceCollection>? configureServices = null)
  {
    var services = new ServiceCollection();
    DesktopCompositionRoot.ConfigureServices(services, connectionString, new DesktopWorkflowSettings(
      "desktop.bootstrap@local.invalid",
      "Document Engine Executable Slice",
      "Initial Inspection Session",
      "Observed deterministic bootstrap workflow note.",
      "photo-01.jpg",
      "image/jpeg",
      "evidence/photo-01.jpg",
      "sha256:deterministic",
      "Deterministic interpretation summary.",
      "Initial Draft Report",
      "Summary",
      "Deterministic report summary.",
      "Observations",
      "Deterministic report observations.",
      Guid.Parse("0f74d133-75a0-4cf3-9d80-1f66144d96ac"),
      Guid.Parse("5fbbdb98-6e5d-48e8-930c-4da04db60336"),
      "report-draft-proposal-default",
      "0.1.0",
      0.2m,
      SPINbuster.AI.DeterministicAiScenario.Success,
      DesktopAiReviewAction.None,
      "No AI review for document workflow.",
      "Section 03 30 00 - Cast-in-Place Concrete",
      "03 30 00",
      "Concrete",
      "0",
      "Initial issue.",
      "1",
      "Revised curing requirements.",
      "Request for Information 027",
      "RFI-027",
      "Concrete",
      "0",
      "Clarifies the curing sequence.",
      "RFI-027 clarifies the revised curing requirement.",
      "Section 3.6.B",
      "Provide curing protection immediately after finishing.",
      new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero)));
    configureServices?.Invoke(services);
    return services.BuildServiceProvider();
  }

  private static void DeleteIfPresent(string databasePath)
  {
    SqliteConnection.ClearAllPools();

    if (File.Exists(databasePath))
    {
      try
      {
        File.Delete(databasePath);
      }
      catch (IOException)
      {
      }
    }
  }

  private sealed class ThrowingUnitOfWork : IUnitOfWork
  {
    private readonly IUnitOfWork _inner;
    private readonly int _failOnCommitNumber;
    private int _commitAttempts;

    public ThrowingUnitOfWork(IUnitOfWork inner, int failOnCommitNumber)
    {
      _inner = inner;
      _failOnCommitNumber = failOnCommitNumber;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
      _commitAttempts++;
      if (_commitAttempts == _failOnCommitNumber)
      {
        throw new InvalidOperationException("Forced commit failure.");
      }

      await _inner.CommitAsync(cancellationToken);
    }
  }

  private sealed record SeededDocumentProject(
    ProjectId ProjectId,
    DocumentImportSessionId ImportSessionId,
    ImportedSourceId ImportedSourceId);
}
