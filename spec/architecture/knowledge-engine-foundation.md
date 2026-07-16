# Knowledge Engine Foundation

## Scope

`KNOWLEDGE-ENGINE-FOUNDATION-0.1-RC` introduces the first Domain and Application foundation for authoritative project knowledge.

This slice is limited to:

- project-scoped knowledge documents
- immutable revision history
- explicit supersession
- verification state
- governed relationships
- precise citations
- provider-neutral application contracts and use cases

This slice excludes:

- file storage
- parsing
- OCR
- embeddings
- vector search
- live AI retrieval
- synchronization
- UI workflows

## Ownership boundaries

- `SPINbuster.Domain` owns knowledge invariants and lifecycle rules.
- `SPINbuster.Application` owns orchestration, repository contracts, and transaction boundaries.
- `SPINbuster.Infrastructure` owns persistence mappings and query implementations for the local SQLite path.
- `SPINbuster.AI` may consume governed knowledge but does not author authoritative knowledge in this slice.
- `SPINbuster.Documents` may later adapt document inputs and exports but does not mutate Knowledge Engine persistence directly.

## Aggregate decisions

- `KnowledgeDocument` is the aggregate root for stable identity, lifecycle, current-authoritative revision tracking, and document-local audit history.
- `KnowledgeDocumentRevision` is a durable entity under document ownership with immutable authored data and explicit mutable lifecycle or verification fields only.
- `KnowledgeRelationship` is a separate durable record with its own audit facts because contradictions must remain visible independently of document revision chains.
- `KnowledgeCitation` is a durable citation record that points to a revision location without embedding full document content.

## Lifecycle rules

- A document has a stable identity across revisions.
- A document may have at most one current authoritative revision in this slice.
- The first authoritative revision is added explicitly.
- Later authoritative revisions must explicitly supersede the current authoritative revision.
- Supersession cannot cross document boundaries.
- Archived documents cannot receive new active revisions until restored.
- Superseded revisions remain queryable and traceable.

## Query boundary

Knowledge neighborhood loading returns a bounded graph snapshot.

The executable local slice also permits a read-only project knowledge snapshot that returns:

- stable document identities
- revision chains
- current authoritative revisions
- citations
- bounded relationships
- audit history

That snapshot must remain presentation-safe and must not expose mutable Domain aggregates directly.

It must not expose:

- database query objects
- EF tracking artifacts
- file-system handles
- provider-specific retrieval types

## Audit boundary

Mutating use cases stage audit facts before one unit-of-work commit.

The foundation slice distinguishes:

- document registration
- revision creation
- revision supersession
- verification change
- relationship creation
- contradiction detection
- citation creation
