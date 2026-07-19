# Current State

Repository status:
Latest governance baseline: `ARCHITECTURE-VISION-2.0`. Latest software baseline: `PARSING-AND-FRAGMENT-FOUNDATION-0.1`. Active implementation package: `PARSING-EXECUTABLE-SLICE-0.1-RC`. Build passing. Domain tests `152/152`. Application tests `156/156`. Documents tests `28/28`. Infrastructure tests `42/42`. Architecture tests `24/24`. Desktop tests `34/34`. Total `442/442`. Warnings `0`.

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Latest governance baseline:
`ARCHITECTURE-VISION-2.0`

Latest software baseline:
`PARSING-AND-FRAGMENT-FOUNDATION-0.1`

Active implementation package:
`PARSING-EXECUTABLE-SLICE-0.1-RC`

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
- All 442 tests passing.

Current architectural decisions:

- `ARCHITECTURE-VISION-2.0` is the active governance baseline.
- `PARSING-AND-FRAGMENT-FOUNDATION-0.1` is the latest released software baseline.
- `PARSING-EXECUTABLE-SLICE-0.1-RC` is the active implementation package.
- Fragment identity is parser-run-scoped, not revision-stable (EDR-KE-010 resolved).
- Replay key is 5-column: `(ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash)`.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- The Document Engine owns binary-source handling and non-authoritative processing outputs only.
- The Desktop host composes document workflow behavior through Application commands and queries only.
- Parser adapters are registered as `IDocumentParser` singletons in the Documents adapter layer.
- The Rule Engine will remain deterministic and separate from AI recommendations.
- Knowledge Engine command idempotency is still deferred by `EDR-KE-009`.

Next task:
Begin `PARSING-EXECUTABLE-SLICE-0.1-RC`

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

Requested review:

- Confirm the parsing executable slice boundary before implementation begins

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
