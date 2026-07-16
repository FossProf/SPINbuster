# Current State

Repository status:
`AI-DRAFT-PROPOSAL-SLICE-0.1` is released. Build passing. Desktop end-to-end tests `2/2`. Infrastructure tests `14/14`. Application tests `32/32`. Domain tests `36/36`. AI tests `6/6`. Architecture tests `12/12`. Warnings `0`.

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Current baseline:
`AI-DRAFT-PROPOSAL-SLICE-0.1`

Recent accomplishments:

- Established the initial .NET solution and layered project skeleton.
- Added architecture tests to enforce dependency direction and scaffold rules.
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
- Added a scaffolded initial EF Core migration and design-time DbContext factory for the SQLite persistence slice.
- Added SQLite integration tests proving commit-together behavior, rollback on staged-audit failure, and explicit detached update support for `Project` and `InspectionSession`.
- Added narrow Domain rehydration hooks so Infrastructure can rebuild released aggregates without reflection or persistence leakage into Application contracts.
- Aligned `SPINbuster.Server` with the Infrastructure DbContext so the official EF Core startup path can validate migrations.
- Verified `dotnet ef migrations has-pending-model-changes` returns no pending changes.
- Verified `MigrateAsync()` from an empty SQLite database, migration-history recording, and second-run idempotence.
- Released the local SQLite persistence foundation as `INFRASTRUCTURE-0.1`.
- Added `AddSpinbusterApplication()` and `AddSpinbusterSqliteInfrastructure()` composition helpers so hosts can wire the existing handlers and SQLite-backed repositories consistently.
- Added a read-only Application query to reload a persisted `Project` plus `InspectionSession` snapshot, including field notes and audit history, after command commits.
- Reworked `SPINbuster.Desktop` into a deterministic bootstrap console host that applies migrations, executes Create Project -> Start Inspection Session -> Capture Field Note, reloads persisted state, and prints IDs, lifecycles, and audit history.
- Added Desktop end-to-end tests proving the workflow runs against a real SQLite file and can be reloaded from a fresh service provider.
- Corrected `StartInspectionSessionUseCase` so brand-new inspection sessions persist both the creation audit event and the start audit event in the same commit.
- Switched `SqliteInspectionSessionRepository` collection loads to split queries to avoid EF multiple-collection include runtime warnings during the vertical slice.
- Released the first local executable vertical slice as `VERTICAL-SLICE-0.1`.
- Recorded the post-release prototype review milestone for `VERTICAL-SLICE-0.1`, including validated assumptions, uncovered defects, DI/rehydration/migration/audit/query friction, and the temporary Desktop host assessment.
- Added typed Application-layer identity contracts with `ApplicationUserId` and `OperationId`.
- Extended the Domain report model with structured draft sections, explicit revision numbers, and persisted provenance to source field notes and evidence attachments.
- Added `CreateReportDraftCommand` plus a read-only report reload query while keeping `GenerateReportDraftRequest` side-effect free.
- Added SQLite persistence for reports, source-reference mappings, operation-id mappings, and detached report rehydration.
- Added Infrastructure tests for report persistence, idempotent operation mapping, and atomic rollback of report state plus audit staging.
- Extended the Desktop bootstrap workflow through Attach Evidence -> Add Interpretation -> Assemble Draft Context -> Create Report Draft -> Reload Report -> Display report audit history.
- Validated the executable slice against a fresh SQLite file after migrations.
- Released the authoritative report-draft vertical slice as `REPORT-DRAFT-SLICE-0.1`.
- Recorded the post-release prototype review milestone for `REPORT-DRAFT-SLICE-0.1`, including migration behavior, idempotency, provenance validation, and report-section revisioning assessment.
- Released the governed AI draft proposal substrate with governed context manifests, provider-neutral generation contracts, deterministic Tier 0 provider adapters, structured proposal validation, durable model-run and proposal persistence, and AI-specific architecture guardrails.
- Added a report-draft proposal JSON Schema under `schemas/ai/` and updated the authoritative AI subsystem specifications under `spec/ai/`.
- Added AI-focused Domain, Application, AI, Infrastructure, and Architecture tests and validated the full solution with zero warnings.
- Hardened the AI review candidate so human proposal disposition is separate from technical model-run closure, requested runs persist before provider execution, and canonical proposal payloads are stored for review.

Current architectural decisions:

- `REPORT-DRAFT-SLICE-0.1` is the active released baseline.
- `AI-DRAFT-PROPOSAL-SLICE-0.1` is the active released AI baseline.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- `SPINbuster.Shared` is constrained to narrow cross-boundary contracts and primitives.
- Adapter-to-adapter references are disallowed.
- The Domain layer remains dependency-free aside from the existing project reference to `SPINbuster.Shared`, and contains no EF, HTTP, serialization, AI, repository, or UI concerns.
- The Application layer currently depends only on `SPINbuster.Domain` and owns orchestration contracts rather than persistence, transport, or provider implementations.
- The Application layer stages audit facts before a single unit-of-work commit so state and audit persistence can share one logical transaction.
- Mutated loaded aggregates require explicit repository `UpdateAsync` calls in Application handlers.
- The local SQLite Infrastructure slice persists Domain aggregates through explicit mapping records rather than implicit EF tracking assumptions.
- The Infrastructure slice uses staged audit persistence inside the same unit-of-work commit as aggregate state changes.
- The startup-project and design-time DbContext paths now use aligned SQLite provider configuration for migration tooling.
- The temporary Desktop host depends only on `SPINbuster.Application` and `SPINbuster.Infrastructure`.
- The first vertical slice reloads persisted state through an Application query instead of reading EF models directly in the host.
- `EDR-DOM-001` defers versioned evidence interpretation history; current behavior is single-assignment with no silent replacement.
- `EDR-APP-001` is now accepted for the report-draft slice: `CreateReportDraftCommand` uses `OperationId`, and Infrastructure enforces uniqueness for that authoritative outcome.
- `EDR-APP-002` fixes `GenerateReportDraftRequest` as a side-effect-free query that assembles drafting context only.
- `EDR-AI-001` defers authoritative report revision creation from human-accepted AI proposals; the current slice persists human review disposition without mutating authoritative reports.
- `EDR-AI-002` defers concurrent duplicate-resolution and crash-recovery rules for AI proposal requests until live-provider integration.
- AI provider support remains intentionally limited to the deterministic fixture in this baseline.
- AI remains operationally optional; the deterministic Tier 0 provider exercises the full proposal pipeline without any live AI dependency.
- AI proposals, model runs, run attempts, and governed context manifests commit through the existing unit-of-work boundary alongside audit records.

Next task:
Define and review the next authoritative AI acceptance slice

Known issues:

- Most non-architecture test projects are still empty scaffolds and intentionally have no real test cases yet.
- Evidence interpretation is intentionally single-assignment for `DOMAIN-0.1`; richer interpretation history is deferred by `EDR-DOM-001`.
- The Desktop host is still a console bootstrapper and should not accumulate broader UI assumptions before the real client direction is chosen.
- The SQLite report migration emits EF's expected non-transactional SQLite table-rebuild warning because the released report table shape changed from the earlier baseline; the migrated workflow still completes successfully from a fresh database.
- Human-accepted AI proposals still do not create authoritative report revisions; that boundary is intentionally deferred by `EDR-AI-001`.
- Advanced retry orchestration and crash recovery remain intentionally deferred by `EDR-AI-002`.

Requested review:

- Whether human-accepted advisory proposals should create a new authoritative report revision in Domain or Application first
- Whether prompt-package registry metadata should remain repository-owned or later become persisted configuration

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
