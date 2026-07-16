# Current State

Repository status:
`KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1` is released. Build passing. Desktop end-to-end tests `6/6`. Infrastructure tests `23/23`. Application tests `60/60`. Domain tests `48/48`. AI tests `6/6`. Architecture tests `16/16`. Warnings `0`.

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Latest released baseline:
`KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1`

Next active package:
Knowledge Engine ingestion and chunking planning

Recent accomplishments:

- Established the initial .NET solution and layered project skeleton.
- Added architecture tests to enforce project reference rules and scaffold guardrails.
- Hardened build configuration with deterministic builds and warnings-as-errors.
- Added continuity files for future AI sessions.
- Released the scaffold baseline as `SKELETON-0.1`.
- Implemented the first Domain foundation with strongly typed IDs, lifecycle-aware aggregates, immutable raw notes, explicit save-transaction states, and append-only audit events.
- Verified restore, build, Domain tests, architecture tests, and full solution test execution.
- Released the initial Domain foundation as `DOMAIN-0.1`.
- Added the first Application foundation with command/query contracts, repository interfaces, `IUnitOfWork`, `IClock`, `ICurrentUser`, `IAuditRecorder`, and seven initial use cases.
- Added Application tests with in-memory fakes to verify orchestration, lifecycle guards, evidence interpretation boundaries, and report draft request shaping.
- Reduced the Application project to a minimal inward dependency on `SPINbuster.Domain` and added an eighth architecture guardrail to keep that reference set minimal.
- Hardened the Application layer so audit events are staged before commit instead of written post-commit.
- Added explicit `UpdateAsync` semantics for mutated loaded aggregates instead of relying on implicit tracking.
- Recorded `EDR-APP-001` and `EDR-APP-002` for command idempotency and draft-generation ownership.
- Expanded Application tests to cover staged audit ordering, commit failure behavior, staging failure behavior, explicit update semantics, and read-only query isolation.
- Released the initial Application foundation as `APPLICATION-0.1`.
- Added the local SQLite Infrastructure foundation with EF Core DbContext, entity configurations, strongly typed ID value conversions, repository implementations, staged audit persistence, and a `SqliteUnitOfWork`.
- Verified `dotnet ef migrations has-pending-model-changes` returns no pending changes.
- Released the local SQLite persistence foundation as `INFRASTRUCTURE-0.1`.
- Reworked `SPINbuster.Desktop` into a deterministic bootstrap console host and released `VERTICAL-SLICE-0.1`.
- Added the authoritative report-draft workflow and released `REPORT-DRAFT-SLICE-0.1`.
- Added the governed AI draft proposal substrate and released `AI-DRAFT-PROPOSAL-SLICE-0.1`.
- Extended the temporary Desktop host through the deterministic AI proposal workflow and released `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`.
- Added the first Knowledge Engine Domain and Application foundation with authoritative knowledge documents, immutable revisions, explicit supersession, project-scoped relationships, and precise citations.
- Added the first Knowledge Engine SQLite persistence slice and released `KNOWLEDGE-ENGINE-PERSISTENCE-0.1`.
- Extended the temporary Desktop host through the first executable local Knowledge Engine workflow.
- Added `AddKnowledgeCitation` plus `LoadProjectKnowledgeSnapshot` so the host can remain thin and Application-driven.
- Added SQLite-backed Desktop tests for successful Knowledge execution, reload, current revision selection, relationship traversal, citation reload, audit ordering, failure presentation, commit failure handling, and proof that report plus AI records remain unchanged.
- Recorded the executable-slice prototype review and deferred `EDR-KE-009` for Knowledge command idempotency before synchronization-oriented work.
- Released `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1` and pushed tag `knowledge-engine-executable-slice-0.1`.

Current architectural decisions:

- `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1` is the active released knowledge baseline.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- `SPINbuster.Shared` is constrained to narrow cross-boundary contracts and primitives.
- Adapter-to-adapter references are disallowed.
- The Domain layer remains dependency-free aside from the existing project reference to `SPINbuster.Shared`, and contains no EF, HTTP, serialization, AI, repository, or UI concerns.
- The Application layer currently depends only on `SPINbuster.Domain` and owns orchestration contracts rather than persistence, transport, or provider implementations.
- The Application layer stages audit facts before a single unit-of-work commit so state and audit persistence can share one logical transaction.
- Mutated loaded aggregates require explicit repository `UpdateAsync` calls in Application handlers.
- The local SQLite Infrastructure slice persists Domain aggregates through explicit mapping records rather than implicit EF tracking assumptions.
- The temporary Desktop host depends on `SPINbuster.Application`, `SPINbuster.Infrastructure`, and the deterministic `SPINbuster.AI` composition helper only.
- Knowledge Engine executable reload now flows through a presentation-safe project snapshot query rather than host-specific reconstruction logic.
- Knowledge Engine mutations still do not have a uniform `OperationId` replay contract; `EDR-KE-009` makes that required before synchronization or automated ingestion.

Next task:
Define the next Knowledge Engine package before starting ingestion, chunking, or broader retrieval workflows

Known issues:

- Most non-architecture test projects are still empty scaffolds and intentionally have no real test cases yet.
- Evidence interpretation is intentionally single-assignment for `DOMAIN-0.1`; richer interpretation history is deferred by `EDR-DOM-001`.
- The Desktop host is still a console bootstrapper and should not accumulate broader UI assumptions before the real client direction is chosen.
- SQLite migration execution still emits the previously accepted provider warning around the report-table rebuild path during a fresh executable run. The workflow completes successfully, but the warning should remain documented rather than ignored.
- Human-accepted AI proposals still do not create authoritative report revisions; that boundary is intentionally deferred by `EDR-AI-001`.
- Advanced retry orchestration and crash recovery remain intentionally deferred by `EDR-AI-002`.
- Knowledge Engine local SQLite persistence is implemented; parsing, OCR, embeddings, executable retrieval flows, and cross-project sharing remain intentionally deferred.
- Knowledge Engine command idempotency is still deferred and must be designed before synchronization or automated ingestion work.

Requested review:

- Whether the next Knowledge Engine package should focus first on ingestion and chunking boundaries or on broader retrieval/query shaping
- Whether `KnowledgeDocumentRevision` should remain aggregate-owned through persistence or later be promoted to a separate aggregate boundary
- Whether the stable-subject-key approach for relationship uniqueness should remain the long-term enforcement mechanism beyond local SQLite
- Whether human-accepted advisory proposals should create a new authoritative report revision in Domain or Application first

Current capabilities:

- Create project
- Start inspection session
- Capture immutable field notes
- Attach raw evidence
- Add one non-replaceable interpretation
- Assemble report-draft context
- Create authoritative revision-1 report drafts
- Persist provenance and audit history
- Retry draft creation safely through `OperationId`
- Build governed report-proposal context manifests
- Generate deterministic advisory AI proposals with no live provider dependency
- Persist model runs, run attempts, and advisory proposal manifests
- Reject advisory AI proposals through explicit review workflow
- Execute deterministic AI proposal request, replay, and review flows through the Desktop host
- Register authoritative knowledge documents
- Add, supersede, and verify knowledge revisions explicitly
- Create project-scoped knowledge relationships with contradiction visibility
- Load bounded knowledge neighborhoods through Application queries
- Persist and reload authoritative knowledge revisions, relationships, citations, and audit history through local SQLite
- Execute a deterministic Knowledge Engine document-registration, revisioning, supersession, relationship, and citation workflow through the Desktop host
- Reload a project-scoped Knowledge Engine snapshot with document identities, revision chains, current authoritative revisions, relationships, citations, and audit history
- Present expected Knowledge Engine failure cases without crashing the scripted executable path
