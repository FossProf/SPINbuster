# Current Priority

Current milestone:
Prototype Vertical Slice

Baseline:
REPORT-DRAFT-SLICE-0.1

Status:
Released

Build:
Passing

Warnings:
0

Domain tests:
25/25 passing

Architecture tests:
8/8 passing

Application tests:
17/17 passing

Infrastructure tests:
10/10 passing

Desktop end-to-end tests:
2/2 passing

Current task:
Define `AI-DRAFT-PROPOSAL-SLICE-0.1`

Required outcome:

- Preserve the released authoritative report-draft baseline while adding the first local AI-assisted proposal path.
- Keep AI output non-authoritative until explicit human acceptance creates a new authoritative report revision.
- Preserve the current `Draft` ownership boundary, provenance rules, and `OperationId` retry guarantees.
- Introduce governed AI context assembly and structured proposal validation without adding HTTP, MAUI, or cloud-provider dependencies.
- Keep the Desktop host deterministic and intentionally narrow until a real MAUI client is introduced.

Next review:
`AI-DRAFT-PROPOSAL-SLICE-0.1` package review

Known blockers:
None

Last completed:
Prototype review recorded for `REPORT-DRAFT-SLICE-0.1`

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
- `docs/decisions/status/REPORT-DRAFT-SLICE-0.1-PROTOTYPE-REVIEW.md`
- Relevant report-draft slice files under `src/SPINbuster.Domain/`, `src/SPINbuster.Application/`, `src/SPINbuster.Infrastructure/`, and `src/SPINbuster.Desktop/`

Validation before completion:

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test tests/SPINbuster.Application.Tests/SPINbuster.Application.Tests.csproj`
- `dotnet test tests/SPINbuster.Infrastructure.Tests/SPINbuster.Infrastructure.Tests.csproj`
- `dotnet test tests/SPINbuster.Desktop.Tests/SPINbuster.Desktop.Tests.csproj`
- `dotnet test tests/SPINbuster.Domain.Tests/SPINbuster.Domain.Tests.csproj`
- `dotnet test tests/SPINbuster.Architecture.Tests/SPINbuster.Architecture.Tests.csproj`
- `dotnet test SPINbuster.sln --no-build -m:1`
- `dotnet src/SPINbuster.Desktop/bin/Release/net9.0/SPINbuster.Desktop.dll --ConnectionStrings:Spinbuster="Data Source=tmp/report-draft-slice-validation.sqlite"`
