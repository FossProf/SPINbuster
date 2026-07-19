# Repository Map

Purpose: Explain how the repository is organized and where different kinds of work belong.

## Top-Level Structure

- `.ai/` contains agent bootstrap guidance, current priorities, and operating rules.
- `PROJECT_STATE.md` provides the fastest project-status snapshot for new sessions.
- `spec/` contains authoritative engineering specifications.
- `docs/` contains human-readable documentation derived from specifications.
- `src/` contains implementation projects.
- `tests/` contains automated verification.
- `database/`, `schemas/`, `prompts/`, `tools/`, and `scripts/` contain supporting assets and workflows.

## Documentation And Decision Layout

- `docs/00-governance/` contains the durable repository governance layer.
- `docs/00-governance/ARCHITECTURE_VISION.md` defines the long-term platform constitution, including Layered Architecture and Capability Architecture.
- `docs/00-governance/CAPABILITY_MATRIX.md` assigns single-engine ownership for platform capabilities.
- `docs/00-governance/PLATFORM_COMPLETION_CRITERIA.md` defines objective completion gates for architecture, feature completeness, and commercial readiness.
- `docs/00-governance/ROADMAP.md` records the strategic roadmap and milestone direction.
- `docs/00-governance/ROADMAP.md` now organizes future work by capability evolution rather than only by isolated implementation slices.
- `docs/decisions/adr/` contains architecture decision records.
- `docs/decisions/edr/` contains engineering and product decision records.
- `docs/decisions/status/` contains baseline, review, and status records.
- `docs/03-implementation/IMPLEMENTATION_LOG.md` records completed milestones and the next implementation step.
- `docs/decisions/edr/EDR-KE-001` through `EDR-KE-012` record the current Knowledge Engine and Document Engine deferred or accepted boundaries.
- `docs/decisions/status/PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC-PROTOTYPE-REVIEW.md` captures the parsing foundation review findings and next-package recommendation.

## Specification Layout

- `spec/README.md` is the top-level navigation index for the authoritative specification repository.
- `spec/knowledge/README.md` defines the Knowledge Engine boundary at a high level.
- `spec/knowledge/engineering-object-model.md` defines the shared durable noun model used across Knowledge, Documents, Rules, Reports, and AI.
- `spec/knowledge/engineering-knowledge-model.md` defines the authoritative conceptual model for engineering knowledge.
- `spec/documents/README.md`, `spec/documents/document-engine-boundary.md`, and `spec/documents/document-engine-foundation.md` define the current Document Engine boundary and durable foundation.
- `spec/documents/parsing-and-fragment-foundation.md` defines the parsing and fragment foundation boundary with released status.
- `spec/rules/README.md` and `spec/rules/rule-engine-boundary.md` define the future Rule Engine boundary.
- `spec/database/` contains persistence specifications.
- `spec/ai/` contains durable AI behavior specifications.

## Source Projects

- `src/SPINbuster.Shared` contains only narrow cross-boundary contracts, primitives, identifiers, and serialization-safe shared DTO primitives.
- `src/SPINbuster.Domain` contains authoritative business concepts and invariants.
- `src/SPINbuster.Application` contains orchestration, command and query workflows, transaction boundaries, and project-scope enforcement.
- `src/SPINbuster.Infrastructure` contains persistence and adapter implementations.
- `src/SPINbuster.Documents` contains deterministic Document Engine adapters for hashing, media inspection, immutable content storage, fixture processing, the review-candidate local filesystem adapter, and the deterministic PlainTextDocument parser.
- `src/SPINbuster.Rules` is reserved for future deterministic rule definitions and evaluators.
- `src/SPINbuster.AI` contains AI integration adapters and advisory proposal support.
- `src/SPINbuster.Reporting` contains reporting composition and report output support.
- `src/SPINbuster.Server` contains the server host and composition root.
- `src/SPINbuster.Desktop` is currently a temporary bootstrap host and not yet a MAUI Blazor Hybrid application. It now contains the parsing executable workflow runner in addition to the earlier AI, Knowledge, and Document executable slices.

## Current Released Baseline

- `PARSING-AND-FRAGMENT-FOUNDATION-0.1` is the latest released baseline.

## Current Governance Baseline

- `ARCHITECTURE-VISION-2.0` is the latest frozen governance baseline.

## Next Active Package

- `PARSING-EXECUTABLE-SLICE-0.1-RC`

## Current Document Engine Flow

- The released executable slice adds a project-scoped Application snapshot query for document workflow state.
- The released local filesystem storage adapter replaces the Desktop host's fixture-only document bytes with a local filesystem immutable content store.
- The released parsing and fragment foundation adds deterministic text parsing, fragment candidate persistence, and snapshot reload through the Desktop host.
- The temporary Desktop host now exercises:
  - multi-source batch import
  - same-project duplicate reuse
  - cross-project duplicate privacy
  - deterministic processing outcomes
  - non-authoritative candidate review
  - repeated execution on reused SQLite databases without mutating prior runs
  - exact-byte reopen after provider recreation
  - missing and corrupt file detection against persisted bytes
  - bounded orphan visibility through adapter-specific inventory
  - reload of document audit history and authority-isolation state
  - deterministic text parsing with fragment candidate production
  - idempotent replay of parser runs across provider recreation
  - parser version coexistence with historical candidate preservation
  - unsupported media, cancelled, and malformed content failure handling
  - authority isolation from Knowledge, Report, and AI records through parsing
- The Infrastructure layer now also contains a host-facing database migrator abstraction so startup migration stays outside the Desktop host's direct EF Core concerns.
- The Desktop host now resolves its default durable document root under `%LOCALAPPDATA%\\SPINbuster\\document-content` rather than under build-output directories.

## Working Rule

Start from `.ai/` for navigation.
Read `PROJECT_STATE.md` first when rapid project-state context is needed.
Use `spec/` as the source of truth for design and behavior.
Use `docs/` for explanatory and reader-friendly material.
Treat roadmap capability phases as planning groupings, not as code dependency diagrams.
Treat Layered Architecture as the dependency model and Capability Architecture as the ownership model.
