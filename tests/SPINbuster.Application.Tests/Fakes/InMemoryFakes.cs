using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Repositories;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests.Fakes;

internal sealed class FakeClock : IClock
{
  public FakeClock(DateTimeOffset utcNow)
  {
    UtcNow = utcNow;
  }

  public DateTimeOffset UtcNow { get; set; }
}

internal sealed class FakeCurrentUser : ICurrentUser
{
  public FakeCurrentUser(string userId)
  {
    UserId = userId;
  }

  public string UserId { get; }
}

internal sealed class FakeAuditRecorder : IAuditRecorder
{
  private readonly List<string>? _sharedOperationLog;

  public FakeAuditRecorder(List<string>? sharedOperationLog = null)
  {
    _sharedOperationLog = sharedOperationLog;
  }

  public List<AuditEvent> StagedEvents { get; } = [];

  public List<string> SequenceLog { get; } = [];

  public bool ThrowOnStage { get; set; }

  public void Stage(AuditEvent auditEvent)
  {
    if (ThrowOnStage)
    {
      throw new InvalidOperationException("Audit staging failed.");
    }

    SequenceLog.Add("audit-stage");
    _sharedOperationLog?.Add("audit-stage");
    StagedEvents.Add(auditEvent);
  }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
  private readonly List<string>? _sharedOperationLog;

  public FakeUnitOfWork(List<string>? sharedOperationLog = null)
  {
    _sharedOperationLog = sharedOperationLog;
  }

  public int CommitCount { get; private set; }

  public List<string> SequenceLog { get; } = [];

  public bool ThrowOnCommit { get; set; }

  public Task CommitAsync(CancellationToken cancellationToken = default)
  {
    SequenceLog.Add("commit");
    _sharedOperationLog?.Add("commit");
    if (ThrowOnCommit)
    {
      throw new InvalidOperationException("Commit failed.");
    }

    CommitCount++;
    return Task.CompletedTask;
  }
}

internal sealed class FakeProjectRepository : IProjectRepository
{
  private readonly Dictionary<ProjectId, Project> _projects = [];

  public Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken = default)
  {
    _projects.TryGetValue(projectId, out var project);
    return Task.FromResult(project);
  }

  public Task AddAsync(Project project, CancellationToken cancellationToken = default)
  {
    _projects[project.Id] = project;
    return Task.CompletedTask;
  }

  public List<Project> UpdatedProjects { get; } = [];

  public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
  {
    _projects[project.Id] = project;
    UpdatedProjects.Add(project);
    return Task.CompletedTask;
  }
}

internal sealed class FakeInspectionSessionRepository : IInspectionSessionRepository
{
  private readonly Dictionary<InspectionSessionId, InspectionSession> _inspectionSessions = [];

  public Task<InspectionSession?> GetByIdAsync(
    InspectionSessionId inspectionSessionId,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions.TryGetValue(inspectionSessionId, out var inspectionSession);
    return Task.FromResult(inspectionSession);
  }

  public Task AddAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions[inspectionSession.Id] = inspectionSession;
    return Task.CompletedTask;
  }

  public List<InspectionSession> UpdatedInspectionSessions { get; } = [];

  public Task UpdateAsync(
    InspectionSession inspectionSession,
    CancellationToken cancellationToken = default)
  {
    _inspectionSessions[inspectionSession.Id] = inspectionSession;
    UpdatedInspectionSessions.Add(inspectionSession);
    return Task.CompletedTask;
  }
}

internal sealed class FakeReportRepository : IReportRepository
{
  private readonly Dictionary<ReportId, Report> _reports = [];

  public Task<Report?> GetByIdAsync(ReportId reportId, CancellationToken cancellationToken = default)
  {
    _reports.TryGetValue(reportId, out var report);
    return Task.FromResult(report);
  }

  public Task AddAsync(Report report, CancellationToken cancellationToken = default)
  {
    _reports[report.Id] = report;
    return Task.CompletedTask;
  }
}

internal sealed class FakeSaveTransactionRepository : ISaveTransactionRepository
{
  private readonly Dictionary<SaveTransactionId, SaveTransaction> _saveTransactions = [];

  public Task<SaveTransaction?> GetByIdAsync(
    SaveTransactionId saveTransactionId,
    CancellationToken cancellationToken = default)
  {
    _saveTransactions.TryGetValue(saveTransactionId, out var saveTransaction);
    return Task.FromResult(saveTransaction);
  }

  public Task AddAsync(
    SaveTransaction saveTransaction,
    CancellationToken cancellationToken = default)
  {
    _saveTransactions[saveTransaction.Id] = saveTransaction;
    return Task.CompletedTask;
  }
}
