# Current State

Repository status:
Latest governance baseline: `ARCHITECTURE-VISION-2.0`. Latest software baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`. Active implementation package: none (released; no next implementation package started). Build passing. Domain tests `234/234`. Application tests `233/233`. Documents tests `78/78`. Infrastructure tests `104/104`. Architecture tests `24/24`. AI tests `6/6`. Desktop tests `75/75`. Total `754/754`. Warnings `0` (pre-existing CA1848 acknowledged).

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Latest governance baseline:
`ARCHITECTURE-VISION-2.0`

Latest software baseline:
`FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`

Active implementation package:
none (released; no next implementation package started)

Recent accomplishments:

- Completed `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1-RC` with 6 hardening passes (WO1-WO6):
  - WO1: `PromotionProvenance` value object with immutable audit properties
  - WO2: SupersedeCurrentRevision — sole Domain supersession operation, atomic UnitOfWork transaction
  - WO3: `IAuthorityPolicy` in Domain, `AuthorityPolicy` in Application, DI registered
  - WO4 FINAL: Canonical identity (`KnowledgeDocumentIdentity`), `ConcurrencyToken` on `KnowledgeDocument`, `IKnowledgeDocumentRepository.UpdateAsync` signature hardened, `CanonicalIdentityHash` unique index via migration #15, `promotion_diagnostics`/`promotion_provenances` ownership via migration #16, `AuthorityBasis`/`PolicyVersion` on `PromotionProvenance` via migration #17, `SourceAuthorityLevel` removed from use-case args, all callers updated
  - WO5: `Supersedes` relationship created during supersession, 5 focused Application tests, 3 Desktop recoverable-failure tests, RC review gap analysis updated
  - WO6 FINAL: Persisted attempt history (LoadPromotionAttempts query/handler, Desktop loads real attempts), attempt/diagnostic ownership (4 SQLite tests), canonical identity migration upgrade (5 SQLite tests), released migration integrity (guard tests), supersession claims corrected, governance docs corrected, test counts updated to 740/740
  - WO7 CORRECTION: Removed candidate-based diagnostic short-circuit from PromoteFragmentCandidateUseCase (both catch blocks now always create new diagnostic/attempt), secured LoadPromotionAttemptsQuery with ProjectId ownership validation and MaxResults, added SanitizedFailureReason (500-char cap), new LoadPromotionAttemptsUseCaseTests (5 tests), new SqlitePromotionAttemptOwnershipTests (2 tests: wrong-project, max-bound), exposed RecoverableProject in workflow result/runner, fixed Desktop test ProjectId mismatch, test counts updated to 751/751
  - WO8 CONTRACT CLEANUP: Removed raw FailureReason and ContentHash from PromotionAttemptResult, mapped FailureSummary in use case (500-char cap + `...`), continuity corrected to single-UoW SupersedeCurrentRevision and attempt-owned history, test counts updated to 754/754
  - WO9 RELEASE-TEXT: Renamed PromotionAttemptResult.FailureReason -> FailureSummary, removed two-phase commit statement from RC review, fixed migration arithmetic (17 total), verified no active references to BeginSupersession/CompleteSupersession/two-phase, 754/754
  - RELEASE: Released `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1` as immutable tag at commit `f8b6d75`; release-state continuity updated; next action is the engineering-workflow governance package (no next implementation package started)

Current architectural decisions:

- `ARCHITECTURE-VISION-2.0` is the active governance baseline.
- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1` is the latest released software baseline.
- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1` is released; no next implementation package is started (next action is the engineering-workflow governance package).
- Fragment identity is parser-run-scoped, not revision-stable (EDR-KE-010 resolved).
- Fragment identity uses contract version, not implementation version (EDR-DE-006 accepted).
- Fragment candidate review uses terminal disposition model: Generated -> HumanAccepted or Rejected (EDR-DE-007 accepted).
- `HumanAccepted` is necessary but insufficient for Knowledge promotion (EDR-DE-007 accepted).
- Replay key is 5-column: `(ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash)`.
- Knowledge promotion identity: `KnowledgeDocumentIdentity` with case-insensitive canonical title hash.
- Authority policy: AI-parsed candidates classified `Informational`, cannot escalate authority.
- AI-parsed supersession blocked at equal authority via `SourceAuthority >= incoming`.
- Canonical identity enforced via unique index on `CanonicalIdentityHash` (migration #15).
- `ConcurrencyToken` on `KnowledgeDocument` prevents lost updates.
- `IKnowledgeDocumentRepository.UpdateAsync` sets properties and increments token via `FindAsync`.
- `PromotionProvenance` records `AuthorityBasis` and `PolicyVersion`.
- Promotion attempt/diagnostic ownership: per-candidate isolation verified by SQLite integration tests.
- Released migration integrity: 17 migrations apply cleanly, snapshot consistent.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- The Document Engine owns binary-source handling and non-authoritative processing outputs only.
- The Desktop host composes document workflow behavior through Application commands and queries only.
- Parser adapters are registered as `IDocumentParser` singletons in the Documents adapter layer.
- Parser diagnostics are immutable, not independently auditable, no review lifecycle (EDR-DE-008 accepted).
- Parser execution status uses Completed/CompletedWithWarnings/Failed instead of boolean success.
- The Rule Engine will remain deterministic and separate from AI recommendations.
- Knowledge Engine command idempotency is still deferred by `EDR-KE-009`.

Next task:
Begin the engineering-workflow governance package (previously agreed as the next action after promotion release). No next implementation package has been started; do not begin a new vertical slice in this operation.

Known issues:

- Most non-architecture test projects are still empty scaffolds and intentionally have no real test cases yet.
- The Desktop host is still a console bootstrapper and should not accumulate broader UI assumptions before the real client direction is chosen.
- Human-accepted AI proposals still do not create authoritative report revisions; that boundary remains deferred by `EDR-AI-001`.
- Knowledge Engine command idempotency is still deferred and must be designed before synchronization or automated ingestion work.
- Document OCR, assertion promotion, and broader retrieval remain deferred beyond the current foundation.
- Reconciliation and deletion for orphaned immutable filesystem objects remain deferred intentionally.
- The generated Windows Desktop apphost may still be blocked by local machine policy even when the managed DLL runs correctly; treat that as environmental for the temporary host.
- The `MapFailureClassification` in `RequestDocumentParsingUseCase` maps parser failure reasons through string matching, which loses the original parser-specific classification. Acceptable for the foundation but should be refined before production.
- `AddKnowledgeCitationUseCase` retains direct `new AuditEvent(...)` construction as intentional single-event duplication, not a general pattern for other use cases.
- The EF migration `AddFragmentCandidateReviewState` was created during the Domain checkpoint (Prompt 1) before Application review workflows were finalized. Treat this as `FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT`. Do not create another migration unless the model genuinely changes.
- Fragment candidate review concurrency relies on aggregate-level guards (`EnsureReviewNotDecided`). Before server or multi-user work, the database update itself should verify original state with a conditional SQL `WHERE ReviewState = Generated` to ensure true multi-process safety.
- Knowledge promotion concurrency relies on `PromotionIdentity` uniqueness. No optimistic concurrency token prevents two simultaneous promotions from both passing the idempotency check. Acceptable for single-user foundation; required before server or multi-user work.
  5. `SupersedeCurrentRevision` is the sole Domain supersession operation; promotion persistence is one atomic `UnitOfWork` transaction. Attempt history is attempt-owned and indexed/queryable by candidate.
- Pre-existing CA1848 warnings throughout Application use cases (LoggerMessage delegates) are acknowledged technical debt.

Requested review:

- Approved. No release blockers.
- Released as `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`.
- Recommend the engineering-workflow governance package as the next package.

Current capabilities:

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
- `SupersedeCurrentRevision` is the sole Domain supersession operation; promotion persistence uses one atomic `UnitOfWork` transaction.
- Promotion diagnostics are durable and queryable (Eligible/Promoted/Failed lifecycle)
- Project lifecycle management: Draft -> Active (required for promotion eligibility)
- Knowledge snapshot survives provider disposal and recreation
- End-to-end executable proof: create project -> activate -> import -> parse -> review -> promote -> supersede -> verify snapshot
- Persisted attempt history: Desktop scenario loads real promotion attempts across retries
- Attempt/diagnostic ownership: SQLite integration tests verify per-candidate isolation
- Canonical identity migration: unique index, duplicate detection, cross-project isolation verified
- Released migration integrity: 17 migrations apply cleanly, snapshot consistency verified
- AI-parsed supersession blocked at equal authority via IAuthorityPolicy (Informational classification)
- Candidate-based diagnostic short-circuit removed: every promotion attempt creates fresh diagnostic/attempt
- LoadPromotionAttempts secured with ProjectId ownership validation and MaxResults enforcement
- PromotionAttemptResult exposes bounded FailureSummary (500-char cap); raw FailureReason and ContentHash removed from public DTO

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
13. `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`

Release candidates (validated, not released):

1. `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC`
