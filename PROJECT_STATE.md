# PROJECT_STATE

## Current Baseline

- `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`
- Status: `Released`
- Build: `Passing`
- Warnings: `0`
- Architecture tests: `12/12 passing`
- Domain tests: `36/36 passing`
- Application tests: `45/45 passing`
- AI tests: `6/6 passing`
- Infrastructure tests: `14/14 passing`
- Desktop end-to-end tests: `3/3 passing`

## Current Branch

- `main`

## Last Completed Milestone

- First executable local Desktop-to-SQLite vertical slice released
- `VERTICAL-SLICE-0.1` prototype review recorded
- Second local report-draft vertical slice implemented and validated
- `REPORT-DRAFT-SLICE-0.1` prototype review recorded
- Governed AI draft proposal substrate released
- Deterministic executable AI proposal workflow released

## Current Implementation Phase

- Deterministic executable AI proposal slice released

## Current Milestone

- Prototype Vertical Slice

## Open ADRs

- None recorded yet

## Open EDRs

- `EDR-DOM-001` Versioned evidence interpretation history (`Deferred`)
- `EDR-APP-001` Application command idempotency (`Accepted for REPORT-DRAFT-SLICE-0.1`)
- `EDR-APP-002` Draft-generation ownership (`Accepted for APPLICATION-0.1`)
- `EDR-AI-001` Authoritative report revision creation from accepted AI proposals (`Deferred`)
- `EDR-AI-002` AI proposal request idempotency and recovery semantics (`Deferred`)

## Outstanding Technical Debt

- `SPINbuster.Desktop` is still a temporary bootstrap host and has not been replaced with a real MAUI client.
- Most test projects are still empty scaffolds outside of `SPINbuster.Architecture.Tests`.
- `spec/architecture/` still needs fuller authoritative content as implementation begins.
- Known deferred design item: versioned evidence interpretation history.
- Audit staging for new aggregates requires deliberate full-slice staging to avoid missing creation events in future handlers.
- SQLite migrations that rebuild existing tables emit EF's expected non-transactional warning and should keep being validated through fresh-database executable runs.
- Human-accepted AI proposals do not yet create authoritative report revisions.
- Concurrent duplicate AI proposal resolution beyond the current `OperationId` replay path remains deferred until live-provider integration.
- AI provider support remains intentionally limited to the deterministic fixture in this baseline.
- The long-term `Knowledge Engine` subsystem is still only a vision direction and has not yet been formalized into authoritative specifications.

## Immediate Next Task

- Await the next implementation package definition after `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`

## Fast Context

- The repository is the source of truth for project state and architecture.
- Start every new AI session from `.ai/bootstrap.md`.
- The current scaffold builds successfully, Infrastructure tests pass, Application tests pass, Domain tests pass, and the architecture guardrails pass.
- The released vertical slice validates migrations applied at startup, project creation and persistence, inspection-session start and persistence, field-note capture and preservation, successful project/session rehydration, and persisted audit-history reload.
- The current released report-draft baseline extends that path through evidence attachment, interpretation, draft-context assembly, authoritative report-draft creation, provenance reload, duplicate-safe operation handling, and report audit-history reload.
- The current released AI baseline adds governed context manifests, deterministic Tier 0 AI proposal generation, structured proposal validation, durable model-run/proposal persistence, and explicit proposal rejection without authoritative report mutation.
- The current executable AI baseline extends the Desktop host through deterministic proposal request, replay verification, durable model-run history reload, human accept or reject review actions, failure display, and explicit verification that report revisions remain unchanged.
- The longer-term product direction includes a distinct `Knowledge Engine` subsystem that organizes engineering knowledge before AI consumes it.
- The prototype review is recorded in `docs/decisions/status/VERTICAL-SLICE-0.1-PROTOTYPE-REVIEW.md`.
- The report-draft prototype review is recorded in `docs/decisions/status/REPORT-DRAFT-SLICE-0.1-PROTOTYPE-REVIEW.md`.

## Current Capabilities

- Create project
- Start inspection session
- Capture immutable field notes
- Attach raw evidence
- Add one non-replaceable interpretation
- Assemble report-draft context
- Create authoritative revision-1 report drafts
- Persist provenance and audit history
- Retry draft creation safely through `OperationId`
- Build governed report-proposal context manifests
- Generate deterministic advisory AI proposals without live AI services
- Persist model runs, run attempts, and advisory AI proposal manifests
- Load and reject advisory AI proposals without mutating authoritative reports
- Record human acceptance as review intent only
- Execute the deterministic AI proposal workflow through the temporary Desktop host

## Future Direction

- Planned major subsystem: `Knowledge Engine`
- Intent: model engineering knowledge separately from AI so project facts, documents, references, field artifacts, and rules become first-class queryable structures.
- Tentative knowledge graph root:
  `Project -> Specifications / Drawings / Details / RFIs / Change Orders / Mix Designs / Submittals / Materials / Equipment / Field Notes / Photos / Reports / AI Proposals / Engineering Rules`
- Core architectural principle:
  AI consumes governed knowledge context; it does not replace the knowledge model.
