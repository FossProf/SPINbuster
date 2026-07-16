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
- `docs/00-governance/ROADMAP.md` records the strategic roadmap and milestone direction.
- `docs/decisions/adr/` contains architecture decision records.
- `docs/decisions/edr/` contains engineering and product decision records.
- `docs/decisions/status/` contains baseline, review, and status records.
- `docs/03-implementation/IMPLEMENTATION_LOG.md` records completed milestones and the next implementation step.
- `docs/decisions/edr/EDR-KE-001` through `EDR-KE-012` record the current Knowledge Engine and Document Engine deferred or accepted boundaries.

## Specification Layout

- `spec/README.md` is the top-level navigation index for the authoritative specification repository.
- `spec/knowledge/README.md` defines the Knowledge Engine boundary at a high level.
- `spec/knowledge/engineering-object-model.md` defines the shared durable noun model used across Knowledge, Documents, Rules, Reports, and AI.
- `spec/knowledge/engineering-knowledge-model.md` defines the authoritative conceptual model for engineering knowledge.
- `spec/documents/README.md` and `spec/documents/document-engine-boundary.md` define the future Document Engine boundary.
- `spec/rules/README.md` and `spec/rules/rule-engine-boundary.md` define the future Rule Engine boundary.
- `spec/database/` contains persistence specifications.
- `spec/ai/` contains durable AI behavior specifications.

## Source Projects

- `src/SPINbuster.Shared` contains only narrow cross-boundary contracts, primitives, identifiers, and serialization-safe shared DTO primitives.
- `src/SPINbuster.Domain` contains authoritative business concepts and invariants.
- `src/SPINbuster.Application` contains orchestration, command and query workflows, transaction boundaries, and project-scope enforcement.
- `src/SPINbuster.Infrastructure` contains persistence and adapter implementations.
- `src/SPINbuster.Documents` is reserved for future binary import, parsing, and extraction workflows.
- `src/SPINbuster.Rules` is reserved for future deterministic rule definitions and evaluators.
- `src/SPINbuster.AI` contains AI integration adapters and advisory proposal support.
- `src/SPINbuster.Reporting` contains reporting composition and report output support.
- `src/SPINbuster.Server` contains the server host and composition root.
- `src/SPINbuster.Desktop` is currently a temporary bootstrap host and not yet a MAUI Blazor Hybrid application.

## Current Released Baseline

- `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1` is the latest released baseline.

## Active Review Candidate

- `ENGINEERING-KNOWLEDGE-MODEL-0.1-RC`

## Next Planned Implementation Package

- `DOCUMENT-ENGINE-FOUNDATION-0.1-RC`

## Working Rule

Start from `.ai/` for navigation.
Read `PROJECT_STATE.md` first when rapid project-state context is needed.
Use `spec/` as the source of truth for design and behavior.
Use `docs/` for explanatory and reader-friendly material.
