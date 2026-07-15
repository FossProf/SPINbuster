# Current Priority

Current milestone:
Prototype Vertical Slice

Baseline:
SKELETON-0.1

Status:
Released

Build:
Passing

Warnings:
0

Architecture tests:
7/7 passing

Current task:
Implement Domain foundation

Required outcome:

- Establish the first real Domain types and boundaries.
- Preserve the approved inward-pointing architecture.
- Keep `SPINbuster.Shared` narrow and dependency-free.
- Keep Tier 0 operation possible without AI services.
- Maintain passing architecture guardrails.

Next review:
Application layer architecture

Known blockers:
None

Last completed:
Repository scaffold approved (`SKELETON-0.1`)

Authoritative context:

- `PROJECT_STATE.md`
- `.ai/coding-rules.md`
- `.ai/architecture-summary.md`
- `.ai/repository-map.md`
- `spec/architecture/`
- `spec/ai/README.md`

Validation before completion:

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet build SPINbuster.sln --no-restore`
- `dotnet test tests/SPINbuster.Architecture.Tests/SPINbuster.Architecture.Tests.csproj`
