# Current Priority

Current milestone:
Prototype Vertical Slice

Baseline:
AI-PROPOSAL-EXECUTABLE-SLICE-0.1

Status:
Released

Build:
Passing

Warnings:
0

Domain tests:
36/36 passing

Architecture tests:
12/12 passing

Application tests:
45/45 passing

Infrastructure tests:
14/14 passing

AI tests:
6/6 passing

Desktop end-to-end tests:
3/3 passing

Current task:
Record the released deterministic executable AI proposal baseline and await the next implementation package

Required outcome:

- Preserve the released authoritative report-draft baseline while adding the first local AI-assisted proposal path.
- Keep AI output non-authoritative until explicit human acceptance creates a new authoritative report revision.
- Preserve the current `Draft` ownership boundary, provenance rules, and `OperationId` retry guarantees.
- Introduce governed AI context assembly and structured proposal validation without adding HTTP, MAUI, or cloud-provider dependencies.
- Keep the Desktop host deterministic and intentionally narrow until a real MAUI client is introduced.
- Keep AI provider integration limited to the deterministic fixture until the next slice intentionally broadens it.
- Validate deterministic executable AI proposal request, replay, review action, failure display, and no-report-mutation behavior through the Desktop host.
- Preserve the released deterministic fixture boundary until the next package intentionally broadens provider integration.

Next review:
Define and review the next package after `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`

Known blockers:
None

Last completed:
Released `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`

Proposed next direction:

- Persisted field material
- Assemble governed context manifest
- Invoke local AI provider
- Validate structured proposal
- Present proposal for human review
- Explicitly accept into a new authoritative report revision

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
- Relevant report-draft slice files under `src/SPINbuster.Domain/`, `src/SPINbuster.Application/`, `src/SPINbuster.Infrastructure/`, and `src/SPINbuster.Desktop/`

Validation before completion:

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet tool run dotnet-ef migrations has-pending-model-changes --no-build --project src/SPINbuster.Infrastructure --startup-project src/SPINbuster.Server --context SPINbuster.Infrastructure.Persistence.SpinbusterDbContext`
- `dotnet test tests/SPINbuster.AI.Tests/SPINbuster.AI.Tests.csproj --no-build`
- `dotnet test tests/SPINbuster.Application.Tests/SPINbuster.Application.Tests.csproj`
- `dotnet test tests/SPINbuster.Infrastructure.Tests/SPINbuster.Infrastructure.Tests.csproj`
- `dotnet test tests/SPINbuster.Desktop.Tests/SPINbuster.Desktop.Tests.csproj`
- `dotnet test tests/SPINbuster.Domain.Tests/SPINbuster.Domain.Tests.csproj`
- `dotnet test tests/SPINbuster.Architecture.Tests/SPINbuster.Architecture.Tests.csproj`
- `dotnet test SPINbuster.sln --no-build -m:1`
