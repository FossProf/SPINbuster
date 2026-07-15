# VERTICAL-SLICE-0.1 Prototype Review

Date: `2026-07-15`  
Baseline: `VERTICAL-SLICE-0.1`  
Status: `Reviewed after release`

## What The Vertical Slice Validated

- The repository can compose a real executable path from the temporary Desktop host through Application and Domain into SQLite-backed Infrastructure.
- Startup migration application works on the intended local SQLite path.
- `CreateProject`, `StartInspectionSession`, and `CaptureFieldNote` succeed through the released handler pipeline and persist authoritative state.
- Persisted `Project` and `InspectionSession` aggregates can be reloaded through Application-facing contracts rather than direct host access to EF models.
- Audit facts persist and reload alongside aggregate state.
- The current layered dependency rules remain intact while still allowing a complete local executable slice.

## Defects Uncovered

- `StartInspectionSessionUseCase` originally staged the session-start audit event but failed to persist the newly created session's creation audit event in the same commit.
  Resolution: fixed before release by staging the full new-session audit slice.
- `SqliteInspectionSessionRepository` originally emitted EF multiple-collection include warnings during session rehydration.
  Resolution: fixed before release by switching the collection loads to split queries.
- Desktop end-to-end tests originally hit transient Windows SQLite file-lock cleanup failures.
  Resolution: fixed before release by making test cleanup pool-aware and best-effort.

## Architectural Assumptions Confirmed

- The current inward-pointing reference graph is sufficient for a real local executable slice.
- Application orchestration can remain free of EF Core, HTTP, file-system, and UI concerns while still supporting an executable host.
- Audit staging before `IUnitOfWork.CommitAsync()` is the correct boundary for the current local SQLite path.
- Detached aggregate update semantics are workable for the current repository contracts and Infrastructure implementation.
- Domain rehydration through narrow internal hooks is sufficient for the current persistence slice and does not require reflection-based reconstruction.
- A read-only Application query is an acceptable boundary for host-facing persisted-state reloads after command commits.

## Friction Observed

### Dependency Injection

- Manual host wiring was manageable after introducing `AddSpinbusterApplication()` and `AddSpinbusterSqliteInfrastructure()`.
- Composition is still repetitive enough that additional vertical slices should continue to prefer shared registration helpers over per-host manual wiring.

### Rehydration

- Rehydration works, but it depends on Infrastructure-specific mapping discipline and the internal Domain rehydration hooks remaining narrow.
- Audit-trail reconstruction is currently repeated per aggregate repository and may eventually benefit from shared query helpers if more aggregates begin loading audit history.

### Migrations

- Migration execution at startup behaved correctly for the local slice.
- The runtime output is still verbose because EF command logging is visible in the console host; acceptable for a bootstrap prototype, but worth deciding deliberately before broader user-facing workflows.

### Audit Staging

- The slice confirmed the value of staging audit events before commit.
- The uncovered missing-session-creation audit bug shows that any handler creating a new aggregate must deliberately stage the full audit slice, not only post-mutation deltas.

### Query Composition

- The persisted-state reload query is clean at the Application boundary, but it adds another read-model-shaping path that must stay distinct from command-side mutation logic.
- Multi-collection aggregate rehydration in EF requires explicit query-shape choices; split-query configuration is now part of the practical guidance for similar repositories.

## Deferred Decisions Becoming More Urgent

- `EDR-APP-001` Command idempotency is becoming more urgent because the executable slice makes retry behavior and duplicate-safe command handling more concrete.
- `ICurrentUser` remaining a raw `string` is still acceptable for this baseline, but a typed application identity is becoming a stronger candidate before broader workflow growth.
- `EDR-DOM-001` Versioned evidence interpretation history remains deferred and is not yet urgent for the released slice.

## Desktop Host Assessment

- The temporary Desktop host is still adequate for the next slice if the next work stays focused on orchestration and local executable validation.
- It is not adequate as a long-term UI direction and should not accumulate richer presentation assumptions, state management, or client-specific behavior.
- The next slice should continue to treat it as a deterministic bootstrap host, not as the future Desktop application architecture.

## Recommended Next Step

- Define the next implementation package using the released vertical slice as the baseline.
- Prefer expanding one narrow workflow at a time while keeping the Desktop host thin and disposable.
