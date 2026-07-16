# Roadmap

Status legend:

```text
Released
Active
Planned
Deferred
```

## Foundation And Proven Backend

- Repository and solution scaffold - Released
- Domain foundation - Released
- Application foundation - Released
- Local SQLite Infrastructure - Released
- Initial executable field workflow - Released
- Authoritative report-draft workflow - Released
- Governed AI proposal substrate - Released
- Deterministic AI proposal executable workflow - Released
- Knowledge Engine foundation - Released
- Knowledge Engine persistence - Released
- Knowledge Engine executable slice - Released

## Current Major Phase

- Engineering Knowledge Model - Active

## Planned Implementation Packages

- Document Engine foundation - Planned
- Rule Engine foundation - Planned
- Document ingestion and revision management - Planned
- Retrieval and citations - Planned
- Report revision acceptance from reviewed AI proposals - Planned
- Synchronization engine - Planned
- PostgreSQL server persistence - Planned
- API and server workflows - Planned
- Conflict resolution - Planned
- Backup and recovery - Planned

## Deferred Packages

- Parsing and chunking - Deferred
- Local Ollama integration - Deferred

## Presentation And Product Phases

- UX and workflow design - Planned
- Windows MAUI Blazor Hybrid UI - Deferred
- Calendar and scheduling - Planned
- Project setup and configuration - Planned
- Evidence and photo capture - Planned
- GPS and device metadata - Planned
- Mobile companion - Deferred
- Administrative tools - Planned
- Commercial pilot - Planned

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
