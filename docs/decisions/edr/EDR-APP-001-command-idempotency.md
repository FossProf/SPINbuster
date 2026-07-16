# EDR-APP-001

Title: Application command idempotency

Status: Accepted for REPORT-DRAFT-SLICE-0.1

Required before:

- mobile retries
- synchronization
- distributed command processing

Context:

The Application baseline originally executed commands without request identifiers
or deduplication keys. That was acceptable for the first local single-user
baseline but is not sufficient once authoritative report creation and retryable
command paths are introduced.

Current rule:

- `CreateReportDraftCommand` accepts an explicit `OperationId`.
- The report repository persists an operation-to-report mapping.
- Infrastructure enforces uniqueness of that operation mapping so duplicate
  retries do not create a second authoritative draft.
- Earlier commands such as `CreateProject`, `StartInspectionSession`,
  `CaptureFieldNote`, and `PrepareTransactionalSave` do not yet accept explicit
  idempotency identifiers.

Decision:

Adopt command idempotency first at the authoritative report-draft boundary, then
extend the pattern to earlier commands before synchronization or mobile retry
work begins.

Consequences:

- `CreateReportDraftCommand` is safe to retry using the same `OperationId`.
- Application contracts now have a concrete idempotency pattern that later
  commands can follow consistently.
- Infrastructure and transport contracts must not yet assume duplicate-safe
  retries for commands that still lack an `OperationId`.
