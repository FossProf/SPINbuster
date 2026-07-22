# Current State

Repository status:
Latest governance baseline: `ARCHITECTURE-VISION-2.0`. Latest software baseline: `PARSING-AND-FRAGMENT-FOUNDATION-0.1`. Active implementation package: `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC`. Build passing. Domain tests `181/181`. Application tests `184/184`. Documents tests `28/28`. Infrastructure tests `56/56`. Architecture tests `24/24`. AI tests `6/6`. Desktop tests `39/39`. Total `518/518`. Warnings `0`.

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Latest governance baseline:
`ARCHITECTURE-VISION-2.0`

Latest software baseline:
`PARSING-AND-FRAGMENT-FOUNDATION-0.1`

Active implementation package:
`FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC`

Recent accomplishments:

- Released `PARSING-AND-FRAGMENT-FOUNDATION-0.1` with executable proof validated.
- Implemented Domain types (ParserEngine, FragmentIds) with parser-run lifecycle, fragment-candidate identity, locator model, and audit events.
- Added Application layer: IDocumentParser port, RequestDocumentParsingUseCase, LoadParsingSnapshotUseCase, DI, logging.
- Implemented deterministic PlainTextDocumentParser adapter with EF Core SQLite persistence.
- Hardened replay key to 5-column unique index: (ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash).
- Created Desktop executable workflow: ParsingExecutableWorkflowRunner, Bootstrapper, ConsoleFormatter.
- Added 11 Desktop tests covering parsing, idempotent replay, version coexistence, failure handling, and authority isolation.
- Resolved EDR-KE-010: candidate-stage fragment identity derived from `{ImportedSourceId}:{ParserKey}@{ParserContractVersion}:{LocatorType}:{NormalizedLocatorValue}`.
- Created prototype review document at `docs/decisions/status/PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC-PROTOTYPE-REVIEW.md`.
- Completed `FRAGMENT-INTEGRITY-HARDENING-CHECKPOINT`: fixed `FragmentCandidate.Rehydrate` to restore persisted state directly without invalid placeholders, added `ValidateRehydratedState` with 12 invariant checks, documented contract-version identity choice in `EDR-DE-006`.
- Completed `FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT` (Prompt 1): implemented `FragmentCandidateReviewState` enum (Generated, HumanAccepted, Rejected), added `Accept()` and `Reject()` methods with audit events and terminal state guards, added review disposition properties (`ReviewState`, `ReviewedBy`, `ReviewedAtUtc`, `ReviewNotes`), updated `InfrastructureMapper` and `SpinbusterModelConfiguration`, created EF migration `AddFragmentCandidateReviewState`, updated `parsing-and-fragment-foundation.md` spec with review semantics, created `EDR-DE-007` for review disposition and promotion prerequisites, added 20 review lifecycle domain tests.
- Completed `FRAGMENT-REVIEW-APPLICATION-CHECKPOINT` (Prompt 2): implemented `AcceptFragmentCandidateUseCase` and `RejectFragmentCandidateUseCase` commands with audit staging, commit, and structured logging; implemented `LoadFragmentReviewSnapshotUseCase` query with bounded text preview, review metadata, and filter support; extended `IFragmentCandidateRepository` with `GetByIdAsync`, `UpdateAsync`, `GetByProjectAsync`; implemented in `SqliteFragmentCandidateRepository`; registered in `ServiceCollectionExtensions`; added 28 Application tests covering happy paths, terminal state guards, scope rejection, commit failure, audit staging, no-authority-mutation, snapshot bounds, filters, text preview, review metadata, and logging EventIds.
- Completed `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC` (Prompt 4): extended Desktop executable proof with full review lifecycle, 2-source import, parser version coexistence, 4 expected failure scenarios (wrong-project review, already-reviewed candidate, conflicting accept after reject, missing candidate), authority isolation verification; fixed EF Core tracking conflict in `SqliteFragmentCandidateRepository.UpdateAsync`; created prototype review document at `docs/decisions/status/FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC-REVIEW.md`.
- All 518 tests passing.

Current architectural decisions:

- `ARCHITECTURE-VISION-2.0` is the active governance baseline.
- `PARSING-AND-FRAGMENT-FOUNDATION-0.1` is the latest released software baseline.
- `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC` is the active implementation package.
- Fragment identity is parser-run-scoped, not revision-stable (EDR-KE-010 resolved).
- Fragment identity uses contract version, not implementation version (EDR-DE-006 accepted).
- Fragment candidate review uses terminal disposition model: Generated → HumanAccepted or Rejected (EDR-DE-007 accepted).
- `HumanAccepted` is necessary but insufficient for Knowledge promotion (EDR-DE-007 accepted).
- Replay key is 5-column: `(ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash)`.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- The Document Engine owns binary-source handling and non-authoritative processing outputs only.
- The Desktop host composes document workflow behavior through Application commands and queries only.
- Parser adapters are registered as `IDocumentParser` singletons in the Documents adapter layer.
- The Rule Engine will remain deterministic and separate from AI recommendations.
- Knowledge Engine command idempotency is still deferred by `EDR-KE-009`.

Next task:
Awaiting review of `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC`. After approval, determine next package (Knowledge promotion or broader Document Understanding).

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

Requested review:

- Confirm review lifecycle boundary is correct
- Confirm authority isolation for review workflows
- Recommend next package after approval

Current capabilities:

- Current released code behavior includes `PARSING-AND-FRAGMENT-FOUNDATION-0.1`
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
