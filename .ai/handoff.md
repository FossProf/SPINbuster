# Current State

Repository status:
Latest governance baseline: `ARCHITECTURE-VISION-2.0`. Latest software baseline: `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`. Active implementation package: `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` (release candidate, not released). Build passing. Domain tests `181/181`. Application tests `184/184`. Documents tests `63/63`. Infrastructure tests `61/61`. Architecture tests `24/24`. AI tests `6/6`. Desktop tests `45/45`. Total `564/564`. Warnings `0` (pre-existing CA1848 acknowledged).

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Latest governance baseline:
`ARCHITECTURE-VISION-2.0`

Latest software baseline:
`FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`

Active implementation package:
`DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC`

Recent accomplishments:

- Released `PARSING-AND-FRAGMENT-FOUNDATION-0.1` with executable proof validated.
- Released `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1` with full review lifecycle.
- Implemented Domain types (ParserEngine, FragmentIds) with parser-run lifecycle, fragment-candidate identity, locator model, and audit events.
- Added Application layer: IDocumentParser port, RequestDocumentParsingUseCase, LoadParsingSnapshotUseCase, DI, logging.
- Implemented deterministic PlainTextDocumentParser adapter with EF Core SQLite persistence.
- Hardened replay key to 5-column unique index: (ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash).
- Created Desktop executable workflow: ParsingExecutableWorkflowRunner, Bootstrapper, ConsoleFormatter.
- Added 11 Desktop tests covering parsing, idempotent replay, version coexistence, failure handling, and authority isolation.
- Resolved EDR-KE-010: candidate-stage fragment identity derived from `{ImportedSourceId}:{ParserKey}@{ParserContractVersion}:{LocatorType}:{NormalizedLocatorValue}`.
- Created prototype review document at `docs/decisions/status/PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC-PROTOTYPE-REVIEW.md`.
- Completed `FRAGMENT-INTEGRITY-HARDENING-CHECKPOINT`: fixed `FragmentCandidate.Rehydrate` to restore persisted state directly without invalid placeholders, added `ValidateRehydratedState` with 12 invariant checks, documented contract-version identity choice in `EDR-DE-006`.
- Completed `FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT` (Prompt 1): implemented `FragmentCandidateReviewState` enum (Generated, HumanAccepted, Rejected), added `Accept()` and `Reject()` methods with audit events and terminal state guards, added review disposition properties, updated `InfrastructureMapper` and `SpinbusterModelConfiguration`, created EF migration `AddFragmentCandidateReviewState`, updated spec, created `EDR-DE-007`, added 20 review lifecycle domain tests.
- Completed `FRAGMENT-REVIEW-APPLICATION-CHECKPOINT` (Prompt 2): implemented `AcceptFragmentCandidateUseCase` and `RejectFragmentCandidateUseCase` commands with audit staging, commit, and structured logging; implemented `LoadFragmentReviewSnapshotUseCase` query with bounded text preview, review metadata, and filter support; extended `IFragmentCandidateRepository` with `GetByIdAsync`, `UpdateAsync`, `GetByProjectAsync`; implemented in `SqliteFragmentCandidateRepository`; registered in `ServiceCollectionExtensions`; added 28 Application tests.
- Completed `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC` (Prompt 4): extended Desktop executable proof with full review lifecycle, 2-source import, parser version coexistence, 4 expected failure scenarios, authority isolation verification; fixed EF Core tracking conflict in `SqliteFragmentCandidateRepository.UpdateAsync`; created prototype review document.
- Completed `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 1: Domain & Application substrate — `ParserExecutionStatus` enum, `DiagnosticSeverity` enum, `DiagnosticRefType` enum, `ParserDiagnostic` immutable entity, `ParserDiagnosticId`, `IParserDiagnosticRepository`, `DocumentParserRegistry`, `ParserDescriptor`, updated `ParserExecutionResult` with diagnostics, updated `RequestDocumentParsingUseCase` to resolve via registry and persist diagnostics, updated `LoadParsingSnapshotUseCase` and `LoadFragmentReviewSnapshotUseCase` to load diagnostics, created `AddParserDiagnostics` EF migration (10 total), `ParserDiagnosticRecord` with EF Core mapping, registered repositories in DI, added 25 domain tests and 25 Application tests.
- Completed `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 2: Implemented `StructuredTextDocumentParser` with heading extraction (`#{1,6}`), numbered/lettered clause extraction, pipe-delimited table extraction, line-range overlap detection, `OVERLAPPING_CONTENT` diagnostic emission, contract hash via SHA-256. Registered as `IDocumentParser` singleton. Added `.md` to `text/markdown` extension mapping. Added 25 structured text parser tests.
- Completed `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 3: Extended `ParsingExecutableWorkflowRunner` with structured text source import and parsing. Updated `ParsingExecutableWorkflowResult` with structured text fields. Added diagnostics display to `ParsingExecutableWorkflowConsoleFormatter`. Added 5 infrastructure persistence tests for `parser_diagnostics` table. Added 6 Desktop integration tests for structured text parsing, diagnostics round-trip, and restart validation.
- Completed `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 5: Governance & RC Review — created prototype review document, updated all governance and continuity files, validated format/build/test (564/564 passing), left repository at release candidate without release.
- All 564 tests passing.

Current architectural decisions:

- `ARCHITECTURE-VISION-2.0` is the active governance baseline.
- `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1` is the latest released software baseline.
- `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` is validated as release candidate but not released.
- Fragment identity is parser-run-scoped, not revision-stable (EDR-KE-010 resolved).
- Fragment identity uses contract version, not implementation version (EDR-DE-006 accepted).
- Fragment candidate review uses terminal disposition model: Generated -> HumanAccepted or Rejected (EDR-DE-007 accepted).
- `HumanAccepted` is necessary but insufficient for Knowledge promotion (EDR-DE-007 accepted).
- Replay key is 5-column: `(ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash)`.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- The Document Engine owns binary-source handling and non-authoritative processing outputs only.
- The Desktop host composes document workflow behavior through Application commands and queries only.
- Parser adapters are registered as `IDocumentParser` singletons in the Documents adapter layer.
- Parser diagnostics are immutable, not independently auditable, no review lifecycle (EDR-DE-008 accepted).
- Parser execution status uses Completed/CompletedWithWarnings/Failed instead of boolean success.
- The Rule Engine will remain deterministic and separate from AI recommendations.
- Knowledge Engine command idempotency is still deferred by `EDR-KE-009`.

Next task:
Await release decision for `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` or begin `FRAGMENT-TO-KNOWLEDGE-PROMOTION-FOUNDATION-0.1-RC`

Known issues:

- Most non-architecture test projects are still empty scaffolds and intentionally have no real test cases yet.
- The Desktop host is still a console bootstrapper and should not accumulate broader UI assumptions before the real client direction is chosen.
- Human-accepted AI proposals still do not create authoritative report revisions; that boundary remains deferred by `EDR-AI-001`.
- Knowledge Engine command idempotency is still deferred and must be designed before synchronization or automated ingestion work.
- Document OCR, fragment promotion, assertion promotion, and broader retrieval remain deferred beyond the current foundation.
- Reconciliation and deletion for orphaned immutable filesystem objects remain deferred intentionally.
- The generated Windows Desktop apphost may still be blocked by local machine policy even when the managed DLL runs correctly; treat that as environmental for the temporary host.
- The `MapFailureClassification` in `RequestDocumentParsingUseCase` maps parser failure reasons through string matching, which loses the original parser-specific classification. Acceptable for the foundation but should be refined before production.
- `AddKnowledgeCitationUseCase` retains direct `new AuditEvent(...)` construction as intentional single-event duplication, not a general pattern for other use cases.
- The EF migration `AddFragmentCandidateReviewState` was created during the Domain checkpoint (Prompt 1) before Application review workflows were finalized. Treat this as `FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT`. Do not create another migration unless the model genuinely changes.
- Fragment candidate review concurrency relies on aggregate-level guards (`EnsureReviewNotDecided`). Before server or multi-user work, the database update itself should verify original state with a conditional SQL `WHERE ReviewState = Generated` to ensure true multi-process safety.
- Pre-existing CA1848 warnings throughout Application use cases (LoggerMessage delegates) are acknowledged technical debt.

Requested review:

- Approved. No release blockers.
- Recommend next package after approval

Current capabilities:

- Current released code behavior includes `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`
- The repository contains an authoritative conceptual engineering knowledge model
- The repository includes the released durable Document Engine foundation
- The repository includes a deterministic executable Document Engine workflow
- The repository includes a local filesystem immutable content store
- The repository includes a released parsing and fragment foundation with executable proof
- Deterministic text parsing produces fragment candidates with reproducible identity
- Parser runs, fragment candidates, and audit history persist through SQLite and survive provider recreation
- Parser version coexistence preserves historical candidates
- Unsupported media, cancelled, and malformed content produce terminal failure states without crashing
- Parsing does not widen Knowledge, Report, or AI authority boundaries
- Fragment candidate rehydration restores persisted state directly with invariant validation
- Fragment candidates support review lifecycle with Accept/Reject terminal states
- Review disposition is audit-tracked and persisted with fragment candidates
- Application Accept/Reject commands stage new audit events and commit in one transaction
- LoadFragmentReviewSnapshot returns bounded text preview, review metadata, and filter support
- Review does not mutate identity, provenance, or locator properties
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

Released baselines (chronological):

1. `VERTICAL-SLICE-0.1`
2. `APPLICATION-0.1`
3. `INFRASTRUCTURE-0.1`
4. `AI-DRAFT-PROPOSAL-SLICE-0.1`
5. `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`
6. `KNOWLEDGE-ENGINE-PERSISTENCE-0.1`
7. `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1`
8. `DOCUMENT-ENGINE-FOUNDATION-0.1`
9. `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1`
10. `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1`
11. `PARSING-AND-FRAGMENT-FOUNDATION-0.1`
12. `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`

Release candidates (validated, not released):

1. `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC`
