# Repository Map

Purpose: Explain how the repository is organized and where different kinds of work belong.

## Top-Level Structure

- `.ai/` contains agent bootstrap guidance, current priorities, and operating rules.
- `PROJECT_STATE.md` provides the fastest project-status snapshot for new sessions.
- `spec/` contains authoritative engineering specifications.
- `docs/` contains human-readable documentation derived from specifications.
- `src/` contains implementation projects.
- `tests/` contains automated verification.
- `database/`, `schemas/`, `prompts/`, `tools/`, and `scripts/` contain supporting assets and workflows.

## Documentation And Decision Layout

- `docs/decisions/adr/` contains architecture decision records.
- `docs/decisions/edr/` contains engineering and product decision records.
- `docs/decisions/status/` contains baseline, review, and status records.
- `docs/03-implementation/IMPLEMENTATION_LOG.md` records completed milestones and the next implementation step.
- `docs/decisions/edr/EDR-DOM-001-versioned-evidence-interpretation-history.md` records the deferred interpretation-history design item for the Domain layer.
- `docs/decisions/edr/EDR-APP-001-command-idempotency.md` records the deferred command-idempotency design item for the Application layer.
- `docs/decisions/edr/EDR-APP-002-draft-generation-ownership.md` records the accepted drafting-query boundary for `APPLICATION-0.1`.

## Source Projects

- `src/SPINbuster.Shared` contains only narrow cross-boundary contracts, primitives, identifiers, and serialization-safe shared DTO primitives.
- `src/SPINbuster.Domain` contains core domain types and domain-level policies, including the initial Project, InspectionSession, FieldNote, EvidenceAttachment, Report, SaveTransaction, and AuditEvent model.
- `src/SPINbuster.Rules` contains reusable business rule evaluation components that support the core.
- `src/SPINbuster.Application` contains application-layer orchestration, command/query contracts, repository interfaces, transaction boundaries, audit abstractions, identity/time abstractions, and the first vertical-slice use cases.
- `src/SPINbuster.Infrastructure` contains persistence and external system adapters for non-AI concerns.
- `src/SPINbuster.AI` contains AI integration adapters and AI-specific orchestration support.
- `src/SPINbuster.Documents` contains document generation and document workflow support.
- `src/SPINbuster.Reporting` contains reporting composition and report output support.
- `src/SPINbuster.Server` contains the server host and composition root.
- `src/SPINbuster.Desktop` is currently a temporary bootstrap host and not yet a MAUI Blazor Hybrid application.

## Test Projects

- `tests/SPINbuster.Architecture.Tests` enforces project reference rules and scaffold guardrails.
- `tests/SPINbuster.Shared.Tests` verifies the shared layer.
- `tests/SPINbuster.Domain.Tests` verifies the domain layer.
- `tests/SPINbuster.Rules.Tests` verifies the rules layer.
- `tests/SPINbuster.Application.Tests` verifies the application layer.
- `tests/SPINbuster.Infrastructure.Tests` verifies infrastructure adapters.
- `tests/SPINbuster.AI.Tests` verifies AI integration behavior.
- `tests/SPINbuster.Documents.Tests` verifies document-related components.
- `tests/SPINbuster.Reporting.Tests` verifies reporting components.
- `tests/SPINbuster.Server.Tests` verifies server-hosted behavior.
- `tests/SPINbuster.Desktop.Tests` verifies desktop-hosted behavior.

The one-to-one test-project pattern is acceptable for the skeleton phase and can be reassessed when real tests begin replacing scaffolding.

## Intended Artifact Structure

- `schemas/ai/`, `schemas/api/`, `schemas/database/`, `schemas/events/`, `schemas/reports/`, and `schemas/sync/` are the intended durable schema buckets.
- `prompts/field-interpreter/`, `prompts/field-validator/`, `prompts/report-drafter/`, `prompts/retrieval-ranker/`, `prompts/rule-assistant/`, `prompts/intelligence-candidate/`, and `prompts/document-analyzer/` are the intended prompt-workflow buckets.

## Working Rule

Start from `.ai/` for navigation.
Read `PROJECT_STATE.md` first when rapid project-state context is needed.
Use `spec/` as the source of truth for design and behavior.
Use `docs/` for explanatory and reader-friendly material.

## Change Placement

Put implementation changes in `src/` and `tests/`.
Put engineering rules and subsystem contracts in `spec/`.
Put lightweight agent instructions in `.ai/`.

## Current Solution Files

- `Directory.Build.props` defines repository-wide .NET build defaults.
- `Directory.Packages.props` centralizes NuGet package versions.
- `.editorconfig` defines formatting and editor conventions.
- `NuGet.Config` keeps restore behavior self-contained within the repository.
- `SPINbuster.sln` aggregates all production and test projects.

## Current Application Foundation

- `src/SPINbuster.Application/Contracts/` defines command and query handler contracts.
- `src/SPINbuster.Application/Abstractions/` defines `IClock`, `ICurrentUser`, `IAuditRecorder`, and `IUnitOfWork`.
- `src/SPINbuster.Application/Repositories/` defines inward-facing repository interfaces for `Project`, `InspectionSession`, `Report`, and `SaveTransaction`, including explicit update semantics for mutated loaded aggregates.
- `src/SPINbuster.Application/UseCases/` currently contains `CreateProject`, `StartInspectionSession`, `CaptureFieldNote`, `AttachEvidence`, `AddInterpretation`, `GenerateReportDraftRequest`, and `PrepareTransactionalSave`.
- `tests/SPINbuster.Application.Tests/` uses in-memory fakes to verify orchestration, lifecycle guards, staged audit ordering, explicit mutation updates, failure handling, ownership boundaries, and draft-request shaping without adding persistence or transport concerns.
