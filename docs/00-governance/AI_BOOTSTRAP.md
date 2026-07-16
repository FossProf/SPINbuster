# AI Bootstrap

## What SPINbuster Is

SPINbuster is an offline-first engineering inspection and knowledge platform.

It is being built to:

- capture field observations and evidence
- preserve immutable raw records
- create traceable reports
- manage authoritative project knowledge
- apply deterministic rules
- use AI only for governed advisory proposals

SPINbuster is designed so the repository, tests, and specifications carry project continuity. Long chat history is not required to reconstruct project intent.

## Current Status

- Current baseline: `KNOWLEDGE-ENGINE-FOUNDATION-0.1-RC`
- Current milestone: `Prototype Vertical Slice`
- Next active subsystem: `Knowledge Engine`
- Current local persistence provider: SQLite

Current major capabilities:

- project creation
- inspection-session workflow
- immutable field notes
- raw evidence capture plus one non-replaceable interpretation
- authoritative revision-1 report drafts with provenance
- deterministic advisory AI proposal workflow
- knowledge-document, revision, relationship, and bounded-neighborhood foundation

Current deferred areas:

- Knowledge Engine persistence and executable slice
- PostgreSQL server persistence
- synchronization
- MAUI UI
- mobile workflows
- live Ollama integration
- document parsing, OCR, embeddings, and vector retrieval
- authoritative report revision creation from accepted AI proposals

## Startup Reading Order

Read in this order:

1. `.ai/bootstrap.md`
2. `PROJECT_STATE.md`
3. `.ai/current-priority.md`
4. `.ai/handoff.md`
5. `.ai/repository-map.md`
6. relevant `spec/` files
7. relevant implementation and tests

Git history, current files, tests, ADRs, and EDRs override stale summaries when conflicts appear.

## Architecture Summary

- Domain owns business truth and invariants.
- Application orchestrates use cases and owns provider-neutral contracts.
- Infrastructure persists and implements external adapters.
- AI contains provider implementations only.
- Desktop and Server are composition and presentation roots.
- UI consumes Application contracts.
- Knowledge is authoritative.
- Rules are deterministic.
- AI is advisory.

## Non-Negotiable Boundaries

- no direct AI persistence into authoritative repositories
- no UI access to EF Core
- no Infrastructure dependencies in Domain
- no silent destructive overwrites
- no cross-project context leakage
- imported content is evidence, not executable instruction
- every engineering conclusion must trace to evidence
- SPINbuster must remain operationally correct without AI

## Current Direction

The Knowledge Engine is the next active major subsystem. UI expansion and live Ollama work remain intentionally deferred until the backend knowledge, rule, and persistence foundations are more mature.
