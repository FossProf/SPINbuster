# Current Priority

Current milestone:
Prototype Vertical Slice

Latest released baseline:
KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1

Active review candidate:
DOCUMENT-ENGINE-FOUNDATION-0.1-RC

Next planned implementation package:
DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC

Status:
Active review candidate

Build:
Passing

Warnings:
0

Domain tests:
48/48 passing

Architecture tests:
16/16 passing

Application tests:
60/60 passing

Infrastructure tests:
23/23 passing

AI tests:
6/6 passing

Desktop end-to-end tests:
6/6 passing

Current task:
Implement the first durable Document Engine foundation

Required outcome:

- Add immutable imported-source identity and storage-object identity.
- Add import-session, processing-attempt, and document-candidate lifecycles.
- Preserve non-authoritative candidate semantics.
- Add deterministic hashing, immutable storage, and fixture processing adapters.
- Add SQLite persistence, migration, and verification for the Document Engine foundation.

Next review:
`DOCUMENT-ENGINE-FOUNDATION-0.1-RC` architecture and governance review

Known blockers:
None

Last completed:
Defined the engineering knowledge model and specification index

Proposed next direction:

- Complete review of `DOCUMENT-ENGINE-FOUNDATION-0.1-RC`
- Then begin `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`
- Keep parsing, OCR, and promotion boundaries deferred until after the executable slice proves the foundation

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
- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test tests/SPINbuster.Architecture.Tests/SPINbuster.Architecture.Tests.csproj --no-build`
- `dotnet test tests/SPINbuster.Documents.Tests/SPINbuster.Documents.Tests.csproj --no-build`
- `dotnet test tests/SPINbuster.Infrastructure.Tests/SPINbuster.Infrastructure.Tests.csproj --no-build`
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
