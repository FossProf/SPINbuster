# ADR-ARCH-001: Platform Engine Model

Date: 2026-07-17
Status: Accepted

## Context

SPINbuster now contains enough durable subsystems that a slice-by-slice roadmap is no longer sufficient by itself.

The repository already includes released foundations for:

- Domain and Application orchestration
- local SQLite persistence
- deterministic executable workflows
- Knowledge Engine persistence and execution
- Document Engine persistence and execution
- local filesystem storage for immutable imported content

As the platform grows, future work must remain clear about:

- who owns a capability
- where authoritative state belongs
- which components are replaceable providers
- how long-term planning should avoid overlap and architectural drift

The existing layered architecture already governs code dependencies, but it does not by itself explain long-term capability ownership across engines such as Knowledge, Rules, Retrieval, Context, AI Execution, Reporting, and Synchronization.

## Decision

SPINbuster adopts an explicit platform engine model as a capability-ownership architecture alongside its existing layered architecture.

The platform engine model defines major engines such as:

- Storage Engine
- Document Engine
- Parsing Engine
- Fragment Engine
- Knowledge Engine
- Rule Engine
- Retrieval Engine
- Context Engine
- AI Proposal Layer
- AI Execution Engine
- Provider Adapters
- Reporting Engine
- Synchronization Engine
- Presentation

These engines are ownership boundaries, not dependency boundaries.

The layered architecture remains:

- Domain
- Application
- Infrastructure
- Presentation
- Provider Adapters

The layered architecture governs project references and dependency direction.
The engine model governs capability ownership and long-term platform evolution.

## Rationale

### Why capability architecture now exists

The repository has matured beyond a small set of vertical slices.
Multiple durable capabilities now coexist and will continue to evolve:

- authoritative knowledge
- immutable evidence and imported documents
- deterministic rules
- governed context assembly
- advisory AI proposals
- authoritative reporting
- future synchronization and multi-client presentation

Without explicit capability ownership, these areas would tend to blur together and future slices would risk overlapping authority, especially around document understanding, knowledge promotion, reporting, and AI execution.

### Relationship to Clean Architecture

This decision does not replace Clean Architecture or layered architecture.

Instead:

- Clean Architecture continues to govern implementation direction and code dependencies.
- The engine model adds a second view for platform planning and capability ownership.

A capability such as the Knowledge Engine will still have:

- Domain invariants
- Application workflows
- Infrastructure persistence
- Presentation consumption

The engine model therefore complements Clean Architecture by clarifying who owns the capability while Clean Architecture clarifies how it is implemented.

### Why engines are ownership boundaries instead of dependency boundaries

Capability boundaries answer different questions than dependency boundaries.

Dependency boundaries answer:

- which projects may reference which
- where provider-specific types may appear
- how persistence and presentation stay outside core logic

Ownership boundaries answer:

- which engine owns a capability
- where authoritative state belongs
- which engine may evolve a concept
- where non-responsibility lines prevent overlap

Confusing these two views would cause capability diagrams to be misread as code reference diagrams.
The platform therefore documents them separately and explicitly.

### Why provider independence is fundamental

Provider independence is required because SPINbuster is intended to remain:

- offline-first
- portable across local and cloud execution environments
- resilient to model, parser, storage, and sync provider changes

This means:

- AI providers must be replaceable
- storage providers must be replaceable
- parsing and OCR providers must be replaceable
- future synchronization transports must be replaceable

The platform owns authoritative state, audit rules, and lifecycle semantics.
Providers implement replaceable capabilities behind that platform-owned behavior.

### Why Presentation intentionally remains a consumer

Presentation clients will continue to change over time:

- temporary Desktop bootstrap host
- future MAUI client
- possible server or web clients
- future mobile workflows

If Presentation becomes a business-logic owner, the platform loses provider independence, replayability, testability, and cross-client consistency.

Therefore Presentation remains a consumer of Application workflows and platform engines.
It may compose, display, and collect input, but it does not define engineering truth.

## Consequences

### Positive

- Future roadmap work can be organized by capability evolution instead of only by implementation slices.
- Authoritative ownership is clearer across Documents, Knowledge, Rules, Context, AI, Reporting, and Synchronization.
- Provider boundaries remain explicit and replaceable.
- Future clients can be added without redefining the core platform constitution.

### Tradeoffs

- The repository now carries two architectural views, which requires disciplined terminology.
- Governance documents must keep layered and capability language consistent.
- Some subsystems will collaborate heavily even though ownership remains single-engine.

## Follow-On Governance

This ADR should remain aligned with:

- `docs/00-governance/ARCHITECTURE_VISION.md`
- `docs/00-governance/CAPABILITY_MATRIX.md`
- `docs/00-governance/PLATFORM_COMPLETION_CRITERIA.md`
- `docs/00-governance/ROADMAP.md`
