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
    UserId = new ApplicationUserId(userId);
  }

  public ApplicationUserId UserId { get; }
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

internal sealed class FakeAuditEventQueryRepository : IAuditEventQueryRepository
{
  private readonly FakeAuditRecorder _auditRecorder;

  public FakeAuditEventQueryRepository(FakeAuditRecorder auditRecorder)
  {
    _auditRecorder = auditRecorder;
  }

  public Task<IReadOnlyCollection<AuditEvent>> GetBySubjectAsync(
    string subjectType,
    string subjectId,
    CancellationToken cancellationToken = default)
  {
    return Task.FromResult<IReadOnlyCollection<AuditEvent>>(
      _auditRecorder.StagedEvents
        .Where(auditEvent => auditEvent.SubjectType == subjectType && auditEvent.SubjectId == subjectId)
        .OrderBy(auditEvent => auditEvent.OccurredAtUtc)
        .ThenBy(auditEvent => auditEvent.Id.ToString(), StringComparer.Ordinal)
        .ToArray());
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
