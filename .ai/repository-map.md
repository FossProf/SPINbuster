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

- `docs/00-governance/` contains the durable repository governance layer established after the `AI-PROPOSAL-EXECUTABLE-SLICE-0.1` release.
- `docs/00-governance/AI_BOOTSTRAP.md` provides durable orientation for new human developers and AI assistants.
- `docs/00-governance/REPOSITORY_CONSTITUTION.md` records permanent repository authority and architectural rules.
- `docs/00-governance/ENGINEERING_PRINCIPLES.md` records the engineering philosophy.
- `docs/00-governance/RELEASE_MODEL.md` records the established release lifecycle.
- `docs/00-governance/ROADMAP.md` records the strategic roadmap and milestone direction.
- `docs/decisions/adr/` contains architecture decision records.
- `docs/decisions/edr/` contains engineering and product decision records.
- `docs/decisions/status/` contains baseline, review, and status records.
- `docs/decisions/status/VERTICAL-SLICE-0.1-PROTOTYPE-REVIEW.md` records the post-release prototype review for the first executable local vertical slice.
- `docs/decisions/status/REPORT-DRAFT-SLICE-0.1-PROTOTYPE-REVIEW.md` records the post-release prototype review for the authoritative report-draft slice.
- `docs/03-implementation/IMPLEMENTATION_LOG.md` records completed milestones and the next implementation step.
- `docs/decisions/edr/EDR-DOM-001-versioned-evidence-interpretation-history.md` records the deferred interpretation-history design item for the Domain layer.
- `docs/decisions/edr/EDR-APP-001-command-idempotency.md` records the accepted command-idempotency rule for authoritative report-draft creation.
- `docs/decisions/edr/EDR-APP-002-draft-generation-ownership.md` records the accepted drafting-query boundary for `APPLICATION-0.1`.
- `docs/decisions/edr/EDR-AI-002-ai-proposal-request-idempotency-and-recovery.md` records deferred duplicate-resolution and recovery work for live AI proposal execution.
- `docs/decisions/edr/EDR-KE-001-binary-file-storage-ownership.md` through `EDR-KE-008-multi-current-revision-conflict-resolution.md` record the deferred boundaries for future Knowledge Engine expansion.

## Source Projects

- `src/SPINbuster.Shared` contains only narrow cross-boundary contracts, primitives, identifiers, and serialization-safe shared DTO primitives.
- `src/SPINbuster.Domain` contains core domain types and domain-level policies, including the current Project, InspectionSession, FieldNote, EvidenceAttachment, Report, SaveTransaction, AuditEvent, and the first Knowledge Engine model for authoritative documents, revisions, relationships, and citations.
- `src/SPINbuster.Rules` contains reusable business rule evaluation components that support the core.
- `src/SPINbuster.Application` contains application-layer orchestration, command/query contracts, repository interfaces, transaction boundaries, audit abstractions, typed application identity and operation contracts, the current vertical-slice use cases, and the provider-neutral Knowledge Engine orchestration contracts.
- `src/SPINbuster.Infrastructure` contains persistence and external system adapters for non-AI concerns, including the durable AI substrate records and the current Knowledge Engine SQLite persistence adapters.
- `src/SPINbuster.AI` contains AI integration adapters and AI-specific orchestration support, currently limited to a deterministic Tier 0 provider and prompt-package registry.
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
Read `docs/00-governance/AI_BOOTSTRAP.md` for durable orientation after the operational bootstrap files.
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
- `src/SPINbuster.Application/ApplicationIdentity.cs` defines `ApplicationUserId` and `OperationId`.
- `src/SPINbuster.Application/Repositories/` defines inward-facing repository interfaces for `Project`, `InspectionSession`, `Report`, and `SaveTransaction`, including explicit update semantics for mutated loaded aggregates and operation-aware report persistence.
- `src/SPINbuster.Application/Repositories/` also now defines inward-facing persistence contracts for `ContextManifest`, `ModelRun`, and `AiProposal`.
- `src/SPINbuster.Application/Repositories/` now also defines repository contracts for `KnowledgeDocument`, `KnowledgeDocumentRevision`, `KnowledgeRelationship`, and `KnowledgeCitation`.
- `src/SPINbuster.Application/Abstractions/` now also defines provider-neutral AI contracts, prompt-package resolution, and structured proposal validation contracts.
- `src/SPINbuster.Application/UseCases/` currently contains `CreateProject`, `StartInspectionSession`, `CaptureFieldNote`, `AttachEvidence`, `AddInterpretation`, `GenerateReportDraftRequest`, `CreateReportDraft`, `PrepareTransactionalSave`, `BuildReportProposalContext`, `RequestReportDraftProposal`, `LoadAiProposal`, and `RejectAiProposal`.
- `src/SPINbuster.Application/UseCases/` now also contains `AcceptAiProposal` and `LoadAiProposalWorkflowSnapshot` for executable review and durable AI history reload.
- `src/SPINbuster.Application/UseCases/` now also contains `RegisterKnowledgeDocument`, `AddKnowledgeDocumentRevision`, `SupersedeKnowledgeRevision`, `VerifyKnowledgeRevision`, `CreateKnowledgeRelationship`, `LoadKnowledgeDocument`, `LoadKnowledgeRevisionHistory`, and `LoadKnowledgeNeighborhood`.
- `src/SPINbuster.Application/UseCases/LoadInspectionWorkflowSnapshot/` reloads persisted project and inspection-session state for the first executable local vertical slice, including field notes and audit history.
- `src/SPINbuster.Application/UseCases/LoadReportDraftSnapshot/` reloads persisted report drafts, structured sections, provenance, and report audit history through the Application boundary.
- `src/SPINbuster.Application/Internal/` now contains the governed report-proposal context assembly path, AI audit-event shaping helpers, and the JSON-backed structured proposal validator.
- `tests/SPINbuster.Application.Tests/` uses in-memory fakes to verify orchestration, lifecycle guards, staged audit ordering, explicit mutation updates, failure handling, ownership boundaries, draft-request shaping, two-phase AI request persistence, prompt-package contract enforcement, and canonical proposal payload storage without adding persistence or transport concerns.
- `tests/SPINbuster.Application.Tests/` now also verifies Knowledge Engine registration, revision supersession, verification, contradiction recording, bounded neighborhood loading, query isolation, and cancellation-token flow.
- `src/SPINbuster.Infrastructure/Persistence/` contains the local SQLite DbContext, EF Core entity mappings, migration artifacts, typed-ID value converters, and Domain-to-record mapping helpers for reports, report sections, source references, report-draft operation mappings, context manifests, model runs, model-run attempts, advisory AI proposals, and Knowledge Engine persistence records.
- `src/SPINbuster.Infrastructure/Repositories/` contains the local SQLite repository implementations, including explicit detached-update support for mutable loaded aggregates, authoritative report-draft persistence, durable AI substrate persistence, Knowledge document/revision/relationship/citation persistence, and audit-history query support for AI and Knowledge workflow reloads.
- `src/SPINbuster.Infrastructure/Services/` contains `SqliteAuditRecorder` and `SqliteUnitOfWork` for staged audit persistence inside one logical commit boundary.
- `src/SPINbuster.AI/` currently contains the deterministic `IAiGenerationProvider` implementation, prompt-package registry, and Tier 0 scenario controls used to validate the advisory AI path without live services.
- `schemas/ai/` currently contains the authoritative `report-draft-proposal` schema.
- `tests/SPINbuster.Infrastructure.Tests/` contains SQLite integration tests for commit-together behavior, rollback behavior, detached updates, migration metadata presence, migration application, migration idempotence, report persistence, report-draft idempotency enforcement, AI substrate persistence, AI migration compatibility, Knowledge Engine persistence, bounded relationship traversal, citation reload, duplicate-rejection constraints, and atomic AI-state or Knowledge-state plus audit rollback.
- `tests/SPINbuster.AI.Tests/` now contains deterministic provider, failure-classification, and prompt-registry tests for the Tier 0 AI path.
- `spec/knowledge/README.md` defines the authoritative Knowledge Engine foundation boundary.
- `spec/architecture/knowledge-engine-foundation.md` records the foundational architecture and ownership rules for the first Knowledge Engine slice.
- `spec/database/README.md` and `spec/database/knowledge-engine-persistence.md` define the authoritative local SQLite persistence boundary for the Knowledge Engine review candidate.
- `src/SPINbuster.Desktop/` now contains the temporary deterministic console bootstrap host, its narrow composition root, and the local vertical-slice workflow runner for the inspection, report-draft, and deterministic AI proposal paths.
- `tests/SPINbuster.Desktop.Tests/` contains SQLite-backed end-to-end tests for the Desktop workflow, including replay, review action, and failure cases for deterministic AI proposals.

## Current Released Baseline

- `REPORT-DRAFT-SLICE-0.1` is the latest released baseline.
- `AI-DRAFT-PROPOSAL-SLICE-0.1` is the latest released AI baseline.
- `AI-PROPOSAL-EXECUTABLE-SLICE-0.1` is the latest released executable AI baseline.
- `KNOWLEDGE-ENGINE-PERSISTENCE-0.1-RC` is the current review-candidate Knowledge Engine baseline.
- Migration status: no pending model changes, empty-database migration passes, repeated migration is idempotent, and migration history is verified.
- Persistence status: aggregate and staged audit changes commit atomically, roll back atomically, and detached updates are verified.
- Validated vertical-slice path: migrations applied at startup, project created and persisted, inspection session started and persisted, field note captured and preserved, project/session rehydration succeeds, and audit history persists and reloads.
- Validated report-draft path: evidence persists and reloads, interpretation remains separate from raw evidence, authoritative report drafts persist in `Draft`, provenance reload succeeds, duplicate operation IDs do not create a second draft, and report plus audit changes commit atomically.
- Validated AI substrate path: governed report-proposal context manifests persist and reload, deterministic provider output is validated before review, malformed or fabricated output is retained as non-reviewable failure, advisory proposals and audit records commit atomically, repeated migrations are safe, and populated report-draft databases upgrade without losing existing state.
- Validated executable AI path: the Desktop host can request deterministic proposals, replay them idempotently, reload model-run/proposal/attempt/audit history, record `HumanAccepted` or `Rejected` review outcomes, display failed runs with no persisted proposal, and confirm that review actions do not mutate authoritative reports.
