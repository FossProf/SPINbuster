# Current Priority

Current milestone:
Prototype Vertical Slice

Baseline:
APPLICATION-0.1

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

Current task:
Application-to-Infrastructure persistence seam design

Required outcome:

- Define Infrastructure persistence seams against the released Application baseline.
- Preserve the approved inward-pointing architecture and the minimal `Application -> Domain` dependency surface.
- Keep EF Core, SQLite, and transaction implementation details out of Application contracts.
- Preserve the released audit staging and explicit repository update semantics from `APPLICATION-0.1`.
- Respect deferred design items `EDR-DOM-001` and `EDR-APP-001`.
- Preserve accepted drafting-query boundary `EDR-APP-002`.

Next review:
Infrastructure persistence-boundary review

Known blockers:
None

Last completed:
Initial Application foundation released as `APPLICATION-0.1`

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
- `docs/decisions/edr/EDR-DOM-001-versioned-evidence-interpretation-history.md`
- `docs/decisions/edr/EDR-APP-001-command-idempotency.md`
- `docs/decisions/edr/EDR-APP-002-draft-generation-ownership.md`

Validation before completion:

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test tests/SPINbuster.Application.Tests/SPINbuster.Application.Tests.csproj`
- `dotnet test tests/SPINbuster.Domain.Tests/SPINbuster.Domain.Tests.csproj`
- `dotnet test tests/SPINbuster.Architecture.Tests/SPINbuster.Architecture.Tests.csproj`
- `dotnet test SPINbuster.sln --no-build -m:1`
