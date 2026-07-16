# Roadmap

Status legend:

```text
Released
Active
Planned
Deferred
```

## Foundation And Proven Backend

- Repository and solution scaffold — Released
- Domain foundation — Released
- Application foundation — Released
- Local SQLite Infrastructure — Released
- Initial executable field workflow — Released
- Authoritative report-draft workflow — Released
- Governed AI proposal substrate — Released
- Deterministic AI proposal executable workflow — Released

## Current Major Phase

- Knowledge Engine foundation — Active
- Knowledge Engine persistence — Planned
- Knowledge Engine executable slice — Planned

## Subsequent Backend Phases

- Rule Engine — Planned
- Document ingestion and revision management — Planned
- Parsing and chunking — Deferred
- Retrieval and citations — Planned
- Local Ollama integration — Deferred
- Report revision acceptance from reviewed AI proposals — Planned
- Synchronization engine — Planned
- PostgreSQL server persistence — Planned
- API and server workflows — Planned
- conflict resolution — Planned
- backup and recovery — Planned

## Presentation And Product Phases

- UX and workflow design — Planned
- Windows MAUI Blazor Hybrid UI — Deferred
- calendar and scheduling — Planned
- project setup and configuration — Planned
- evidence and photo capture — Planned
- GPS and device metadata — Planned
- mobile companion — Deferred
- administrative tools — Planned
- commercial pilot — Planned

UI is intentionally delayed until the Knowledge, Rule, Document, and synchronization foundations are sufficiently stable.

## Milestone Exit Criteria

Foundation milestone:

- core model is explicit
- invariants are tested
- boundaries are documented
- architecture tests protect layering

Persistence milestone:

- migrations exist
- upgrade path is validated
- atomicity and rollback behavior are tested
- detached update semantics are clear

Executable slice milestone:

- one end-to-end workflow succeeds through the real composition root
- failures degrade predictably
- audit and reload behavior are verified
- prototype review captures what the slice actually proved
