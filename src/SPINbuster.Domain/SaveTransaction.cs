namespace SPINbuster.Domain;

public enum SaveTransactionState
{
  Created = 0,
  Prepared = 1,
  Persisted = 2,
  Committed = 3,
  Failed = 4,
  Aborted = 5,
}

public sealed class SaveTransaction : AuditableEntity
{
  public SaveTransaction(
    SaveTransactionId id,
    ReportId reportId,
    string initiatedBy,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ReportId = reportId;
    InitiatedBy = DomainGuards.NotNullOrWhiteSpace(initiatedBy, nameof(initiatedBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    State = SaveTransactionState.Created;

    AppendAuditEvent(CreateAuditEvent("SaveTransactionCreated", initiatedBy, createdAtUtc, "Save transaction created."));
  }

  public SaveTransactionId Id { get; }

  public ReportId ReportId { get; }

  public string InitiatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public SaveTransactionState State { get; private set; }

  public string? FailureReason { get; private set; }

  public DateTimeOffset? PreparedAtUtc { get; private set; }

  public DateTimeOffset? PersistedAtUtc { get; private set; }

  public DateTimeOffset? CompletedAtUtc { get; private set; }

  internal static SaveTransaction Rehydrate(
    SaveTransactionId id,
    ReportId reportId,
    string initiatedBy,
    DateTimeOffset createdAtUtc,
    SaveTransactionState state,
    string? failureReason,
    DateTimeOffset? preparedAtUtc,
    DateTimeOffset? persistedAtUtc,
    DateTimeOffset? completedAtUtc,
    IEnumerable<AuditEvent> auditTrail)
  {
    var saveTransaction = new SaveTransaction(id, reportId, initiatedBy, createdAtUtc)
    {
      State = state,
      FailureReason = failureReason,
      PreparedAtUtc = preparedAtUtc,
      PersistedAtUtc = persistedAtUtc,
      CompletedAtUtc = completedAtUtc,
    };

    saveTransaction.RestoreAuditTrail(auditTrail);
    return saveTransaction;
  }

  public void Prepare(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureState(SaveTransactionState.Created, nameof(Prepare));

    State = SaveTransactionState.Prepared;
    PreparedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("SaveTransactionPrepared", actor, occurredAtUtc, "Save transaction prepared."));
  }

  public void Persist(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureState(SaveTransactionState.Prepared, nameof(Persist));

    // Persisted means the write set was applied. Commit remains a separate
    // step so callers can model post-write verification or confirmation.
    State = SaveTransactionState.Persisted;
    PersistedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("SaveTransactionPersisted", actor, occurredAtUtc, "Save transaction persisted."));
  }

  public void Commit(string actor, DateTimeOffset occurredAtUtc)
  {
    EnsureState(SaveTransactionState.Persisted, nameof(Commit));

    State = SaveTransactionState.Committed;
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("SaveTransactionCommitted", actor, occurredAtUtc, "Save transaction committed."));
  }

  public void Fail(string actor, DateTimeOffset occurredAtUtc, string reason)
  {
    if (State is SaveTransactionState.Committed or SaveTransactionState.Aborted or SaveTransactionState.Failed)
    {
      throw new LifecycleTransitionException(nameof(SaveTransaction), State.ToString(), nameof(Fail));
    }

    State = SaveTransactionState.Failed;
    FailureReason = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("SaveTransactionFailed", actor, occurredAtUtc, $"Save transaction failed: {FailureReason}."));
  }

  public void Abort(string actor, DateTimeOffset occurredAtUtc, string reason)
  {
    if (State is SaveTransactionState.Committed or SaveTransactionState.Aborted or SaveTransactionState.Failed)
    {
      throw new LifecycleTransitionException(nameof(SaveTransaction), State.ToString(), nameof(Abort));
    }

    State = SaveTransactionState.Aborted;
    FailureReason = DomainGuards.NotNullOrWhiteSpace(reason, nameof(reason));
    CompletedAtUtc = DomainGuards.NotDefault(occurredAtUtc, nameof(occurredAtUtc));
    AppendAuditEvent(CreateAuditEvent("SaveTransactionAborted", actor, occurredAtUtc, $"Save transaction aborted: {FailureReason}."));
  }

  private void EnsureState(SaveTransactionState expectedState, string transitionName)
  {
    if (State != expectedState)
    {
      throw new LifecycleTransitionException(nameof(SaveTransaction), State.ToString(), transitionName);
    }
  }

  private AuditEvent CreateAuditEvent(
    string eventType,
    string actor,
    DateTimeOffset occurredAtUtc,
    string description)
  {
    return new AuditEvent(
      AuditEventId.New(),
      nameof(SaveTransaction),
      Id.ToString(),
      eventType,
      actor,
      occurredAtUtc,
      description);
  }
}
