# FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC Prototype Review

Date: 2026-07-21
Status: Released
Active implementation package: `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`
Released baseline: `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`
Next active package: `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` (recommended)

## Validation snapshot

- Domain tests: `181/181`
- Application tests: `184/184`
- Documents tests: `28/28`
- Infrastructure tests: `56/56`
- Architecture tests: `24/24`
- AI tests: `6/6`
- Desktop tests: `39/39`
- Total tests: `518/518`

## Checkpoints completed

- Prompt 1 (`FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT`): Implemented `FragmentCandidateReviewState` enum (Generated, HumanAccepted, Rejected), `Accept()` and `Reject()` methods with audit events and terminal state guards, review disposition properties, updated InfrastructureMapper and SpinbusterModelConfiguration, created EF migration `AddFragmentCandidateReviewState`, updated parsing-and-fragment-foundation.md spec, created `EDR-DE-007`, added 20 review lifecycle domain tests.
- Prompt 2 (`FRAGMENT-REVIEW-APPLICATION-CHECKPOINT`): Implemented `AcceptFragmentCandidateUseCase` and `RejectFragmentCandidateUseCase` with audit staging, commit, and structured logging; implemented `LoadFragmentReviewSnapshotUseCase` query with bounded text preview, review metadata, and filter support; extended `IFragmentCandidateRepository` with `GetByIdAsync`, `UpdateAsync`, `GetByProjectFilteredAsync`; implemented in `SqliteFragmentCandidateRepository`; registered in `ServiceCollectionExtensions`; added 28 Application tests.
- Prompt 3 (`FRAGMENT-REVIEW-PERSISTENCE-CHECKPOINT`): Verified EF migration integrity, repository update semantics, atomic commit behavior, and concurrency through first-commit-wins guard.
- Prompt 4 (`FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC`): Extended Desktop executable proof with full review lifecycle, 2-source import, parser version coexistence, expected failure scenarios, and authority isolation verification.

## Behavior validated

### Fragment candidate review lifecycle

- Fragment candidates transition through `Generated(0)`, `HumanAccepted(1)`, or `Rejected(2)` states.
- `Accept()` and `Reject()` are terminal: no reopen workflow exists.
- `HumanAccepted` is reviewed for possible later promotion; does NOT create authoritative Knowledge, citations, assertions, requirements, report content, or rules.
- Review disposition (`ReviewedBy`, `ReviewedAtUtc`, `ReviewNotes`) persists with fragment candidates.
- Review does not mutate identity, provenance, or locator properties.

### Application review workflows

- `AcceptFragmentCandidateUseCase` stages new audit events and commits in one transaction.
- `RejectFragmentCandidateUseCase` stages new audit events and commits in one transaction.
- `LoadFragmentReviewSnapshotUseCase` returns bounded text preview, review metadata, and filter support.
- Scope validation ensures candidates belong to the correct project.
- Terminal state guards prevent re-accepting or re-rejecting decided candidates.

### Review snapshot reload

- `FragmentReviewSnapshot` returns candidate identity, review state, review metadata, and bounded text preview.
- Snapshot survives provider recreation and repeated execution.
- Filters support project-scoped queries and review-state filtering.

### Desktop executable proof

- Imports 2 controlled text sources through the Application import pipeline.
- Parses both sources with version coexistence (separate parser versions preserve historical candidates).
- Accepts first candidate from Source A, rejects last candidate from Source A.
- Loads review snapshots for both accept and reject outcomes.
- Runs 3 existing failure scenarios (unsupported media, cancelled parse, malformed output).
- Adds 4 expected failure scenarios (wrong-project review, already-reviewed candidate, conflicting accept after reject, missing candidate).
- Verifies parser version coexistence with historical candidate preservation.
- Verifies authority isolation: parsing and review do not create Knowledge, Report, or AI records.

### Concurrency and atomicity

- First-commit-wins concurrency model prevents conflicting updates.
- Domain `EnsureReviewNotDecided` guard prevents re-opening decided candidates.
- CHECK constraint enforces review field consistency in SQLite schema.
- Composite indexes `(ProjectId, ReviewState)` and `(ParserRunId, ReviewState)` support filtered queries.

### EF Core tracking fix

- `SqliteFragmentCandidateRepository.UpdateAsync` detaches existing tracked entity before calling `Update()` to prevent tracking conflicts when same scope adds then updates.

## Desktop composition boundary

- The Desktop host composes the executable review workflow through Application commands and queries only.
- The `PlainTextDocumentParser` is registered as `IDocumentParser` in the Documents adapter layer.
- The Desktop host does not directly access Domain entities, EF Core DbContext, or parser internals.
- The `ParsingExecutableWorkflowBootstrapper` resolves all dependencies from DI.
- The `DesktopWorkflowFailurePresentation` record from `LocalVerticalSliceWorkflowResult` is reused for all expected failure scenarios.

## Authority isolation

- Document parsing does not create or mutate `KnowledgeDocument`, `KnowledgeDocumentRevision`, `KnowledgeRelationship`, `KnowledgeCitation`, Report, or AI Proposal records.
- Fragment candidate review (Accept/Reject) does not create or mutate any authoritative records.
- Authority isolation remained intact across success, review, failure, reload, and repeated execution flows.

## Migration status

- 9 total EF Core migrations.
- `AddFragmentCandidateReviewState` was created during Prompt 1 before Application review workflows were finalized. Treat this as `FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT`. Do not create another migration unless the model genuinely changes.

## Prototype review questions

### Does the review lifecycle correctly prevent re-opening decided candidates?

Yes. Domain guards (`EnsureReviewNotDecided`) and CHECK constraints both enforce terminal states. Accepting or rejecting an already-decided candidate throws a domain exception and produces an expected failure scenario in the Desktop executable proof.

### Does review preserve authority boundaries?

Yes. Review (Accept/Reject) persists disposition metadata only. It does not create Knowledge, citations, assertions, requirements, report content, or rules. `HumanAccepted` is necessary but insufficient for Knowledge promotion (per `EDR-DE-007`).

### Can review survive provider recreation?

Yes. Review disposition persists through SQLite and is reloaded through `LoadFragmentReviewSnapshotUseCase` after provider disposal and recreation. The Desktop executable proof demonstrates this explicitly.

### Is the first-commit-wins concurrency model adequate?

Adequate for the foundation. The model prevents conflicting updates without requiring explicit locking. Domain guards provide an additional invariant check layer. Production systems may need optimistic concurrency tokens, but the foundation boundary is sufficient.

### What must be resolved before production review workflows?

1. **Promotion design**: Fragment-to-knowledge promotion workflows remain deferred per `EDR-KE-011`.
2. **Batch review UX**: The current model handles individual candidate review; batch operations need UI design.
3. **Review delegation**: Role-based review assignment is not yet modeled.
4. **Review audit export**: Audit trail export for compliance is not yet implemented.

## Known friction

- SQLite provider-specific query-shaping for `DateTimeOffset` ordering remains a known implementation concern.
- The `MapFailureClassification` in `RequestDocumentParsingUseCase` maps parser failure reasons through string matching, which loses the original parser-specific classification. Acceptable for the foundation but should be refined before production.
- The `AddFragmentCandidateReviewState` migration was created prematurely during Prompt 1. This is a known process deviation and should not be repeated.

## Recommended next package

Recommendation: TBD after review

Rationale:

- The fragment candidate review lifecycle is now validated end-to-end: Domain types, Application workflows, persistence, concurrency, Desktop executable proof, and authority isolation.
- The next increment should focus on either Knowledge promotion from reviewed candidates or broader Document Understanding capabilities.
- This package stays focused on the review lifecycle without prematurely broadening into Knowledge promotion, OCR, or AI extraction.

Follow-on order:

1. Knowledge promotion from reviewed candidates (per `EDR-KE-011`)
2. PDF/text-layout adapter integration
3. Batch review and delegation workflows
4. Review audit export for compliance
