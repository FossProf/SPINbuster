# SPINbuster

SPINbuster is organized as a layered .NET solution scaffold with implementation projects under `src/`, matching test projects under `tests/`, engineering specifications under `spec/`, and lightweight agent operating guidance under `.ai/`.

## Solution Layout

- `SPINbuster.sln` is the root solution file.
- `src/` contains the production projects:
  - `SPINbuster.Shared`
  - `SPINbuster.Domain`
  - `SPINbuster.Rules`
  - `SPINbuster.Application`
  - `SPINbuster.Infrastructure`
  - `SPINbuster.AI`
  - `SPINbuster.Documents`
  - `SPINbuster.Reporting`
  - `SPINbuster.Server`
  - `SPINbuster.Desktop`
- `tests/` contains one matching test project per production project plus `SPINbuster.Architecture.Tests` for dependency-graph guardrails.

## Architecture Intent

- References point inward toward more stable layers.
- `SPINbuster.Shared` is intentionally narrow and should not become a general dumping ground.
- `SPINbuster.Domain` and `SPINbuster.Rules` stay free of infrastructure concerns.
- `SPINbuster.Application` coordinates use cases without requiring AI services.
- `SPINbuster.Infrastructure`, `SPINbuster.AI`, `SPINbuster.Documents`, and `SPINbuster.Reporting` sit outside the core.
- `SPINbuster.Server` and `SPINbuster.Desktop` are application entry points and composition roots.
- `SPINbuster.Desktop` is currently a temporary bootstrap host, not yet a MAUI Blazor Hybrid application.

## Validation

Use the following commands after structural changes:

```powershell
dotnet restore
dotnet build SPINbuster.sln
```
