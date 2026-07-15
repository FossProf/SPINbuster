# EDR-APP-001

Title: Application command idempotency

Status: Deferred

Required before:

- mobile retries
- synchronization
- distributed command processing

Context:

The current Application baseline executes commands without request identifiers or
deduplication keys. This is acceptable for the local single-user baseline but is
not sufficient once retries or eventually connected clients are introduced.

Current rule:

- `CreateProject`
- `StartInspectionSession`
- `CaptureFieldNote`
- `PrepareTransactionalSave`

do not yet accept explicit idempotency identifiers.

Decision:

Defer command idempotency until the first synchronization or mobile-retry slice.

Consequences:

- Command handlers remain simpler for `APPLICATION-0.1`.
- Infrastructure and transport contracts must not assume duplicate-safe retries yet.
- Idempotency must be designed before synchronization-aware workflows are added.
