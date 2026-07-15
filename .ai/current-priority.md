# Current Priority

Current milestone:
Prototype Vertical Slice

Baseline:
DOMAIN-0.1

Status:
Released

Build:
Passing

Warnings:
0

Domain tests:
24/24 passing

Architecture tests:
7/7 passing

Current task:
Application-layer vertical-slice contracts and use cases

Required outcome:

- Define application-layer use-case contracts around the released Domain baseline.
- Preserve the approved inward-pointing architecture.
- Keep persistence, EF Core, transport, and AI implementation concerns out of Application contracts.
- Respect deferred design item `EDR-DOM-001`.

Next review:
Application-layer vertical-slice review

Known blockers:
None

Last completed:
Domain foundation released (`DOMAIN-0.1`)

Authoritative context:

- `PROJECT_STATE.md`
- `.ai/coding-rules.md`
- `.ai/architecture-summary.md`
- `.ai/repository-map.md`
- `spec/architecture/`
- `spec/ai/README.md`
- `src/SPINbuster.Domain/`
- `tests/SPINbuster.Domain.Tests/`
- `docs/decisions/edr/EDR-DOM-001-versioned-evidence-interpretation-history.md`

Validation before completion:

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test tests/SPINbuster.Domain.Tests/SPINbuster.Domain.Tests.csproj`
- `dotnet test tests/SPINbuster.Architecture.Tests/SPINbuster.Architecture.Tests.csproj`
