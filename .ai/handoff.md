# Current State

Repository status:
`DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1` is the latest released baseline. The active review candidate is `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1-RC`. The next planned package is `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`. Build passing. Domain tests `53/53`. Application tests `74/74`. Documents tests `28/28`. Infrastructure tests `27/27`. Architecture tests `21/21`. Desktop tests `23/23`. Warnings `0`.

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Latest released baseline:
`DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1`

Active review candidate:
`LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1-RC`

Next planned package:
`PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`

Recent accomplishments:

- Released `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1` and pushed tag `knowledge-engine-executable-slice-0.1`.
- Added the first executable local Knowledge Engine workflow through the temporary Desktop host.
- Added `AddKnowledgeCitation` plus `LoadProjectKnowledgeSnapshot` so the host remains thin and Application-driven.
- Added SQLite-backed Desktop tests for successful Knowledge execution, reload, current revision selection, relationship traversal, citation reload, audit ordering, failure presentation, commit failure handling, and proof that report plus AI records remain unchanged.
- Added the engineering object model and specification index.
- Implemented the first durable Document Engine foundation across Domain, Application, Documents, and Infrastructure.
- Added immutable storage-object identity, imported-source identity, import sessions, processing attempts, and non-authoritative document candidates.
- Added deterministic SHA-256 hashing, media inspection, in-memory immutable storage, and deterministic fixture processing adapters.
- Added SQLite persistence and the `DocumentEngineFoundationRc` migration.
- Released `DOCUMENT-ENGINE-FOUNDATION-0.1` after hardening durable terminal processing state, explicit import-session completion, and migration drift guards.
- Implemented the first executable local Document Engine workflow through the temporary Desktop host.
- Added `LoadProjectDocumentWorkflowSnapshot` so the host reloads project-scoped import sessions, imported sources, processing attempts, candidates, and audit history through Application contracts only.
- Added an Infrastructure migrator abstraction so Desktop startup migration no longer reaches directly into EF Core.
- Hardened document audit staging to persist only audit deltas during repeated aggregate updates.
- Hardened batch import lifecycle semantics so one import session can validate and import multiple sources before explicit completion.
- Hardened SQLite document query shaping to avoid provider translation failures for `DateTimeOffset` ordering.
- Added Desktop tests for multi-source import, exact-byte reopen, deterministic processing outcomes, review persistence, duplicate privacy, and commit-failure orphan behavior.
- Hardened the executable workflow for repeated execution on reused SQLite databases by scoping each run to current-run IDs, unique fixture filenames, and unique fixture content.
- Released baseline `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1`.
- Implemented the local filesystem immutable content store with ID-addressed layout, atomic writes, bounded reparse-point defense, restart-safe reopen, and adapter-private inventory.
- Corrected the Desktop default durable storage root to `%LOCALAPPDATA%\\SPINbuster\\document-content`.
- Added restart, repeated-run, corruption, orphan, configuration, and no-absolute-path Desktop tests against the real local filesystem adapter.
- Added Application-level immutable content store failure classification so orchestration can distinguish missing, unavailable, access-denied, integrity, cancellation, and I/O outcomes without filesystem-specific leakage.

Current architectural decisions:

- `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1` is now the active released baseline.
- The active review candidate is `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1-RC`.
- The next planned package is `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- The Document Engine owns binary-source handling and non-authoritative processing outputs only.
- The Desktop host composes document workflow behavior through Application commands and queries only.
- Startup migration for temporary hosts now flows through a dedicated Infrastructure migrator abstraction rather than direct `DbContext` access.
- Durable document bytes now flow through a local filesystem adapter while the in-memory adapter remains fixture-only.
- The Rule Engine will remain deterministic and separate from AI recommendations.
- Knowledge Engine mutations still do not have a uniform `OperationId` replay contract; `EDR-KE-009` keeps that deferred before synchronization or automated ingestion.

Next task:
Complete validation and the review-only commit for `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1-RC`, then begin `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`

Known issues:

- Most non-architecture test projects are still empty scaffolds and intentionally have no real test cases yet.
- The Desktop host is still a console bootstrapper and should not accumulate broader UI assumptions before the real client direction is chosen.
- Human-accepted AI proposals still do not create authoritative report revisions; that boundary remains deferred by `EDR-AI-001`.
- Knowledge Engine command idempotency is still deferred and must be designed before synchronization or automated ingestion work.
- Document parsing, OCR, fragment promotion, assertion promotion, and broader retrieval remain deferred beyond the current foundation.
- Reconciliation and deletion for orphaned immutable filesystem objects remain deferred intentionally.
- The generated Windows Desktop apphost may still be blocked by local machine policy even when the managed DLL runs correctly; treat that as environmental for the temporary host.

Requested review:

- Confirm the local filesystem adapter review candidate is ready for release after review
- Confirm `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC` is the correct next package

Current capabilities:

- Current released code behavior now includes `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1`
- The repository now also contains an authoritative conceptual engineering knowledge model
- The repository now includes the released durable Document Engine foundation beneath the future executable slice
- The repository now includes a deterministic executable Document Engine workflow that persists import sessions, duplicates, processing attempts, review state, and audit history without mutating authoritative Knowledge, Report, or AI records, including repeated execution on a reused SQLite database
- The repository now includes a local filesystem immutable content store with restart-safe byte reopen, corruption detection, bounded orphan visibility, and no absolute-path exposure through public snapshots or Desktop output
