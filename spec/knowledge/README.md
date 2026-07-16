# Knowledge Engine

## Purpose

The Knowledge Engine defines authoritative project knowledge records that remain useful with or without AI.

It exists to model stable document identity, immutable historical revisions, governed relationships, precise citations, and the broader engineering-knowledge concepts that later retrieval, reporting, deterministic rules, and AI workflows consume.

## Foundational rules

- Knowledge records are authoritative project data.
- Project isolation is mandatory.
- Historical revisions are never silently overwritten.
- Superseded information remains queryable and traceable.
- AI may consume governed knowledge but may not create authoritative knowledge automatically.
- Derived conclusions must remain traceable to explicit source material.

## Core conceptual terms

- source material
- knowledge document
- knowledge revision
- knowledge fragment
- citation
- engineering assertion
- observation
- requirement
- constraint
- deterministic rule
- interpretation
- proposal
- relationship
- conflict
- applicability
- authority
- verification
- provenance

The authoritative conceptual model now lives in `spec/knowledge/engineering-knowledge-model.md`.

## Current released implementation scope

Current released capabilities include:

- register a knowledge document
- add an initial authoritative revision
- supersede the current authoritative revision explicitly
- change revision verification status explicitly
- create project-scoped relationships
- add a citation to a specific revision
- load a knowledge document
- load revision history
- load a bounded knowledge neighborhood graph
- load a presentation-safe project knowledge snapshot
- persist and reload knowledge records through SQLite Infrastructure adapters

## Executable workflow boundary

The first executable local Knowledge Engine slice proves:

- document registration
- revision supersession
- relationship creation
- citation persistence
- project knowledge snapshot reload
- audit-history presentation
- predictable failure presentation for invalid workflow attempts

The executable host must remain thin and may call Application commands and queries only.

Knowledge Engine mutation idempotency beyond conservative duplicate rejection is deferred by `docs/decisions/edr/EDR-KE-009-knowledge-command-idempotency.md`.

## Non-goals still deferred

- Binary file storage ownership
- Parsing and chunking
- OCR
- Embeddings and vector search
- Automatic authority classification
- AI-generated relationship promotion
- Cross-project sharing
- Multi-current-revision conflict resolution

See `spec/architecture/knowledge-engine-foundation.md`, `spec/knowledge/engineering-knowledge-model.md`, and the deferred EDRs under `docs/decisions/edr/` for the current boundary.
