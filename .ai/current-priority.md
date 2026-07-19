# Current Priority

Current milestone:
Prototype Vertical Slice

Latest governance baseline:
ARCHITECTURE-VISION-2.0

Latest software baseline:
PARSING-AND-FRAGMENT-FOUNDATION-0.1

Active implementation package:
PARSING-EXECUTABLE-SLICE-0.1-RC

Status:
Parsing and fragment foundation released as PARSING-AND-FRAGMENT-FOUNDATION-0.1

Build:
Passing

Warnings:
0

Domain tests:
152/152 passing

Application tests:
156/156 passing

Documents tests:
28/28 passing

Infrastructure tests:
42/42 passing

Architecture tests:
24/24 passing

Desktop tests:
34/34 passing

Total tests:
442/442 passing

Current task:
Begin `PARSING-EXECUTABLE-SLICE-0.1-RC`

Required outcome:

- Extend the Desktop proof to exercise multi-source parsing, version coexistence, and review workflow.
- Preserve the frozen governance baseline while extending the parsing proof.
- Keep OCR, AI extraction, and promotion boundaries governed and deferred until explicitly designed.
- Do not bypass provenance, validation, scope, lifecycle, or project boundaries.

Next review:
Parsing executable slice boundary review

Known blockers:
None

Last completed:

- Released `PARSING-AND-FRAGMENT-FOUNDATION-0.1` with executable proof validated across 4 prompts

Proposed next direction:

- Begin `PARSING-EXECUTABLE-SLICE-0.1-RC` to extend Desktop proof with multi-source parsing and review workflow
- Preserve the non-authoritative document-candidate boundary
- Keep OCR, AI extraction, and reconciliation workflows deferred until fragment contracts are explicit

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
- Import controlled text sources and produce deterministic fragment candidates through PlainTextDocumentParser
- Persist parser runs and fragment candidates through SQLite with 5-column unique replay index
- Reload parser runs, fragment candidates, and audit history through LoadParsingSnapshotQuery
- Demonstrate unsupported media, cancelled parse, and malformed output as expected failures without crashing
- Verify parser version coexistence with historical candidate preservation
- Verify authority isolation: parsing does not create Knowledge, Report, or AI records

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
- `spec/documents/parsing-and-fragment-foundation.md`
- `docs/decisions/status/PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC-PROTOTYPE-REVIEW.md`
- `spec/rules/README.md`
- `spec/rules/rule-engine-boundary.md`
- `docs/00-governance/ROADMAP.md`
- `docs/decisions/edr/EDR-KE-002-document-parsing-and-chunking.md`
- `docs/decisions/edr/EDR-KE-009-knowledge-command-idempotency.md`
- `docs/decisions/edr/EDR-KE-010-knowledge-fragment-identity.md`
- `docs/decisions/edr/EDR-KE-011-engineering-assertion-promotion.md`
- `docs/decisions/edr/EDR-KE-012-document-engine-ownership-boundary.md`

Validation before completion:

- `dotnet format SPINbuster.sln --no-restore`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
