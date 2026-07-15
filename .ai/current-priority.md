# Current Priority

Current milestone:
Prototype Vertical Slice

Baseline:
VERTICAL-SLICE-0.1

Status:
Released

Build:
Passing

Warnings:
0

Domain tests:
24/24 passing

Architecture tests:
8/8 passing

Application tests:
13/13 passing

Infrastructure tests:
7/7 passing

Desktop end-to-end tests:
2/2 passing

Current task:
Next implementation package definition

Required outcome:

- Preserve the released first executable local vertical slice above the released local SQLite Infrastructure foundation.
- Preserve the approved inward-pointing architecture and the existing minimal `Application -> Domain` dependency surface.
- Keep EF Core, SQLite, HTTP, Ollama, file-system, and UI details out of the Application layer.
- Preserve the released audit staging, explicit repository update semantics, and infrastructure transaction guarantees from `INFRASTRUCTURE-0.1`.
- Maintain the validated temporary Desktop host path:
  Create Project -> Start Inspection Session -> Capture Field Note -> Commit through SQLite -> Reload persisted state -> Display persisted audit history.
- Keep the Desktop host deterministic and intentionally narrow until a real MAUI client is introduced.
- Respect deferred design items `EDR-DOM-001` and `EDR-APP-001`.
- Preserve accepted drafting-query boundary `EDR-APP-002`.

Next review:
Next implementation package review

Known blockers:
None

Last completed:
First executable local Desktop-to-SQLite vertical slice released as `VERTICAL-SLICE-0.1`

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
- Relevant `src/SPINbuster.Application/`, `src/SPINbuster.Infrastructure/`, and `src/SPINbuster.Desktop/` files for the active vertical slice

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
