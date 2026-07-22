# Current Priority

Current milestone:
Prototype Vertical Slice

Latest governance baseline:
ARCHITECTURE-VISION-2.0

Latest software baseline:
FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1

Active implementation package:
DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC

Status:
FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1 released

Build:
Passing

Warnings:
0

Domain tests:
181/181 passing

Application tests:
184/184 passing

Documents tests:
28/28 passing

Infrastructure tests:
56/56 passing

Architecture tests:
24/24 passing

AI tests:
6/6 passing

Desktop tests:
39/39 passing

Total tests:
518/518 passing

Current task:
`FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1` released. Begin `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC`

Required outcome:

- Implement structural text extraction: headings, numbered clauses, tables, source-location fidelity
- Establish overlapping-fragment policy
- Add parser diagnostics and partial-success semantics
- Keep review lifecycle boundaries from FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1 intact
- Preserve authority isolation

Next review:
After `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` completion

Known blockers:
None

Known process deviation:
The EF migration `AddFragmentCandidateReviewState` was created during Prompt 1 before Application review workflows were finalized. This was premature per slice boundaries. Treat the checkpoint as `FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT`. Do not create another migration unless the model genuinely changes.

Last completed:

- Released `PARSING-AND-FRAGMENT-FOUNDATION-0.1` with executable proof validated
- Completed `FRAGMENT-INTEGRITY-HARDENING-CHECKPOINT`: fixed Rehydrate, added rehydration validation, documented contract-version identity choice (EDR-DE-006) across 4 prompts
- Completed `FRAGMENT-REVIEW-DOMAIN-AND-SCHEMA-CHECKPOINT` (Prompt 1): implemented FragmentCandidateReviewState enum, Accept/Reject methods with audit events, review disposition properties, updated InfrastructureMapper/EF model, added EF migration, updated spec, created EDR-DE-007, added 20 review lifecycle domain tests
- Completed `FRAGMENT-REVIEW-APPLICATION-CHECKPOINT` (Prompt 2): implemented Accept/Reject use cases, LoadFragmentReviewSnapshot query, repository extensions, DI registration, added 28 Application tests
- Completed `FRAGMENT-REVIEW-PERSISTENCE-CHECKPOINT` (Prompt 3): verified EF migration integrity, repository update semantics, atomic commit, and concurrency
- Completed `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC` (Prompt 4): extended Desktop executable proof with full review lifecycle, 2-source import, version coexistence, expected failure scenarios, authority isolation verification, fixed EF Core tracking conflict, created prototype review document

Proposed next direction:

- Begin `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` for structural text extraction
- After that, proceed to `FRAGMENT-TO-KNOWLEDGE-PROMOTION-FOROUNDATION-0.1-RC`
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
- Review fragment candidates through Accept/Reject lifecycle with audit trail
- Persist review disposition (ReviewedBy, ReviewedAtUtc, ReviewNotes) with fragment candidates
- Verify review does not mutate identity, provenance, or locator properties
- Execute full Desktop review workflow with 2-source import, version coexistence, and expected failure scenarios
- Verify first-commit-wins concurrency and terminal state guards
- Verify authority isolation: parsing and review do not create Knowledge, Report, or AI records
- Record concurrency technical debt for server/multi-user safety

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
- `docs/decisions/edr/EDR-DE-006-fragment-identity-contract-version-choice.md`
- `docs/decisions/edr/EDR-DE-007-fragment-candidate-review-disposition-and-promotion-prerequisites.md`

Validation before completion:

- `dotnet format SPINbuster.sln --no-restore`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
