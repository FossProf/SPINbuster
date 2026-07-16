# Knowledge Engine

## Purpose

The Knowledge Engine defines authoritative project knowledge records that remain useful with or without AI.

It exists to model stable document identity, immutable historical revisions, governed relationships, and precise citations so later retrieval, reporting, and AI workflows consume traceable engineering knowledge instead of ad hoc conversation memory.

## Foundational rules

- Knowledge records are authoritative project data.
- Project isolation is mandatory.
- Historical revisions are never silently overwritten.
- Superseded information remains queryable and traceable.
- AI may consume governed knowledge but may not create authoritative knowledge automatically.
- Derived conclusions must remain traceable to explicit source material.

## Core Domain concepts

- `KnowledgeDocument` is the stable project-scoped identity for a knowledge item.
- `KnowledgeDocumentRevision` represents immutable historical content metadata plus explicit supersession and verification state.
- `KnowledgeRelationship` connects knowledge documents or revisions without increasing their authority.
- `KnowledgeCitation` points to a precise location inside a cited revision without storing large document content in the Domain layer.

## Initial document types

- `Drawing`
- `Specification`
- `RFI`
- `Bulletin`
- `Submittal`
- `ChangeOrder`
- `Report`
- `FieldNote`
- `Evidence`
- `GeneralReference`

## Initial application capabilities

- Register a knowledge document
- Add an initial authoritative revision
- Supersede the current authoritative revision explicitly
- Change revision verification status explicitly
- Create project-scoped relationships
- Add a citation to a specific revision
- Load a knowledge document
- Load revision history
- Load a bounded knowledge neighborhood graph
- Load a presentation-safe project knowledge snapshot for executable workflows
- Persist and reload knowledge records through SQLite Infrastructure adapters

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

## Non-goals for the foundation slice

- Binary file storage ownership
- Parsing and chunking
- OCR
- Embeddings and vector search
- Automatic authority classification
- AI-generated relationship promotion
- Cross-project sharing
- Multi-current-revision conflict resolution

See `spec/architecture/knowledge-engine-foundation.md` and the deferred EDRs under `docs/decisions/edr/` for the current boundary.

The current persistence boundary is further specified in `spec/database/knowledge-engine-persistence.md`.
