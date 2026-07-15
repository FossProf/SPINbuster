# PROJECT_STATE

## Current Baseline

- `INFRASTRUCTURE-0.1`
- Status: `Released`
- Build: `Passing`
- Warnings: `0`
- Architecture tests: `8/8 passing`
- Domain tests: `24/24 passing`
- Application tests: `13/13 passing`
- Infrastructure tests: `7/7 passing`

## Current Branch

- `main`

## Last Completed Milestone

- Local SQLite persistence foundation released

## Current Implementation Phase

- Application vertical-slice planning

## Current Milestone

- Prototype Vertical Slice

## Open ADRs

- None recorded yet

## Open EDRs

- `EDR-DOM-001` Versioned evidence interpretation history (`Deferred`)
- `EDR-APP-001` Application command idempotency (`Deferred`)
- `EDR-APP-002` Draft-generation ownership (`Accepted for APPLICATION-0.1`)

## Outstanding Technical Debt

- `SPINbuster.Desktop` is still a temporary bootstrap host and has not been replaced with a real MAUI client.
- Most test projects are still empty scaffolds outside of `SPINbuster.Architecture.Tests`.
- `spec/architecture/` still needs fuller authoritative content as implementation begins.
- Known deferred design item: versioned evidence interpretation history.
- Known deferred application design item: command idempotency before retry and synchronization work.

## Immediate Next Task

- Application-layer vertical-slice contracts and use cases

## Fast Context

- The repository is the source of truth for project state and architecture.
- Start every new AI session from `.ai/bootstrap.md`.
- The current scaffold builds successfully, Infrastructure tests pass, Application tests pass, Domain tests pass, and the architecture guardrails pass.
- The current Infrastructure baseline is released with no pending model changes, successful empty-database migration, idempotent repeat migration, verified migration history, atomic aggregate-plus-audit commit and rollback, and detached updates validated.
