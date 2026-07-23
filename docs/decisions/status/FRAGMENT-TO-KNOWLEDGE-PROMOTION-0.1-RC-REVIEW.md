# FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC Prototype Review

Date: 2026-07-23
Status: Release Candidate (not released)
Active implementation package: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC`
Released baseline: none (pending release decision)
Next active package: TBD after RC release decision

## Validation snapshot

- Domain tests: `188/188`
- Application tests: `184/184`
- Documents tests: `78/78`
- Infrastructure tests: `82/82`
- Architecture tests: `24/24`
- AI tests: `6/6`
- Desktop tests: `62/62`
- Total tests: `624/624`

## Checkpoints completed

- Prompt 1 (`PROMOTION-DOMAIN-AND-APPLICATION-CHECKPOINT`): Created `PromotionDiagnostic` entity with `Eligible/Promoted/Failed` lifecycle, `PromotionDiagnosticStatus` enum, `PromotionDiagnosticId`, domain invariants (terminal state machine, max failure reason length). Created `PromoteFragmentCandidateUseCase` with full precondition checklist (INV-PROMO-001 through INV-PROMO-005), `KnowledgeDocument` matching by project/type/title, `AddInitialRevision` and `SupersedeCurrentRevision` paths, citation creation with duplicate check, `DerivedFrom` relationship creation. Created `ActivateProjectUseCase` (Draft -> Active). Created `IPromotionDiagnosticRepository` with 6 methods including content-hash-based idempotency lookup. Registered in DI. Added 24 domain tests for `PromotionDiagnostic`.
- Prompt 2 (`PROMOTION-INFRASTRUCTURE-CHECKPOINT`): Created `PromotionDiagnosticRecord` EF entity with `FragmentCandidateId` unique index, FK constraints, and all nullable knowledge FK columns. Created EF migration `PromotionDiagnosticSlice` (12th migration). Created `SqlitePromotionDiagnosticRepository` with `FindSuccessfulByContentHashAsync` cross-table JOIN query. Created `SqliteKnowledgeDocumentRepository`, `SqliteKnowledgeRevisionRepository`, `SqliteKnowledgeCitationRepository`, `SqliteKnowledgeRelationshipRepository`. Added 6 Infrastructure persistence tests.
- Prompt 3 (`PROMOTION-REPOSITORY-DI-CHECKPOINT`): Wired all 5 knowledge repositories and `PromotionDiagnostic` repository in `ServiceCollectionExtensions` and `DesktopCompositionRoot`. Added `LoadPromotionDiagnosticUseCase` query handler. Added 6 Application tests for promotion use case.
- Prompt 4 (`PROMOTION-EXECUTABLE-SLICE-CHECKPOINT`): Created `KnowledgePromotionWorkflowRunner` (26-step orchestration), `KnowledgePromotionWorkflowBootstrapper`, `KnowledgePromotionWorkflowResult` (26 properties), `KnowledgePromotionWorkflowConsoleFormatter` (8 sections). Extended `DesktopCompositionRoot` with all promotion DI registrations. Created `ActivateProject` use case (required because promotion requires Active lifecycle). Added 14 Desktop tests covering: promotion, idempotent replay, supersession, supersession replay, knowledge snapshot, authority isolation, diagnostics persistence, snapshot persistence, parsing integration, fragment review integration, expected failure scenarios, console formatter output, data preservation across runs.
- Prompt 5 (`PROMOTION-RC-VALIDATION`): Root cause analysis and 8 bug fixes for promotion pipeline: (1) primary idempotency guard by candidate ID before content-hash JOIN, (2) defensive error handlers checking existing diagnostics before insert, (3) runner failure handling via result check instead of exception propagation, (4) project activation use case (Draft -> Active), (5) citation locator value for WholeDocument's empty normalized value, (6) UpdateAsync change tracker fix for new documents, (7) revision label uniqueness across parser runs, (8) two-phase commit for supersession (filtered unique index collision). Full spec gap analysis. All 624 tests passing.

## Behavior validated

### Promotion lifecycle

- Fragment candidate must be `HumanAccepted` to promote (INV-PROMO-001).
- Parser run must be `Completed` with `Completed` or `CompletedWithWarnings` execution status (INV-PROMO-002).
- Source content hash must match between candidate and parser run (INV-PROMO-003).
- Imported document source must be `Available` (INV-PROMO-004).
- Project must be `Active` (INV-PROMO-005); promotion creates from `Draft` via `ActivateProject`.
- All preconditions are validated deterministically; no AI participates in promotion decisions (INV-PROMO-010).

### Knowledge document creation and matching

- Documents are matched deterministically by project, document type, and canonical title (case-insensitive).
- New documents created when no match exists; existing documents receive new revisions.
- `CanonicalTitle` is required; `ExternalReferenceNumber` and `DisciplineOrCategory` are optional.

### Revision lifecycle

- First promotion on a document creates initial revision via `AddInitialRevision` (Received -> CurrentAuthoritative).
- Subsequent promotions on same document use two-phase supersession: `BeginSupersession` (marks old revision Superseded, commits) then `CompleteSupersession` (adds new revision as CurrentAuthoritative, commits).
- Two-phase commit required because SQLite filtered unique index `Lifecycle = CurrentAuthoritative` enforces one authoritative revision per document, and EF Core processes INSERTs before UPDATEs.
- Revision label includes ordinal and 8-char GUID prefix to prevent duplicates across parser runs.
- `SourceAuthority` is `Informational` for all parsed content.

### Citation rules

- Exactly one `KnowledgeCitation` per successful promotion (INV-PROMO-006).
- Citation points to the specific revision, not current authoritative (INV-PROMO-007: revision is immutable once created).
- Duplicate citation check prevents same locator type + value on same revision.
- `WholeDocument` locator uses `RawValue` instead of `NormalizedValue` (NormalizedValue is empty string for WholeDocument).
- Citations remain valid after supersession (they reference historical revisions).

### DerivedFrom relationship

- Every successful promotion creates a `DerivedFrom` relationship from the new revision to the document.
- Duplicate check prevents redundant relationships for the same source-target pair.

### Idempotency

- Idempotent replay by fragment candidate ID: if `PromotionDiagnostic` exists for candidate, returns existing result without creating new records (INV-PROMO-009).
- Idempotent replay by content hash: if successful diagnostic exists for same project + content hash + normalized locator, returns existing result.
- Both replay paths are tested: same-candidate replay and supersession replay.
- Idempotent replay of failed promotions re-runs from `Eligible` state.

### Supersession

- Supersession occurs when promoted fragment matches existing document with a current authoritative revision.
- Old revision transitions to `Superseded` lifecycle.
- New revision transitions to `CurrentAuthoritative` lifecycle.
- `SupersedesRevisionId` on new revision explicitly identifies the revision being superseded (INV-PROMO-008).
- `SupersededExistingRevision` flag and `SupersededRevisionId` recorded in diagnostic for audit.

### Promotion diagnostics

- `PromotionDiagnostic` records are durable and queryable (not transient) (INV-PROMO-011).
- Status lifecycle: `Eligible -> Promoted` or `Eligible -> Failed` (terminal states).
- Diagnostic captures: document ID, revision ID, citation ID, supersession flag, superseded revision ID.
- Failure diagnostics capture: failure reason (max 2000 chars).
- Diagnostics survive provider disposal and recreation.

### Authority isolation

- Promotion does not mutate `FragmentCandidate`, `ParserRun`, `ImportedDocumentSource`, or AI Proposal records.
- Promotion creates new `KnowledgeDocument`, `KnowledgeDocumentRevision`, `KnowledgeCitation`, `KnowledgeRelationship`, and `PromotionDiagnostic` records only.
- No AI model, embedding, inference, or automated classification participates in the promotion decision (INV-PROMO-010).

### Desktop executable proof

- End-to-end workflow: create project -> activate -> import 2 sources -> parse -> review fragments -> promote -> supersede -> verify snapshot.
- 13 Desktop tests cover: first promotion, idempotent replay, supersession, supersession replay, knowledge snapshot with 2 revisions, authority isolation, diagnostics persistence, snapshot persistence, parsing integration, fragment review integration, expected failure scenarios, console formatter output, data preservation across runs.
- Console formatter produces readable output without exposing file paths.
- Two runs against same database preserve prior data (different projects coexist).

### Concurrency and atomicity

- Idempotency guard by fragment candidate ID prevents duplicate promotions.
- `PromotionDiagnostic` unique index on `FragmentCandidateId` prevents duplicate diagnostics.
- Domain state machine prevents invalid lifecycle transitions on `PromotionDiagnostic`, `KnowledgeDocumentRevision`, and `KnowledgeDocument`.

## Desktop composition boundary

- The Desktop host composes the promotion workflow through Application commands and queries only.
- `KnowledgePromotionWorkflowRunner` orchestrates all steps via `ICommandHandler` and `IQueryHandler` interfaces.
- The Desktop host does not directly access Domain entities, EF Core DbContext, or repository internals.
- `KnowledgePromotionWorkflowBootstrapper` resolves all dependencies from DI via async service scope.
- `KnowledgePromotionWorkflowConsoleFormatter` formats results using only result record properties.
- `DesktopWorkflowFailurePresentation` captures expected failure scenarios.

## Authority isolation

- Promotion creates Knowledge records only from explicitly human-reviewed candidates.
- No AI proposal, AI inference, or automated classification participates in promotion decisions.
- Authority isolation verified by `AuthorityIsolationNoAiDecisionsInPromotion` test.
- Parsing and review workflows remain isolated from Knowledge mutation.

## Migration status

- 12 total EF Core migrations.
- `PromotionDiagnosticSlice` migration created during Prompt 2.
- `PromotionDiagnosticRecord` table with unique index on `FragmentCandidateId`, FK constraints, and nullable knowledge FK columns.

## Prototype review questions

### Does the promotion workflow correctly enforce all INV-PROMO preconditions?

Partially. The use case enforces INV-PROMO-001 (HumanAccepted), INV-PROMO-002 (Completed parser run), INV-PROMO-003 (content hash match), INV-PROMO-004 (Available source), and INV-PROMO-005 (Active project). Not implemented: `IdentityKeyHash` validation is handled by domain rehydration (not explicit in use case). HigherAuthorityExists conflict check, temporal ordering on supersession, and AmbiguousDocumentMatch multi-match detection are not implemented (see gap analysis below).

### Does supersession correctly preserve revision history?

Yes. The two-phase commit (`BeginSupersession` + `CompleteSupersession`) correctly marks the old revision as Superseded and adds the new revision as CurrentAuthoritative. The filtered unique index on `Lifecycle = CurrentAuthoritative` is satisfied because the UPDATE of the old revision's lifecycle is committed before the INSERT of the new revision. The `SupersededByRevisionId` and `SupersedesRevisionId` fields maintain the bidirectional link.

### Is idempotent replay reliable?

Yes. Two independent idempotency paths exist: by fragment candidate ID (primary) and by content hash (secondary). Both are tested with direct replay and supersession replay scenarios. The primary guard was moved before the content-hash JOIN to improve reliability.

### Can promotion survive provider recreation?

Yes. Both `PromotionDiagnostic` and `KnowledgeSnapshot` survive provider disposal and recreation. Tests explicitly demonstrate this.

### What must be resolved before production promotion workflows?

1. **Supersedes relationship** (spec 3c.5): The promotion use case creates `DerivedFrom` relationships but does not create `Supersedes`-typed `KnowledgeRelationship` records. The supersession link is tracked only via revision properties (`SupersedesRevisionId` / `SupersededByRevisionId`).
2. **HigherAuthorityExists conflict** (spec 3g): No check compares the existing revision's `SourceAuthority` against the incoming promotion's authority level.
3. **AmbiguousDocumentMatch** (spec 3d): `FindOrCreateKnowledgeDocumentAsync` uses `FirstOrDefault` without checking for multiple matching documents.
4. **ConcurrentPromotion guard** (spec 3g): Idempotency protection exists but no optimistic concurrency token prevents two simultaneous promotions from both passing the idempotency check.
5. **Temporal ordering on supersession** (spec 3f): No check verifies `new revision.ReceivedAtUtc >= existing revision.ReceivedAtUtc`.
6. **Spec audit events** (spec 3i): Domain-level audit events are emitted (`KnowledgeDocumentRegistered`, `KnowledgeRevisionCreated`, `KnowledgeRevisionSuperseded`), but the spec-specific workflow events (`PromotionWorkflowStarted`, `PromotionCompleted`, `PromotionFailed`, etc.) are not emitted as distinct named events.
7. **IdentityKeyHash use-case validation** (spec 3a.8): The hash is validated during domain rehydration, not explicitly in the use case. This is sufficient but not directly visible in the promotion workflow.

## Gap analysis (spec compliance)

| Spec requirement | Status | Notes |
|---|---|---|
| INV-PROMO-001 HumanAccepted | Implemented | |
| INV-PROMO-002 Completed parser run | Implemented | |
| INV-PROMO-003 Content hash match | Implemented | |
| INV-PROMO-004 Source Available | Implemented | |
| INV-PROMO-005 Project Active | Implemented | Via ActivateProject |
| INV-PROMO-006 One citation per promotion | Implemented | |
| INV-PROMO-007 Revision immutable | Implemented | No mutation after creation |
| INV-PROMO-008 Explicit supersession | Implemented | SupersedesRevisionId required |
| INV-PROMO-009 Idempotency preserved | Implemented | Dual-path: by ID and by content hash |
| INV-PROMO-010 AI excluded from authority | Implemented | |
| INV-PROMO-011 Conflicts remain visible | Partial | Domain exceptions thrown but no structured conflict diagnostics |
| INV-PROMO-012 Provenance chain unbroken | Implemented | DerivedFrom relationship + audit trail |
| Supersedes relationship (3c.5) | Not implemented | Only DerivedFrom created |
| HigherAuthorityExists (3g) | Not implemented | |
| AmbiguousDocumentMatch (3d) | Not implemented | Uses FirstOrDefault |
| ConcurrentPromotion guard (3g) | Partial | Idempotency only, no concurrency token |
| SupersessionChainBroken (3g) | Indirect | Domain MarkSuperseded enforces |
| Temporal ordering (3f) | Not implemented | |
| Spec audit events (3i) | Not implemented | Domain events used instead |

## Known friction

- SQLite filtered unique index (`Lifecycle = CurrentAuthoritative`) required a two-phase commit architecture for supersession. This is an EF Core + SQLite limitation workaround, not a domain concern.
- `SqliteKnowledgeDocumentRepository.UpdateAsync` uses `FindAsync` (not `SingleAsync`) because new documents exist only in the change tracker before first commit. This is a known EF Core tracking behavior.
- The `BeginSupersession` method temporarily sets `CurrentAuthoritativeRevisionId = null` on the document. This is a valid intermediate state during the two-phase commit but creates a brief window where the document appears to have no authoritative revision.
- Audit event staging for the two-phase commit required count-based deduplication to prevent re-staging events already committed in the first transaction.
- Pre-existing CA1848 warnings throughout Application use cases (LoggerMessage delegates) are acknowledged technical debt.

## Recommended next package

Recommendation: Complete the Knowledge Promotion spec gaps before release

Rationale:

- The vertical slice proves the core promotion flow end-to-end: precondition validation, document matching, revision creation, supersession, citation, relationship, idempotency, diagnostics, authority isolation.
- Six spec gaps remain: Supersedes relationship, HigherAuthorityExists, AmbiguousDocumentMatch, ConcurrentPromotion, temporal ordering, and spec audit events.
- These gaps are well-scoped and can be addressed as incremental improvements without architectural changes.
- The two-phase commit architecture is validated and stable.

Follow-on order:

1. Supersedes relationship creation during supersession
2. HigherAuthorityExists conflict check
3. AmbiguousDocumentMatch detection
4. Spec audit event naming (PromotionWorkflowStarted, PromotionCompleted, etc.)
5. Temporal ordering on supersession
6. ConcurrentPromotion optimistic concurrency token (deferred to server/multi-user boundary)
