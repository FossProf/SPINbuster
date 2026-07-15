# Current State

Repository status:
`APPLICATION-0.1` is now the latest released baseline. Build passing. Application tests `13/13`. Domain tests `24/24`. Architecture tests `8/8`. Warnings `0`.

Current branch:
`main`

Current milestone:
`Prototype Vertical Slice`

Current baseline:
`APPLICATION-0.1`

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

Current architectural decisions:

- `SKELETON-0.1` is the active scaffold baseline.
- `SPINbuster.Desktop` remains a temporary bootstrap host, not a MAUI application yet.
- `SPINbuster.Shared` is constrained to narrow cross-boundary contracts and primitives.
- Adapter-to-adapter references are disallowed.
- The Domain layer remains dependency-free aside from the existing project reference to `SPINbuster.Shared`, and contains no EF, HTTP, serialization, AI, repository, or UI concerns.
- The Application layer currently depends only on `SPINbuster.Domain` and owns orchestration contracts rather than persistence, transport, or provider implementations.
- The Application layer stages audit facts before a single unit-of-work commit so state and audit persistence can share one logical transaction.
- Mutated loaded aggregates require explicit repository `UpdateAsync` calls in Application handlers.
- `EDR-DOM-001` defers versioned evidence interpretation history; current behavior is single-assignment with no silent replacement.
- `EDR-APP-001` defers command idempotency until retry and synchronization work begins.
- `EDR-APP-002` fixes `GenerateReportDraftRequest` as a side-effect-free query that assembles drafting context only.

Next task:
Application-to-Infrastructure persistence seam design

Known issues:

- Most non-architecture test projects are still empty scaffolds and intentionally have no real test cases yet.
- Evidence interpretation is intentionally single-assignment for `DOMAIN-0.1`; richer interpretation history is deferred by `EDR-DOM-001`.
- Infrastructure implementations do not exist yet; the repository is still at the contract-and-orchestration stage for persistence concerns.

Requested review:

- Application-to-Infrastructure persistence seam before SQLite work begins
- Whether `ICurrentUser` should stay `string` or move to a typed identifier in the next baseline
