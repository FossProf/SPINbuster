# Current Priority

Current milestone:
Prototype Vertical Slice

Latest released baseline:
KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1

Active review candidate:
ENGINEERING-KNOWLEDGE-MODEL-0.1-RC

Next planned implementation package:
DOCUMENT-ENGINE-FOUNDATION-0.1-RC

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
Define the authoritative engineering knowledge model and subsystem boundaries

Required outcome:

- Create the authoritative conceptual engineering knowledge model under `spec/knowledge/`.
- Define the initial Document Engine and Rule Engine boundaries without implementing them.
- Preserve all released Knowledge Engine, AI, and report behavior classifications accurately.
- Keep AI advisory, citations revision-bound, and raw records immutable.
- Leave `DOCUMENT-ENGINE-FOUNDATION-0.1-RC` as the next planned implementation package after review and release.

Next review:
`ENGINEERING-KNOWLEDGE-MODEL-0.1-RC` governance review

Known blockers:
None

Last completed:
Released `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1`

Proposed next direction:

- Complete governance review for the engineering knowledge model package
- Then begin `DOCUMENT-ENGINE-FOUNDATION-0.1-RC`
- Keep `EDR-KE-002`, `EDR-KE-009`, `EDR-KE-010`, and `EDR-KE-011` active before automated document workflows begin

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
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
