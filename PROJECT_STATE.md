# PROJECT_STATE

## Latest Governance Baseline

- `ARCHITECTURE-VISION-2.0`
- Status: `Released`

## Latest Software Baseline

- `PARSING-AND-FRAGMENT-FOUNDATION-0.1`
- Status: `Released`

## Active Implementation Package

- `PARSING-EXECUTABLE-SLICE-0.1-RC`

## Current Branch

- `main`

## Last Completed Milestone

- Parsing and fragment foundation released

## Current Implementation Phase

- Document Understanding — parsing foundation complete

## Current Milestone

- Prototype Vertical Slice

## Test Status

- Domain: `152/152`
- Application: `156/156`
- Documents: `28/28`
- Infrastructure: `42/42`
- Architecture: `24/24`
- Desktop: `34/34`
- Total: `442/442`

## Open ADRs

- None recorded yet

## Open EDRs

- `EDR-DOM-001` Versioned evidence interpretation history (`Deferred`)
- `EDR-APP-001` Application command idempotency (`Accepted for REPORT-DRAFT-SLICE-0.1`)
- `EDR-APP-002` Draft-generation ownership (`Accepted for APPLICATION-0.1`)
- `EDR-AI-001` Authoritative report revision creation from accepted AI proposals (`Deferred`)
- `EDR-AI-002` AI proposal request idempotency and recovery semantics (`Deferred`)
- `EDR-KE-001` Binary file storage ownership (`Deferred`)
- `EDR-KE-002` Document parsing and chunking (`Deferred`)
- `EDR-KE-003` OCR boundary (`Deferred`)
- `EDR-KE-004` Embeddings and vector search (`Deferred`)
- `EDR-KE-005` Automatic authority classification (`Deferred`)
- `EDR-KE-006` AI-generated relationship promotion (`Deferred`)
- `EDR-KE-007` Cross-project knowledge sharing (`Deferred`)
- `EDR-KE-008` Multi-current-revision conflict resolution (`Deferred`)
- `EDR-KE-009` Knowledge command idempotency (`Deferred`)
- `EDR-KE-010` Knowledge fragment identity (`Resolved in PARSING-AND-FRAGMENT-FOUNDATION-0.1`)
- `EDR-KE-011` Engineering assertion promotion (`Deferred`)
- `EDR-KE-012` Document Engine ownership boundary (`Accepted`)

## Outstanding Technical Debt

- `SPINbuster.Desktop` is still a temporary bootstrap host and has not been replaced with a real MAUI client.
- Most test projects are still empty scaffolds outside of `SPINbuster.Architecture.Tests`.
- Human-accepted AI proposals do not yet create authoritative report revisions.
- Knowledge Engine command idempotency is still deferred and must be resolved before synchronization-oriented work.
- The `MapFailureClassification` in `RequestDocumentParsingUseCase` loses original parser-specific classification; should be refined before production.
- Document OCR, fragment promotion, assertion promotion, and broader retrieval remain deferred beyond the current foundation.
- Immutable-object reconciliation and deletion remain deferred; local filesystem inventory is diagnostic only.
- The Windows Desktop apphost may still be blocked by local machine policy even when the managed DLL executes correctly.

## Immediate Next Task

- Begin `PARSING-EXECUTABLE-SLICE-0.1-RC`

## Fast Context

- The repository is the source of truth for project state and architecture.
- Start every new AI session from `.ai/bootstrap.md`.
- The latest governance baseline is `ARCHITECTURE-VISION-2.0`.
- The latest software baseline is `PARSING-AND-FRAGMENT-FOUNDATION-0.1`.
- The active implementation package is `PARSING-EXECUTABLE-SLICE-0.1-RC`.

## Current Capabilities

- Current released capabilities include `PARSING-AND-FRAGMENT-FOUNDATION-0.1`
- Deterministic text parsing produces fragment candidates with reproducible identity
- Parser runs, fragment candidates, and audit history persist through SQLite and survive provider recreation
- Parser version coexistence preserves historical candidates
- Parsing does not widen Knowledge, Report, or AI authority boundaries
