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
Next implementation package definition

Required outcome:

- Preserve the released `VERTICAL-SLICE-0.1` path while extending it through:
  Attach Evidence -> Add Interpretation -> Assemble Draft Context -> Create Report Draft -> Reload Report -> Display report audit history.
- Keep the Application layer free of EF Core, SQLite, HTTP, Ollama, file-system, and UI concerns.
- Preserve explicit repository mutation semantics and single-commit audit staging.
- Keep `GenerateReportDraftRequest` side-effect free under `EDR-APP-002`.
- Keep the new authoritative report draft in `Draft` state only.
- Enforce duplicate-safe retry behavior for `CreateReportDraftCommand` through `OperationId`.
- Keep the Desktop host deterministic and intentionally narrow until a real MAUI client is introduced.

Next review:
Next implementation package review

Known blockers:
None

Last completed:
Authoritative report-draft vertical slice released as `REPORT-DRAFT-SLICE-0.1`

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
