# Current Priority

Current milestone:
Prototype Vertical Slice

Latest released baseline:
DOCUMENT-ENGINE-FOUNDATION-0.1

Active review candidate:
DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC

Next planned implementation package:
DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC

Status:
Released baseline recorded; next package active

Build:
Passing

Warnings:
0

Domain tests:
52/52 passing

Architecture tests:
17/17 passing

Application tests:
70/70 passing

Infrastructure tests:
27/27 passing

AI tests:
6/6 passing

Desktop end-to-end tests:
6/6 passing

Current task:
Implement the first executable Document Engine workflow

Required outcome:

- Build the first executable local Document Engine workflow through the temporary Desktop host.
- Apply migrations at startup and exercise the released Document Engine foundation end to end.
- Persist and reload import sessions, imported sources, processing attempts, document candidates, and audit history.
- Present deterministic success and failure outcomes without widening the authoritative boundary.

Next review:
`DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC` executable and workflow review

Known blockers:
None

Last completed:
Released the Document Engine foundation baseline

Proposed next direction:

- Implement `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`
- Keep parsing, OCR, and promotion boundaries deferred until after the executable slice proves the foundation
- Preserve the non-authoritative document-candidate boundary

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
