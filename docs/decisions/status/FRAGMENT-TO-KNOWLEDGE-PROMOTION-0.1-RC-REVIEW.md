# FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC Prototype Review

Date: 2026-07-23
Updated: 2026-07-27
Status: Release Candidate (not released)
Active implementation package: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC`
Released baseline: none (pending release decision)
Next active package: TBD after RC release decision

## Validation snapshot

- Domain tests: `234/234`
- Application tests: `221/221`
- Documents tests: `78/78`
- Infrastructure tests: `100/100`
- Architecture tests: `24/24`
- AI tests: `6/6`
- Desktop tests: `75/75`
- Total tests: `738/738`

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

### Supersedes relationship

- Every successful supersession creates a `Supersedes` relationship from the new revision to the old revision.
- Duplicate check prevents redundant relationships for the same source-target pair.

### Idempotency

- Idempotent replay is PromotionIdentity-based only: if a `PromotionRecord` with matching identity hash exists and has a successful attempt, returns cached diagnostic without creating new records (INV-PROMO-009).
- `PromotionIdentity` hash is derived from project, document type, canonical title, external reference, discipline, fragment identity key, and contract version (case-insensitive).
- Candidate-ID or content-hash replay is NOT used; successful replay relies solely on `PromotionIdentity` hash.
- Error handlers check for existing diagnostics per candidate to avoid duplicate diagnostics on retryable failures.

### Supersession

- Supersession occurs when promoted fragment matches existing document with a current authoritative revision AND the new revision has higher source authority than the existing revision.
- AI-parsed candidates receive `Informational` authority from `AuthorityPolicy`, so supersession between AI-parsed candidates is blocked (equal authority).
- Human-controlled `AddKnowledgeDocumentRevision` preserves the revision-management path for genuine supersession.
- Old revision transitions to `Superseded` lifecycle when supersession succeeds.
- New revision transitions to `CurrentAuthoritative` lifecycle when supersession succeeds.
- `SupersedesRevisionId` on new revision explicitly identifies the revision being superseded (INV-PROMO-008).
- `SupersededExistingRevision` flag and `SupersededRevisionId` recorded in diagnostic for audit.
- When supersession is blocked, `HigherAuthorityExists` conflict type is returned.

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

- End-to-end workflow: create project -> activate -> import 2 sources -> parse -> review fragments -> promote -> supersession attempt (blocked by authority policy — AI-parsed candidates are Informational) -> verify snapshot -> recoverable failure -> retry.
- 18 Desktop tests cover: first promotion, idempotent replay, supersession attempt (HigherAuthorityExists), supersession replay, knowledge snapshot with 1 revision, authority isolation, diagnostics persistence, snapshot persistence, parsing integration, fragment review integration, expected failure scenarios, console formatter output, data preservation across runs, recoverable failed promotion, recoverable retry success, retry diagnostic divergence, persisted attempt history, attempt history survives provider recreation.
- Console formatter produces readable output without exposing file paths.
- Two runs against same database preserve prior data (different projects coexist).

### Concurrency and atomicity

- Idempotency guard by PromotionIdentity hash prevents duplicate promotions.
- Non-unique index on `FragmentCandidateId` in `promotion_diagnostics` permits multiple attempts per candidate with independent diagnostic histories.
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

- 17 total EF Core migrations (14 released + ConcurrencyTokenSlice + ConcurrencyTokenAndCanonicalIdentityHash + AttemptAndDiagnosticOwnership + GovernedSourceAuthority).
- `PromotionDiagnosticSlice` migration created during Prompt 2.
- `AttemptAndDiagnosticOwnership` migration removed unique `FragmentCandidateId` index on `promotion_diagnostics`, added `ConflictType` column, and created `promotion_attempts` table.
- `GovernedSourceAuthority` migration added `AuthorityBasis` and `PolicyVersion` columns to `promotion_provenances`.

## Prototype review questions

### Does the promotion workflow correctly enforce all INV-PROMO preconditions?

Yes. The use case enforces INV-PROMO-001 (HumanAccepted), INV-PROMO-002 (Completed parser run), INV-PROMO-003 (content hash match), INV-PROMO-004 (Available source), and INV-PROMO-005 (Active project). HigherAuthorityExists conflict check (equal-authority blocks), temporal ordering on supersession, and AmbiguousDocumentMatch multi-match detection are implemented. AI-parsed candidates receive `Informational` authority and cannot supersede each other.

### Does supersession correctly preserve revision history?

Yes. The two-phase commit (`BeginSupersession` + `CompleteSupersession`) correctly marks the old revision as Superseded and adds the new revision as CurrentAuthoritative. The filtered unique index on `Lifecycle = CurrentAuthoritative` is satisfied because the UPDATE of the old revision's lifecycle is committed before the INSERT of the new revision. The `SupersededByRevisionId` and `SupersedesRevisionId` fields maintain the bidirectional link.

### Is idempotent replay reliable?

Yes. PromotionIdentity-based replay returns cached diagnostics for successful promotions. The identity hash is derived from project, document type, canonical title, external reference, discipline, fragment identity key, and contract version (case-insensitive). Error handlers check for existing diagnostics per candidate to avoid duplicate diagnostics on retryable failures.

### Can promotion survive provider recreation?

Yes. Both `PromotionDiagnostic` and `KnowledgeSnapshot` survive provider disposal and recreation. Tests explicitly demonstrate this.

### What must be resolved before production promotion workflows?

1. **Spec audit events** (spec 3i): Domain-level audit events are emitted (`KnowledgeDocumentRegistered`, `KnowledgeRevisionCreated`, `KnowledgeRevisionSuperseded`), but the spec-specific workflow events (`PromotionWorkflowStarted`, `PromotionCompleted`, `PromotionFailed`, etc.) are not emitted as distinct named events. Domain events are sufficient for foundation use; spec-specific event naming can be added when the promotion boundary stabilizes.
2. **IdentityKeyHash use-case validation** (spec 3a.8): The hash is validated during domain rehydration, not explicitly in the use case. This is sufficient but not directly visible in the promotion workflow.

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
| INV-PROMO-011 Conflicts remain visible | Implemented | Structured conflict diagnostics with ConflictType |
| INV-PROMO-012 Provenance chain unbroken | Implemented | DerivedFrom + Supersedes relationships + audit trail |
| Supersedes relationship (3c.5) | Implemented | Supersedes KnowledgeRelationship created during supersession |
| HigherAuthorityExists (3g) | Implemented | AuthorityPolicy returns Informational for AI-parsed; equal-authority blocks |
| AmbiguousDocumentMatch (3d) | Implemented | Multiple match detection with ConflictType |
| ConcurrentPromotion guard (3g) | Implemented | Idempotency + ConcurrencyConflictException handling |
| SupersessionChainBroken (3g) | Indirect | Domain MarkSuperseded enforces |
| Temporal ordering (3f) | Implemented | ReceivedAtUtc comparison on supersession |
| Spec audit events (3i) | Partial | Domain events emitted; spec-specific named events deferred |

## Known friction

- SQLite filtered unique index (`Lifecycle = CurrentAuthoritative`) required a two-phase commit architecture for supersession. This is an EF Core + SQLite limitation workaround, not a domain concern.
- `SqliteKnowledgeDocumentRepository.UpdateAsync` uses `FindAsync` (not `SingleAsync`) because new documents exist only in the change tracker before first commit. This is a known EF Core tracking behavior.
- The `BeginSupersession` method temporarily sets `CurrentAuthoritativeRevisionId = null` on the document. This is a valid intermediate state during the two-phase commit but creates a brief window where the document appears to have no authoritative revision.
- Audit event staging for the two-phase commit required count-based deduplication to prevent re-staging events already committed in the first transaction.
- Pre-existing CA1848 warnings throughout Application use cases (LoggerMessage delegates) are acknowledged technical debt.

## Recommended next package

Recommendation: Complete remaining spec items before release

Rationale:

- The vertical slice proves the core promotion flow end-to-end: precondition validation, document matching, revision creation, citation, relationship, idempotency, diagnostics, authority isolation.
- Supersession is correctly blocked for AI-parsed candidates (both Informational). Human-controlled `AddKnowledgeDocumentRevision` preserves the revision-management path.
- One spec gap remains: spec audit events (PromotionWorkflowStarted, PromotionCompleted, etc.) are deferred.
- The two-phase commit architecture is validated and stable.

Follow-on order:

1. ~~Supersedes relationship creation during supersession~~ (Implemented in WO5)
2. ~~HigherAuthorityExists conflict check~~ (Implemented in WO4)
3. ~~AmbiguousDocumentMatch detection~~ (Implemented in WO4)
4. Spec audit event naming (PromotionWorkflowStarted, PromotionCompleted, etc.)
5. ~~Temporal ordering on supersession~~ (Implemented in WO4)
6. ConcurrentPromotion optimistic concurrency token (deferred to server/multi-user boundary)
