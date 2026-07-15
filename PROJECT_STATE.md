# PROJECT_STATE

## Current Baseline

- `DOMAIN-0.1`
- Status: `Released`
- Build: `Passing`
- Warnings: `0`
- Architecture tests: `7/7 passing`
- Domain tests: `24/24 passing`

## Current Branch

- `main`

## Last Completed Milestone

- Initial Domain foundation released

## Current Implementation Phase

- Application-layer vertical-slice preparation

## Current Milestone

- Prototype Vertical Slice

## Open ADRs

- None recorded yet

## Open EDRs

- `EDR-DOM-001` Versioned evidence interpretation history (`Deferred`)

## Outstanding Technical Debt

- `SPINbuster.Desktop` is still a temporary bootstrap host and has not been replaced with a real MAUI client.
- Most test projects are still empty scaffolds outside of `SPINbuster.Architecture.Tests`.
- `spec/architecture/` still needs fuller authoritative content as implementation begins.
- The Domain model exists, but the Application layer contracts and orchestration boundaries are not defined yet.
- Known deferred design item: versioned evidence interpretation history.

## Immediate Next Task

- Application-layer vertical-slice contracts and use cases

## Fast Context

- The repository is the source of truth for project state and architecture.
- Start every new AI session from `.ai/bootstrap.md`.
- The current scaffold builds successfully, Domain tests pass, and the architecture guardrails pass.
- The current Domain baseline is released; the next task is Application-layer contracts and use cases.
