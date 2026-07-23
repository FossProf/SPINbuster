# Implementation Log

2026-07-16

Completed:
- Engineering object model and specification index
- Document Engine durable foundation
- Immutable source identity, import sessions, processing attempts, and candidate persistence
- SQLite migration and verification for the Document Engine foundation

Next:
- Document Engine executable slice

## 2026-07-17

Completed:

- Document Engine executable Desktop workflow
- Multi-source batch import through one deterministic import session
- Project-scoped document workflow snapshot query
- Deterministic document processing outcome persistence and reload
- Non-authoritative candidate review persistence
- Desktop Application-only composition hardening
- Infrastructure database migrator abstraction for host startup
- Audit-delta staging fix for repeated document aggregate mutations
- SQLite document query-shaping hardening for `DateTimeOffset` ordering
- Desktop tests for duplicate privacy, exact-byte reopen, durable failure handling, and commit-failure orphan behavior
- Repeated-execution hardening for reused SQLite databases
- Released baseline `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1`
- Local filesystem immutable content store review candidate
- ID-addressed immutable object layout with atomic writes and integrity verification
- Desktop composition for durable filesystem-backed document bytes
- Restart and repeated-run proof against the same SQLite database and storage root
- Missing-file, corrupt-file, orphan-visibility, and default-root-policy hardening
- Application-level immutable-store failure classification for persisted-byte workflows
- `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1` approved for release
- Released baseline `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1`
- Began `ROADMAP-V2.0-RC` to reorganize planning around capability evolution while preserving released slice history
- Began `ARCHITECTURE-VISION-2.0-RC` to define the long-term platform constitution, engine model, and completion criteria
- Froze governance baseline `ARCHITECTURE-VISION-2.0`

Next:

- `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`

## 2026-07-18

Completed:

- Parsing and fragment foundation review candidate (`PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`)
- Prompt 1: Domain types — ParserEngine, FragmentIds, ParserRun lifecycle, FragmentCandidate identity, locator model, audit events
- Prompt 2: Application layer — IDocumentParser port, RequestDocumentParsingUseCase, LoadParsingSnapshotUseCase, DI, logging, 30 new Application tests
- Prompt 3: Deterministic parser adapter (PlainTextDocumentParser), SQLite persistence (EF Core records, value converters, model config, mapper, repositories), 5-column unique replay index, migration, static init fix, DI registration
- Prompt 4: Integration, governance, and RC validation — ParsingExecutableWorkflowRunner, Bootstrapper, ConsoleFormatter, 11 Desktop tests, prototype review document, spec and .ai file updates
- Fixed LoadParsingSnapshotResult to include ParserVersion
- Resolved EDR-KE-010: candidate-stage fragment identity derived from `{ImportedSourceId}:{ParserKey}@{ParserContractVersion}:{LocatorType}:{NormalizedLocatorValue}`

Next:

- Begin `PARSING-EXECUTABLE-SLICE-0.1-RC`

## 2026-07-22

Completed:

- `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 1: Domain & Application substrate — `ParserExecutionStatus` enum, `DiagnosticSeverity` enum, `DiagnosticRefType` enum, `ParserDiagnostic` immutable entity, `ParserDiagnosticId`, `IParserDiagnosticRepository`, `DocumentParserRegistry`, `ParserDescriptor`, updated `ParserExecutionResult` with diagnostics, updated `RequestDocumentParsingUseCase` to resolve via registry and persist diagnostics, updated `LoadParsingSnapshotUseCase` and `LoadFragmentReviewSnapshotUseCase` to load diagnostics, created `AddParserDiagnostics` EF migration (10 total), `ParserDiagnosticRecord` with EF Core mapping, registered repositories in DI, added 25 domain tests and 25 Application tests.
- `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 2: Documents Adapter — Implemented `StructuredTextDocumentParser` with heading extraction, numbered/lettered clause extraction, pipe-delimited table extraction, line-range overlap detection, `OVERLAPPING_CONTENT` diagnostic emission, contract hash via SHA-256. Registered as `IDocumentParser` singleton. Added `.md` extension mapping. Added 25 structured text parser tests.
- `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 3: Infrastructure & Executable Proof — Extended `ParsingExecutableWorkflowRunner` with structured text source import and parsing. Updated `ParsingExecutableWorkflowResult` with structured text fields. Added diagnostics display to `ParsingExecutableWorkflowConsoleFormatter`. Added 5 infrastructure persistence tests for `parser_diagnostics` table. Added 6 Desktop integration tests for structured text parsing, diagnostics round-trip, and restart validation.
- `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` Prompt 5: Governance & RC Review — Created prototype review document, updated all governance and continuity files, validated format/build/test (564/564 passing), left repository at release candidate without release.

Next:

- Await release decision for `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` or begin `FRAGMENT-TO-KNOWLEDGE-PROMOTION-FOUNDATION-0.1-RC`

## 2026-07-23

Completed:

- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC` Prompt 1: Domain & Application substrate — Created `PromotionDiagnostic` entity with `Eligible/Promoted/Failed` lifecycle, `PromotionDiagnosticStatus` enum, `PromotionDiagnosticId` strongly-typed ID, domain invariants (terminal state machine, max failure reason length 2000). Created `PromoteFragmentCandidateUseCase` with full precondition checklist (INV-PROMO-001 through 005), `KnowledgeDocument` matching by project/type/title, `AddInitialRevision` and `SupersedeCurrentRevision` paths, citation creation with duplicate check, `DerivedFrom` relationship creation, idempotency by candidate ID and content hash. Created `ActivateProjectUseCase` (Draft -> Active). Created `IPromotionDiagnosticRepository` with 6 methods. Created `LoadPromotionDiagnosticUseCase` query. Registered in `ServiceCollectionExtensions`. Added 24 domain tests for `PromotionDiagnostic`.
- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC` Prompt 2: Infrastructure persistence — Created `PromotionDiagnosticRecord` EF entity with `FragmentCandidateId` unique index, FK constraints, nullable knowledge FK columns. Created `SqlitePromotionDiagnosticRepository` with `FindSuccessfulByContentHashAsync` cross-table JOIN. Created `SqliteKnowledgeDocumentRepository`, `SqliteKnowledgeRevisionRepository`, `SqliteKnowledgeCitationRepository`, `SqliteKnowledgeRelationshipRepository`. Created `PromotionDiagnosticSlice` migration (12th total). Added 6 Infrastructure persistence tests.
- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC` Prompt 3: Repository DI wiring — Wired all 5 knowledge repositories and `PromotionDiagnostic` repository in `ServiceCollectionExtensions` and `DesktopCompositionRoot`. Added 6 Application tests for promotion use case.
- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC` Prompt 4: Desktop executable proof — Created `KnowledgePromotionWorkflowRunner` (26-step orchestration: create project -> activate -> import -> parse -> review -> promote -> supersede -> verify snapshot -> failure scenarios). Created `KnowledgePromotionWorkflowBootstrapper`, `KnowledgePromotionWorkflowResult` (26 properties), `KnowledgePromotionWorkflowConsoleFormatter` (8 sections). Created `ActivateProject` use case (required because promotion requires Active lifecycle). Added 14 Desktop tests: promotion, idempotent replay, supersession, supersession replay, knowledge snapshot (2 revisions, 2 DerivedFrom relationships), authority isolation, diagnostics persistence, snapshot persistence, parsing integration, fragment review integration, expected failure scenarios, console formatter output, data preservation across runs.
- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC` Prompt 5: RC validation and bug fixes — Root cause analysis and 8 fixes: (1) primary idempotency guard by candidate ID before content-hash JOIN, (2) defensive error handlers checking existing diagnostics before insert, (3) runner failure handling via result check instead of exception propagation, (4) project activation use case (Draft -> Active), (5) citation locator value for WholeDocument's empty normalized value, (6) UpdateAsync change tracker fix (`FindAsync` instead of `SingleAsync`), (7) revision label uniqueness (ordinal + GUID prefix), (8) two-phase commit for supersession (BeginSupersession/CompleteSupersession to handle SQLite filtered unique index). Full spec gap analysis. Prototype review document created. All 624 tests passing. Left as release candidate without release.

Next:

- Await release decision for `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC` or begin spec gap remediation

## 2026-07-15

Completed:

- Repository scaffold
- Architecture tests
- AI bootstrap continuity layer
- Centralized .NET build and package configuration hardening
- Domain foundation
- Application foundation
- Application hardening pass
- Application baseline release (`APPLICATION-0.1`)
- Local SQLite Infrastructure foundation
- Local SQLite Infrastructure migration validation
- Infrastructure baseline release (`INFRASTRUCTURE-0.1`)
- First executable local Desktop-to-SQLite vertical slice
- Desktop end-to-end workflow validation
- Vertical-slice baseline release (`VERTICAL-SLICE-0.1`)
- Prototype review milestone for `VERTICAL-SLICE-0.1`
- Second local report-draft vertical slice
- Authoritative report-draft creation and SQLite provenance persistence
- Report-draft executable workflow validation
- Prototype review milestone for `REPORT-DRAFT-SLICE-0.1`
- First AI substrate foundation
- Governed context manifests and advisory AI proposal persistence
- Deterministic Tier 0 AI provider and prompt-package registry
- Structured AI proposal schema and validation pipeline
- AI substrate SQLite migration validation
- `AI-DRAFT-PROPOSAL-SLICE-0.1-RC` review-candidate validation
- AI review hardening pass for lifecycle semantics, pre-inference run persistence, and canonical proposal payload storage
- Released baseline `AI-DRAFT-PROPOSAL-SLICE-0.1`
- Deterministic executable AI proposal workflow through the Desktop host
- AI proposal replay, review-action, failure-display, and no-report-mutation validation
- Released baseline `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`
- Knowledge Engine Domain and Application foundation
- Knowledge document, revision, relationship, and citation model
- Knowledge Engine repository contracts, use cases, and architecture guardrails
- Deferred Knowledge Engine EDR set and authoritative `spec/knowledge/` specification
- `KNOWLEDGE-ENGINE-FOUNDATION-0.1-RC` validation
- Permanent governance layer established under `docs/00-governance/`
- Knowledge Engine SQLite persistence slice
- Knowledge Engine EF Core mappings, repositories, migration, and upgrade validation
- Knowledge Engine Infrastructure and architecture guardrail expansion
- Released baseline `KNOWLEDGE-ENGINE-PERSISTENCE-0.1`

## 2026-07-16

Completed:

- Knowledge Engine executable Desktop slice review candidate
- Deterministic document registration, revision supersession, relationship, and citation workflow
- Application knowledge snapshot query and citation command
- Desktop executable failure presentation and prototype review
- Deferred `EDR-KE-009` for Knowledge command idempotency
- Released baseline `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1`
- Engineering Knowledge Model review candidate
- Document Engine and Rule Engine boundary specifications
- Knowledge concept glossary and governance updates
- `EDR-KE-010`, `EDR-KE-011`, and `EDR-KE-012`

Next:

- Review `ENGINEERING-KNOWLEDGE-MODEL-0.1-RC`, then begin `DOCUMENT-ENGINE-FOUNDATION-0.1-RC`
