# PROJECT_STATE

## Latest Released Baseline

- `DOCUMENT-ENGINE-FOUNDATION-0.1`
- Status: `Released`

## Active Review Candidate

- `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`
- Status: `Active`
- Build: `Passing`
- Warnings: `0`
- Architecture tests: `17/17 passing`
- Domain tests: `52/52 passing`
- Application tests: `70/70 passing`
- Documents tests: `5/5 passing`
- AI tests: `6/6 passing`
- Infrastructure tests: `27/27 passing`
- Desktop end-to-end tests: `6/6 passing`

## Next Planned Implementation Package

- `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`

## Current Branch

- `main`

## Last Completed Milestone

- Document Engine foundation released

## Current Implementation Phase

- Document Engine executable slice planning

## Current Milestone

- Prototype Vertical Slice

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
- `EDR-KE-010` Knowledge fragment identity (`Deferred`)
- `EDR-KE-011` Engineering assertion promotion (`Deferred`)
- `EDR-KE-012` Document Engine ownership boundary (`Accepted`)

## Outstanding Technical Debt

- `SPINbuster.Desktop` is still a temporary bootstrap host and has not been replaced with a real MAUI client.
- Most test projects are still empty scaffolds outside of `SPINbuster.Architecture.Tests`.
- Human-accepted AI proposals do not yet create authoritative report revisions.
- Knowledge Engine command idempotency is still deferred and must be resolved before synchronization-oriented work.
- Document parsing, OCR, fragment promotion, assertion promotion, and broader retrieval remain conceptual only in the current package.

## Immediate Next Task

- Implement `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`

## Fast Context

- The repository is the source of truth for project state and architecture.
- Start every new AI session from `.ai/bootstrap.md`.
- The current released behavior includes `DOCUMENT-ENGINE-FOUNDATION-0.1`.
- The next active package is the first executable Document Engine slice.
- The next planned implementation package is `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`.

## Current Capabilities

- Current released capabilities remain unchanged from `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1`
- The repository now includes a durable Document Engine foundation with immutable source identity, processing attempts, and non-authoritative candidates
