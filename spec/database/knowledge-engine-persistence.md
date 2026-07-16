# Knowledge Engine Persistence

## Scope

`KNOWLEDGE-ENGINE-PERSISTENCE-0.1-RC` introduces the first durable SQLite persistence layer for the Knowledge Engine.

This slice includes:

- `knowledge_documents`
- `knowledge_document_revisions`
- `knowledge_relationships`
- `knowledge_citations`
- strongly typed ID conversions at the Infrastructure boundary
- detached update support for document and relationship persistence
- project-scoped retrieval and bounded relationship traversal
- migration validation from the released AI executable baseline

This slice excludes:

- binary document storage
- parsing and OCR
- embeddings or vector search
- AI retrieval orchestration
- synchronization
- HTTP APIs
- UI workflows

## Persistence rules

- `KnowledgeDocument` remains the authoritative stable identity for a project-scoped knowledge item.
- `KnowledgeDocumentRevision` persists immutable authored metadata plus explicit lifecycle, verification, and supersession links.
- `KnowledgeRelationship` persists as an independent durable record and uses stable subject keys to enforce duplicate prevention under SQLite.
- `KnowledgeCitation` persists as a revision-scoped locator record and does not own document content.

## Query boundary

Infrastructure repositories may load:

- a project's knowledge documents
- a document's current authoritative revision
- full revision history
- citations by revision
- bounded neighborhoods by subject

Infrastructure repositories must not expose:

- `IQueryable`
- EF tracking artifacts
- `DbContext`
- persistence record types outside `SPINbuster.Infrastructure`

## Migration expectations

- `MigrateAsync()` must succeed on an empty SQLite database.
- `MigrateAsync()` must succeed on a populated `AI-PROPOSAL-EXECUTABLE-SLICE-0.1` database.
- repeated `MigrateAsync()` execution must remain idempotent.
- `__EFMigrationsHistory` must record the Knowledge Engine migration.
- existing released project, inspection, report, AI proposal, model-run, and audit data must survive unchanged.

## Transaction expectations

- knowledge document, revision, and staged audit changes commit together.
- knowledge document, revision, and staged audit changes roll back together.
- detached updates must not rely on implicit EF tracking behavior.

## Current migration artifact

- `src/SPINbuster.Infrastructure/Persistence/Migrations/20260716180657_KnowledgeEnginePersistenceRc.cs`
