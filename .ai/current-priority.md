# Current Priority

Current milestone:
Prototype Vertical Slice

Latest governance baseline:
ARCHITECTURE-VISION-2.0

Latest software baseline:
LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1

Active implementation package:
PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC

Status:
Architecture vision frozen as the governing platform baseline

Build:
Passing

Warnings:
0

Domain tests:
96/96 passing

Application tests:
121/121 passing

Documents tests:
28/28 passing

Infrastructure tests:
32/32 passing

Architecture tests:
23/23 passing

Desktop end-to-end tests:
23/23 passing

Current task:
Begin `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC` under the frozen architecture baseline

Required outcome:

- Define deterministic, non-authoritative fragment handling for persisted document sources.
- Preserve the frozen governance baseline while beginning the next implementation package.
- Keep OCR, AI extraction, and promotion boundaries governed and deferred until explicitly designed.
- Do not bypass provenance, validation, scope, lifecycle, or project boundaries.

Next review:
Parsing and fragment foundation boundary review

Known blockers:
None

Last completed:
Prompt 5 (file-level navigability refactoring) and Prompt 6 (audit-event construction consolidation). `DocumentEngineUseCases.cs` split into 11 per-use-case directories. `InMemoryFakes.cs` split into 7 files by aggregate group. `Identifiers.cs` split into 7 files by aggregate. `AuditableEntity` base class centralizes mechanical audit-event construction with abstract `SubjectType`/`SubjectId` and `CreateAuditEvent` helper. All 10 Domain aggregates updated with explicit `const string AuditSubjectType` and overrides. `AiAuditEventFactory` refactored with private `Create` helper. 329 tests all passing.

Proposed next direction:

- Begin `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`
- Preserve the non-authoritative document-candidate boundary
- Keep OCR, AI extraction, and reconciliation workflows deferred until fragment contracts are explicit
- Use capability-phase planning rather than slice-only planning for future governance updates

Current capabilities:

- Create project
- Start inspection session
- Capture immutable field notes
- Attach raw evidence
- Add one non-replaceable interpretation
- Assemble report-draft context
- Create authoritative revision-1 report drafts
- Persist provenance and audit history
- Retry draft creation safely through `OperationId`
- Build governed report-proposal context manifests
- Run deterministic Tier 0 AI proposal generation without live AI services
- Persist model runs, run attempts, and advisory AI proposals
- Validate structured AI proposal output before review
- Load and reject advisory AI proposals without mutating authoritative reports
- Record human acceptance as review intent only
- Replay deterministic AI proposal requests safely through the Desktop host
- Reload durable model-run, proposal, attempt, and audit history through an application snapshot query
- Persist explicit AI lifecycle audit markers for request, provider attempt, validation, completion, and review disposition
- Register authoritative knowledge documents and immutable revisions
- Record explicit knowledge supersession, verification, relationship, contradiction, and citation audit facts
- Load bounded knowledge neighborhoods through application snapshots
- Execute deterministic Knowledge Engine workflows through the Desktop host
- Reload project-scoped knowledge documents, revisions, relationships, citations, and audit history through a presentation-safe Application query
- Present expected Knowledge Engine failure cases without crashing the scripted demo path
- Register immutable imported document sources
- Detect exact duplicate content deterministically
- Persist import sessions, processing attempts, and non-authoritative document candidates
- Execute the first deterministic Document Engine workflow through the Desktop host
- Persist multi-source batch import state, processing outcomes, candidate review state, and document audit history
- Reload project-scoped document workflow snapshots without mutating Knowledge, Report, or AI records
- Persist immutable document bytes beneath a local filesystem root with ID-addressed layout and atomic-write semantics
- Reopen stored bytes safely after provider recreation and repeated executable runs against the same SQLite database plus storage root
- Detect missing and corrupted stored bytes during processing and fail terminally without widening authority
- Expose bounded adapter-specific orphan visibility for future reconciliation work

Authoritative context:

- `PROJECT_STATE.md`
- `.ai/coding-rules.md`
- `.ai/architecture-summary.md`
- `.ai/repository-map.md`
- `.ai/glossary-summary.md`
- `spec/knowledge/README.md`
- `spec/knowledge/engineering-knowledge-model.md`
- `spec/documents/README.md`
- `spec/documents/document-engine-boundary.md`
- `spec/documents/document-engine-foundation.md`
- `docs/decisions/status/LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1-RC-PROTOTYPE-REVIEW.md`
- `spec/rules/README.md`
- `spec/rules/rule-engine-boundary.md`
- `docs/00-governance/ROADMAP.md`
- `docs/decisions/edr/EDR-KE-002-document-parsing-and-chunking.md`
- `docs/decisions/edr/EDR-KE-009-knowledge-command-idempotency.md`
- `docs/decisions/edr/EDR-KE-010-knowledge-fragment-identity.md`
- `docs/decisions/edr/EDR-KE-011-engineering-assertion-promotion.md`
- `docs/decisions/edr/EDR-KE-012-document-engine-ownership-boundary.md`
- `docs/00-governance/ROADMAP.md`

Validation before completion:

- `dotnet format SPINbuster.sln --no-restore`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
