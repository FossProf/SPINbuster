# Current Priority

Current milestone:
Prototype Vertical Slice

Latest released baseline:
KNOWLEDGE-ENGINE-PERSISTENCE-0.1

Next active package:
KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1-RC

Status:
Released

Build:
Passing

Warnings:
0

Domain tests:
48/48 passing

Architecture tests:
16/16 passing

Application tests:
57/57 passing

Infrastructure tests:
23/23 passing

AI tests:
6/6 passing

Desktop end-to-end tests:
3/3 passing

Current task:
Implement `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1-RC`

Required outcome:

- Preserve the released report-draft and executable AI baselines while introducing the first authoritative Knowledge Engine foundation.
- Keep knowledge records authoritative and project-scoped.
- Preserve immutable historical revisions and explicit supersession semantics.
- Keep repository contracts provider-neutral and free of EF Core, file-system, transport, or UI leakage.
- Keep the Knowledge Engine usable without AI.
- Defer parsing, OCR, embeddings, vector search, and automatic authority promotion explicitly through EDRs.

Next review:
`KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1-RC` end-to-end workflow, reload, and audit review

Known blockers:
None

Last completed:
Released `KNOWLEDGE-ENGINE-PERSISTENCE-0.1`

Proposed next direction:

- Wire the persisted Knowledge Engine workflow through the temporary Desktop host
- Validate revision chains, graph edges, citations, and audit history end to end
- Prepare the next prompt-driven Knowledge Engine package after executable validation

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
- Record explicit knowledge supersession, verification, relationship, and contradiction audit facts
- Load bounded knowledge neighborhoods through application snapshots

Authoritative context:

- `PROJECT_STATE.md`
- `.ai/coding-rules.md`
- `.ai/architecture-summary.md`
- `.ai/repository-map.md`
- `spec/architecture/`
- `spec/ai/README.md`
- `src/SPINbuster.Application/`
- `tests/SPINbuster.Application.Tests/`
- `src/SPINbuster.Domain/`
- `tests/SPINbuster.Domain.Tests/`
- `src/SPINbuster.Infrastructure/`
- `tests/SPINbuster.Infrastructure.Tests/`
- `src/SPINbuster.Desktop/`
- `tests/SPINbuster.Desktop.Tests/`
- `docs/decisions/edr/EDR-DOM-001-versioned-evidence-interpretation-history.md`
- `docs/decisions/edr/EDR-APP-001-command-idempotency.md`
- `docs/decisions/edr/EDR-APP-002-draft-generation-ownership.md`
- `docs/decisions/edr/EDR-AI-001-authoritative-report-revision-acceptance.md`
- `docs/decisions/edr/EDR-AI-002-ai-proposal-request-idempotency-and-recovery.md`
- `docs/decisions/status/REPORT-DRAFT-SLICE-0.1-PROTOTYPE-REVIEW.md`
- `schemas/ai/report-draft-proposal.schema.json`
- `spec/ai/context-manifest.md`
- `spec/ai/json-schemas.md`
- `spec/ai/provider-adapters.md`
- `spec/ai/prompt-contracts.md`
- `spec/ai/confidence-and-uncertainty.md`
- `spec/ai/model-run-lifecycle.md`
- `spec/knowledge/README.md`
- `spec/architecture/knowledge-engine-foundation.md`
- `docs/decisions/edr/EDR-KE-001-binary-file-storage-ownership.md`
- `docs/decisions/edr/EDR-KE-002-document-parsing-and-chunking.md`
- `docs/decisions/edr/EDR-KE-003-ocr-boundary.md`
- `docs/decisions/edr/EDR-KE-004-embeddings-and-vector-search.md`
- `docs/decisions/edr/EDR-KE-005-automatic-authority-classification.md`
- `docs/decisions/edr/EDR-KE-006-ai-generated-relationship-promotion.md`
- `docs/decisions/edr/EDR-KE-007-cross-project-knowledge-sharing.md`
- `docs/decisions/edr/EDR-KE-008-multi-current-revision-conflict-resolution.md`
- Relevant knowledge foundation files under `src/SPINbuster.Domain/`, `src/SPINbuster.Application/`, and `tests/`
- `spec/database/README.md`
- `spec/database/knowledge-engine-persistence.md`
- Relevant report-draft slice files under `src/SPINbuster.Domain/`, `src/SPINbuster.Application/`, `src/SPINbuster.Infrastructure/`, and `src/SPINbuster.Desktop/`

Validation before completion:

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet tool run dotnet-ef migrations has-pending-model-changes --no-build --project src/SPINbuster.Infrastructure --startup-project src/SPINbuster.Server --context SPINbuster.Infrastructure.Persistence.SpinbusterDbContext`
- `dotnet test tests/SPINbuster.Domain.Tests/SPINbuster.Domain.Tests.csproj --no-build`
- `dotnet test tests/SPINbuster.Application.Tests/SPINbuster.Application.Tests.csproj --no-build`
- `dotnet test tests/SPINbuster.Infrastructure.Tests/SPINbuster.Infrastructure.Tests.csproj --no-build`
- `dotnet test tests/SPINbuster.Architecture.Tests/SPINbuster.Architecture.Tests.csproj --no-build`
- `dotnet test SPINbuster.sln --no-build -m:1`
