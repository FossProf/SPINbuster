# Roadmap

Purpose: Define SPINbuster capability evolution from the current released baseline through commercial deployment.

This roadmap preserves historical released milestones as repository history.
It does not rewrite prior releases.

## Roadmap Principles

- Capability architecture is organized by platform ownership and evolution, not by code-project dependency.
- Semantic retrieval supplements deterministic retrieval; it does not replace it.
- Local and cloud AI providers are interchangeable behind governed provider boundaries.
- Human review remains authoritative for engineering conclusions and promoted knowledge.
- UI is intentionally delayed until the platform engines and workflows are stable.
- Future clients consume Application workflows rather than embedding business logic in presentation layers.
- Historical released milestones remain part of the permanent engineering record.

## Historical Released Milestones

Chronological released history:

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

## Current State

- Latest released baseline: `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`
- Active release candidate: `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` (validated, not released)
- Next active package recommendation: TBD after RC release decision

Why this package was next:

- The fragment candidate review lifecycle was released with executable proof.
- The next increment extended Desktop proof to exercise structural text extraction, parser diagnostics, and the parser registry.
- This stayed focused on the executable proof without prematurely broadening into Knowledge promotion, OCR, or AI extraction.

Why release is deferred:

- The RC validates structured text parsing, diagnostics, and the parser registry, but the review found that diagnostic aggregation, export, and cross-run comparison are production concerns that may benefit from further iteration before release.
- Knowledge promotion from reviewed candidates (`EDR-KE-011`) is the natural next capability phase and may influence how diagnostics interact with promotion workflows.

## Released Foundations

Purpose:
Establish the layered core, released persistence path, deterministic executable workflows, and durable offline-first foundations already proven in the repository.

Major packages:

- Repository and solution scaffold
- Domain foundation
- Application foundation
- Local SQLite Infrastructure
- Initial executable field workflow
- Authoritative report-draft workflow
- Governed AI proposal substrate
- Deterministic AI proposal executable workflow
- Knowledge Engine foundation
- Knowledge Engine persistence
- Knowledge Engine executable slice
- Document Engine foundation
- Document Engine executable slice
- Local filesystem storage adapter

Dependencies:

- None; this is the historical base that later phases depend on.

Exit criteria:

- Core layered architecture exists and builds cleanly.
- Local SQLite persistence exists with validated migrations.
- Deterministic executable workflows prove Application orchestration end to end.
- Durable byte storage exists for imported document sources.

Expected executable slice:

- Released and already achieved through Knowledge and Document executable workflows.

## Document Understanding

Purpose:
Convert durable imported bytes into governed, non-authoritative structural understanding that the rest of the platform can evaluate, promote, retrieve, and cite.

Major packages:

- Parsing and fragment foundation
- Parser orchestration boundary
- Fragment candidate persistence and review workflow
- Deterministic parser fixtures
- OCR boundary definition
- Candidate-promotion prerequisites for fragments, assertions, and citations

Dependencies:

- Released Document Engine foundation
- Released local filesystem storage adapter
- Existing project-scoped Application workflows and audit model

Exit criteria:

- Imported sources can produce governed fragment candidates without widening authority.
- Fragment identity, locator semantics, and provenance are explicit.
- Parser failures, abstentions, and partial outcomes are durable and reviewable.
- OCR remains optional and replaceable rather than baked into the core model.

Expected executable slice:

- Import source -> parse deterministically -> persist fragment candidates -> reload candidates -> review dispositions -> verify no authoritative knowledge mutation.

## Knowledge Promotion

Purpose:
Promote reviewed document understanding into authoritative engineering knowledge while preserving provenance, supersession, and contradiction boundaries.

Major packages:

- Fragment-to-knowledge promotion workflows
- Engineering assertion promotion
- Citation promotion from reviewed candidates
- Knowledge conflict and contradiction handling
- Knowledge graph expansion from promoted artifacts

Dependencies:

- Document Understanding
- Existing Knowledge Engine persistence and executable baseline

Exit criteria:

- Human-reviewed candidates can become authoritative knowledge records.
- Promotion preserves immutable source provenance and revision linkage.
- Conflicts and contradiction states are explicit rather than hidden.

Expected executable slice:

- Review fragment candidates -> promote selected artifacts -> persist knowledge revisions and citations -> reload graph and audit history.

## Rule Engine

Purpose:
Evaluate authoritative knowledge and project context through deterministic engineering rules.

Major packages:

- Rule Engine foundation
- Rule definition model
- Rule evaluation workflow
- Rule evidence binding and justification model
- Rule-result persistence and reload

Dependencies:

- Knowledge Promotion
- Stable engineering object model

Exit criteria:

- Rules evaluate against authoritative knowledge, not raw AI output.
- Rule results record evidence, rationale, uncertainty, and lifecycle.
- Deterministic rule execution is presentation-agnostic and provider-independent.

Expected executable slice:

- Load authoritative project knowledge -> evaluate selected rules -> persist rule results -> reload findings and supporting citations.

## Retrieval Engine

Purpose:
Serve bounded, explainable retrieval over authoritative knowledge and governed document understanding.

Major packages:

- Deterministic retrieval foundation
- Citation-aware retrieval
- Query model for project, document, fragment, knowledge, and rule results
- Semantic retrieval supplementation boundary
- Ranking and relevance evaluation

Dependencies:

- Knowledge Promotion
- Rule Engine
- Document Understanding

Exit criteria:

- Deterministic retrieval works over authoritative and review-safe artifacts.
- Semantic retrieval is supplemental, replaceable, and measured rather than authoritative.
- Retrieval responses remain explainable, bounded, and provenance-linked.

Expected executable slice:

- Query project corpus -> retrieve deterministic matches and semantic supplements -> display ranked results with citations and evidence links.

## Context Engine

Purpose:
Assemble governed context manifests for downstream rule evaluation, AI execution, and reporting workflows.

Major packages:

- Context manifest expansion beyond reporting
- Scope policies for retrieval, authority, supersession, and conflict inclusion
- Context budgeting and truncation strategy
- Manifest hashing, replay, and audit behavior

Dependencies:

- Retrieval Engine
- Knowledge Promotion
- Rule Engine

Exit criteria:

- Context manifests can be assembled for multiple downstream consumers, not only reporting proposals.
- Context inclusion policy is explicit, auditable, and replayable.
- Bounded context assembly works offline and remains provider-independent.

Expected executable slice:

- Select project/report/task scope -> assemble governed context manifest -> persist manifest -> reload manifest and explain inclusions.

## AI Execution

Purpose:
Execute governed AI workloads against context manifests while preserving non-authoritative output boundaries and provider interchangeability.

Major packages:

- AI execution engine
- Provider adapter expansion for local and cloud providers
- Provider capability negotiation
- Structured output validation and repair policy
- AI execution observability and evaluation harness

Dependencies:

- Context Engine
- Existing AI proposal substrate and executable workflow

Exit criteria:

- Local and cloud providers can be swapped without changing authoritative workflows.
- AI outputs remain advisory, structured, validated, and auditable.
- Provider failures, retries, and compatibility differences are durable and reviewable.

Expected executable slice:

- Assemble context manifest -> execute proposal through interchangeable provider adapter -> validate structured output -> persist proposal and run history -> reload without authoritative mutation.

## Authoritative Reporting

Purpose:
Convert reviewed knowledge, rule results, and accepted AI or human proposals into authoritative engineering reports and revisions.

Major packages:

- Report revision workflow
- Accepted AI proposal to authoritative revision workflow
- Human-authored authoritative revision workflow
- Report provenance expansion
- Export and publication boundaries

Dependencies:

- Knowledge Promotion
- Rule Engine
- Context Engine
- AI Execution

Exit criteria:

- Authoritative report creation is explicit, audited, and human-controlled.
- Report revisions preserve provenance to knowledge, evidence, rules, and accepted proposals.
- Advisory AI artifacts never directly mutate authoritative report history.

Expected executable slice:

- Load report context -> review evidence, knowledge, and proposals -> create authoritative revision -> reload revision chain and provenance.

## Synchronization

Purpose:
Extend the offline-first platform to support safe exchange between local and remote nodes without eroding authority, auditability, or deterministic conflict handling.

Major packages:

- Synchronization engine
- Command and import idempotency completion
- Conflict detection and resolution workflows
- Remote persistence adapter path
- Backup and recovery strategy

Dependencies:

- Stable local platform engines
- Authoritative Reporting
- Knowledge Promotion

Exit criteria:

- Offline-first local operation remains primary.
- Sync can exchange authoritative and review-state changes safely.
- Conflicts are surfaced explicitly with deterministic resolution policy.

Expected executable slice:

- Create local changes offline -> synchronize to second node -> detect and resolve conflicts -> verify durable convergence and audit history.

## Presentation

Purpose:
Deliver stable user-facing clients on top of Application workflows after platform boundaries are mature enough to avoid UI-driven architecture drift.

Major packages:

- UX and workflow design
- Windows MAUI Blazor Hybrid client
- Administrative tools
- Mobile companion
- Field evidence and capture flows

Dependencies:

- Document Understanding
- Knowledge Promotion
- Rule Engine
- Retrieval Engine
- Context Engine
- Authoritative Reporting
- Synchronization

Exit criteria:

- Presentation contains no business logic.
- Clients consume Application workflows and platform engines through stable contracts.
- Core workflows remain operable across desktop and future mobile clients.

Expected executable slice:

- End-to-end client workflow for project setup, field capture, review, reporting, and sync using existing Application contracts.

## Commercial Readiness

Purpose:
Prepare the platform for operational deployment, supportability, governance, and commercial pilot usage.

Major packages:

- Deployment packaging
- Upgrade and migration strategy
- Security hardening
- Operational observability
- Pilot governance and tenant safeguards
- Support tooling and recovery workflows

Dependencies:

- Presentation
- Synchronization
- Authoritative Reporting
- AI Execution

Exit criteria:

- Platform Architecture 1.0 is stable and documented.
- Operational security, backup, migration, and observability are in place.
- Commercial pilot workflows are supportable without architectural shortcuts.

Expected executable slice:

- Install or provision environment -> migrate data -> execute representative project workflow -> monitor health, recovery, and support surfaces.

## Cross-Cutting Tracks

### Observability

- Audit coverage across every engine
- Runtime telemetry for workflows, providers, sync, and retrieval
- Operational diagnostics for migration, storage, parser, and provider failures

### Security

- Local data protection and device-bound storage posture
- Provider boundary hardening
- Prompt and output safety controls
- Least-authority handling for synchronization and administration

### Performance

- SQLite query-shaping discipline
- Bounded graph and retrieval traversal
- Incremental parsing and indexing strategies
- Provider latency and throughput measurement

### Evaluation

- Deterministic fixture evaluation
- Retrieval quality measurement
- Rule-result correctness baselines
- AI proposal and provider regression suites

### Data Migration

- Schema migration discipline
- Filesystem object compatibility
- Knowledge and report model evolution
- Sync-safe upgrade sequencing

### Provider Compatibility

- Local and cloud AI parity expectations
- Parser and OCR adapter interchangeability
- Storage replacement boundary
- Future database adapter portability

### Testing Strategy

- Domain invariants stay unit-tested
- Application orchestration stays contract-tested
- Architecture guardrails remain enforced
- Infrastructure integration continues against real SQLite and local storage
- Executable slices continue proving end-to-end workflow boundaries

## Release And Slice Expectations

- Every major phase should culminate in at least one executable slice through the real composition root.
- Capability phases may span multiple implementation packages, but executable proof remains mandatory before a phase is considered mature.
- Historical slice names remain valid repository history even as the roadmap now groups work by capability evolution.
