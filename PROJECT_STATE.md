# PROJECT_STATE

## Latest Governance Baseline

- `ARCHITECTURE-VISION-2.0`
- Status: `Released`

## Latest Software Baseline

- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Status: `Released`

## Active Documentation-Only Governance Package

- `ENGINEERING-WORKFLOW-STANDARD-1.0-RC`
- Status: `Installed on review branch` (awaiting architectural adoption approval, not released)
- Installs `docs/00-governance/ENGINEERING_WORK_ORDER_STANDARD.md`; it is not normative until this governance package is approved and released

## Planned Implementation Package

- `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Status: `Planned` (implementation has not begun)
- Planning package: `docs/03-implementation/work-orders/ENGINEERING-ASSERTION-PROMOTION-0.1-RC.md`

## Current Branch

- `main`

## Last Completed Milestone

- Engineering Work Order Standard v1.0 installed on the review branch (documentation-only governance package awaiting approval); Knowledge promotion vertical slice released

## Current Implementation Phase

- Governance package review — Engineering Work Order Standard installed on review branch awaiting architectural adoption approval; Engineering Assertion Promotion planned (not begun)

## Current Milestone

- Prototype Vertical Slice

## Test Status

- Domain: `234/234`
- Application: `233/233`
- Documents: `78/78`
- Infrastructure: `104/104`
- Architecture: `24/24`
- AI: `6/6`
- Desktop: `75/75`
- Total: `754/754`

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
- `EDR-DE-006` Fragment identity contract-version choice (`Accepted`)
- `EDR-DE-007` Fragment candidate review disposition and promotion prerequisites (`Accepted`)
- `EDR-DE-008` Parser diagnostics and registry (`Accepted for DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC`)

## Outstanding Technical Debt

- `SPINbuster.Desktop` is still a temporary bootstrap host and has not been replaced with a real MAUI client.
- Human-accepted AI proposals do not yet create authoritative report revisions.
- Knowledge Engine command idempotency is still deferred and must be resolved before synchronization-oriented work.
- The `MapFailureClassification` in `RequestDocumentParsingUseCase` loses original parser-specific classification; should be refined before production.
- Document OCR, assertion promotion, and broader retrieval remain deferred beyond the current foundation.
- Immutable-object reconciliation and deletion remain deferred; local filesystem inventory is diagnostic only.
- The Windows Desktop apphost may still be blocked by local machine policy even when the managed DLL executes correctly.
- Fragment candidate review concurrency relies on aggregate-level guards (`EnsureReviewNotDecided`). Before server or multi-user work, the database update itself should verify original state with a conditional SQL `WHERE ReviewState = Generated` to ensure true multi-process safety.
- Pre-existing CA1848 warnings throughout Application use cases (LoggerMessage delegates) are acknowledged technical debt.
- Knowledge promotion concurrency relies on `PromotionIdentity` uniqueness. No optimistic concurrency token prevents two simultaneous promotions from both passing the idempotency check. Acceptable for single-user foundation; required before server or multi-user work.
- SQLite filtered unique index workaround is handled by the single atomic `UnitOfWork` transaction in `SupersedeCurrentRevision` (the sole Domain supersession operation). May be simplified if migrating to a database that supports deferred constraint checking.

## Immediate Next Task

- `ENGINEERING-WORKFLOW-STANDARD-1.0-RC` documentation installation awaiting architectural adoption approval. `ENGINEERING-ASSERTION-PROMOTION-0.1-RC` is planned but has not begun; do not start WO0 until explicitly directed.

## Fast Context

- The repository is the source of truth for project state and architecture.
- Start every new AI session from `.ai/bootstrap.md`.
- The latest governance baseline is `ARCHITECTURE-VISION-2.0`.
- The latest software baseline is `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`.
- The documentation-only governance package is `ENGINEERING-WORKFLOW-STANDARD-1.0-RC` (Engineering Work Order Standard installed on the review branch, awaiting architectural adoption approval; not released).
- The planned implementation package is `ENGINEERING-ASSERTION-PROMOTION-0.1-RC` (planned, implementation not begun).

## Current Capabilities

- Current released capabilities include `PARSING-AND-FRAGMENT-FOUNDATION-0.1`, `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`, and `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Deterministic text parsing produces fragment candidates with reproducible identity
- Parser runs, fragment candidates, and audit history persist through SQLite and survive provider recreation
- Parser version coexistence preserves historical candidates
- Parsing does not widen Knowledge, Report, or AI authority boundaries
- Domain-level fragment candidate review lifecycle: Generated, HumanAccepted, Rejected
- Application-layer Accept/Reject commands for fragment candidate review with audit staging and commit
- Bounded FragmentReviewSnapshot query with text preview, review metadata, and filter support
- Application review workflows do not create Knowledge, Report, Rule, or AI records
- Desktop executable proof exercises full review lifecycle with 2-source import and version coexistence
- First-commit-wins concurrency and terminal state guards prevent conflicting updates
- Authority isolation verified: parsing and review do not create Knowledge, Report, or AI records
- Structured text parsing extracts headings, numbered clauses, lettered clauses, and pipe-delimited tables
- Parser diagnostics model: ParserDiagnostic with severity, code, message, and candidate reference
- Overlapping-fragment policy: line-range overlap detection with OVERLAPPING_CONTENT diagnostic
- Parser registry: DocumentParserRegistry resolves parser by key from DI
- Parser execution status: Completed, CompletedWithWarnings, Failed (replaces boolean success)
- Diagnostic persistence: parser_diagnostics table with full round-trip through SQLite
- Desktop executable proof exercises structured text parsing with diagnostics display
- Knowledge Promotion: human-reviewed fragment candidates can be promoted into authoritative KnowledgeDocument, KnowledgeDocumentRevision, KnowledgeCitation, and KnowledgeRelationship records
- Promotion precondition checklist enforced deterministically (no AI participation in authority decisions)
- PromotionIdentity-based idempotent replay (candidate-ID or content-hash replay removed, WO4 hardened)
- Single atomic UnitOfWork supersession (`SupersedeCurrentRevision` is the sole Domain supersession operation)
- Promotion diagnostics are durable and queryable (Eligible/Promoted/Failed lifecycle)
- Project lifecycle management: Draft -> Active (required for promotion eligibility)
- Knowledge snapshot survives provider disposal and recreation
- End-to-end executable proof: create project -> activate -> import -> parse -> review -> promote -> supersede -> verify snapshot
- Persisted attempt history: Desktop scenario loads real promotion attempts across retries
- Attempt/diagnostic ownership: SQLite integration tests verify attempt-owned diagnostic history, indexed and queryable by candidate, with PromotionIdentity-only successful replay
- Canonical identity migration: unique index, duplicate detection, cross-project isolation verified
- Released migration integrity: 17 migrations apply cleanly, snapshot consistency verified
- AI-parsed supersession blocked at equal authority via IAuthorityPolicy (Informational classification)
