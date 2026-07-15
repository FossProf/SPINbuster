# Coding Rules

Purpose: Capture non-negotiable repository operating rules for coding agents and contributors.

## Non-Negotiable Rules

- Preserve the approved inward-pointing layered project references.
- Do not add business logic to bootstrap hosts or placeholder projects without updating the governing specifications first.
- Treat `src/SPINbuster.Desktop` as a temporary bootstrap host until a real MAUI host is intentionally introduced.
- Keep `src/SPINbuster.Shared` narrow: cross-boundary contracts, primitives, identifiers, serialization-safe shared DTO primitives, and non-domain constants only.
- Do not place domain entities, business rules, repositories, persistence models, UI models, or workflows in `src/SPINbuster.Shared`.
- Remove template placeholder files such as `Class1.cs` and `UnitTest1.cs` from baseline scaffolding once real project files exist.
- Add short comments where a domain invariant, lifecycle rule, or boundary decision would not be obvious to a later reviewer from the code alone.
- Keep formatting machine-enforced; do not rely on manual style consistency.

## Authoritative Specifications

Specifications under `spec/` are authoritative for system behavior and engineering rules.
Do not replace or redefine them in `.ai/`.

## File Boundaries

- `src/` contains implementation projects and hosts.
- `tests/` contains automated verification, including architecture guardrails.
- `spec/` contains authoritative engineering definitions.
- `.ai/` contains operational guidance and summaries only.
- `docs/` contains reader-facing documentation, including decision records.

## Required Validation

- Run `dotnet restore SPINbuster.sln --configfile NuGet.Config` after project-graph or package changes.
- Run `dotnet format SPINbuster.sln` after meaningful C# changes when formatting or analyzer cleanup is relevant.
- Run `dotnet build SPINbuster.sln --no-restore` before completing .NET skeleton changes.
- Update `.ai/repository-map.md` when repository structure or project responsibilities change.
