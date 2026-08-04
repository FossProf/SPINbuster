# ENGINEERING-ASSERTION-PROMOTION-0.1-RC

Final architect-level work-order package

## Document Status

- Package type: software vertical slice
- Package profile: A — Full Vertical Slice
- Governed by: `SPINbuster Engineering Work Order Standard v1.0`
- Required released dependency: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Expected output: release candidate only
- Implementation authority: one work order at a time; review required between checkpoints

## 1. Project Context

SPINbuster now has durable document import, immutable byte storage, deterministic parsing, fragment review, authoritative knowledge promotion, promotion provenance, deterministic replay, attempt history, authority classification, and SQLite persistence. The next capability must convert authoritative knowledge into structured engineering statements that can later be evaluated deterministically by the Rule Engine. The assertion layer must not reinterpret raw parser output or create a second knowledge system.

- Knowledge is authoritative only after governed promotion.
- Parser and AI outputs remain advisory/non-authoritative.
- Knowledge revisions, citations, relationships, provenance, audit history, and promotion attempts are durable.
- Authority is derived by policy and cannot be caller-escalated.
- Successful replay uses immutable identity.
- Presentation remains delayed; the Desktop host is an executable proof/composition host.
- Released migrations are immutable and the current SQLite path remains the active persistence baseline.

## 2. Slice Overview

Engineering assertions are normalized, machine-evaluable statements derived from authoritative knowledge revisions. They provide the deterministic bridge between human-governed source material and later rule evaluation. An assertion may describe a requirement, observed condition, classification, dimension, material, relationship, prohibition, allowance, or other engineering fact. The slice does not evaluate rules; it creates the governed input that rules will consume.

### Why this slice exists now

- Knowledge promotion has established authoritative source records and provenance.
- The Rule Engine should not parse prose directly because that would make evaluation nondeterministic and difficult to explain.
- Assertions provide stable identity, typed semantics, evidence linkage, authority, lifecycle, contradiction visibility, and replay.
- Assertion promotion can be human-authored first; AI-assisted assertion proposals can be added later without widening authority.

### Capabilities unlocked

- Deterministic Rule Engine evaluation.
- Constraint and requirement comparison.
- Inspection-observation evaluation against requirements.
- Citation-backed findings and report reasoning.
- Explainable retrieval over normalized engineering facts.
- Future AI proposal generation grounded in authoritative assertions rather than raw documents.

### Explicit deferrals

- Automatic AI assertion extraction or promotion.
- Rule definitions and rule execution.
- Automatic unit conversion beyond validation/normalization explicitly approved in the design.
- Automatic contradiction resolution.
- Semantic retrieval and embeddings.
- OCR expansion.
- User-interface work.
- Cloud/server deployment changes.

### Success criteria

- An authorized human can promote an authoritative knowledge revision/citation into a structured assertion.
- Assertion identity is deterministic and replay-safe.
- Every assertion is traceable to authoritative knowledge, citation, actor, policy, and promotion decision.
- Duplicates and contradictions are explicit and durable.
- Unauthorized or insufficient-authority promotion is rejected without partial state.
- Assertions reload after restart and remain presentation-agnostic.
- The executable proof traverses import through assertion promotion and conflict detection.

## 3. Existing Baseline Analysis

| Area | Baseline to reuse |
|---|---|
| Domain | `Project`, `KnowledgeDocument`, `KnowledgeDocumentRevision`, `KnowledgeCitation`, `KnowledgeRelationship`, `PromotionIdentity`/`Record`/`Attempt`/`Diagnostic`/`Provenance`, authority levels, audit trail model, strongly typed IDs. |
| Application | Command/query handlers, repositories, unit of work, authority policy, promotion use cases, bounded snapshots, structured logging and sensitive-data rules. |
| Infrastructure | SQLite `DbContext`, forward migrations, value converters, repository patterns, optimistic concurrency, migration integrity tests, deferred-reference handling. |
| Documents | Immutable content storage and deterministic parsing; not assertion authority. |
| Desktop | Executable workflow composition through Application contracts only. |
| Governance | Architecture Vision 2.0, roadmap, EDR/ADR discipline, released-migration immutability, human-authority rule. |

## 4. Architectural Constraints

- Assertions consume authoritative knowledge revisions and citations; they do not consume raw `FragmentCandidate` or parser output as their authority source.
- An assertion is authoritative only through an explicit human-controlled promotion workflow.
- AI may eventually propose assertion candidates, but AI cannot assign authority or create authoritative assertions directly.
- Assertion identity shall be deterministic, normalized, versioned, and owned by Domain.
- Successful replay shall be based only on assertion promotion identity.
- Assertion history is immutable; corrections produce superseding assertions or explicit dispositions rather than mutation.
- Contradictions are preserved as governed records and never silently overwritten or automatically resolved.
- Application queries shall be project-scoped, bounded, ordered, and sanitized.
- Persistence shall preserve the Domain model without leaking EF concerns upward.
- No new project or subsystem is created unless the Architecture Justification Gate is satisfied.
- No released migration is modified.
- The Rule Engine remains a downstream consumer and is not implemented in this slice.
- Authoritative knowledge does not automatically create an authoritative engineering assertion; assertion promotion is a distinct governed human decision.
- Knowledge authority is an input to assertion authority; it is not inherited automatically.
- Structured assertion candidates/proposals remain non-authoritative until promoted by an authorized human workflow.
- AI, parsing, normalization, and extraction cannot bypass assertion review.
- The reviewer SHALL be presented with the canonical assertion and its supporting authoritative evidence before promotion is recorded.
- Successful promotion SHALL record reviewer identity, review timestamp, authority basis, authority-policy version, source `KnowledgeDocumentRevision`, supporting `KnowledgeCitation`/evidence, canonical `AssertionIdentity`, and promotion/attempt history.
- Failed assertion promotions remain retryable through immutable attempt history.
- Successful replay remains `AssertionIdentity`-based only.

## 5. Design Decisions Already Made

- Assertions are distinct from knowledge revisions: knowledge preserves authoritative source content; assertions normalize specific engineering meaning.
- Assertions should reference a specific `KnowledgeDocumentRevision` and at least one `KnowledgeCitation` or equivalent evidence reference.
- Promotion requires an active project, authoritative source revision, valid citation/evidence, authorized actor, and governed authority classification.
- Assertion identity includes project, normalized subject, predicate, normalized value representation, qualifier/modality where semantically relevant, source revision/evidence context, and assertion contract version.
- Identity rules are finalized in WO1 before persistence begins.
- Assertion state is append-only/immutable after creation except for explicit lifecycle transitions governed by Domain.
- Conflict detection produces explicit outcomes: duplicate, compatible, superseding, contradictory, ambiguous, unsupported, or insufficient authority.
- Automatic conflict resolution is out of scope.
- No schema migration is created before the Domain/Application design is approved.

## 6. Expected Public Surface (Guidance)

Names may be refined during WO1, but ownership and responsibilities should remain equivalent. Reuse existing naming conventions and avoid unnecessary interfaces.

- `EngineeringAssertionId` and `AssertionPromotionIdentity` (Domain).
- `EngineeringAssertion` aggregate/entity and value objects for subject, predicate, value, modality, qualifier, unit/measurement representation, authority, and lifecycle (Domain).
- `AssertionEvidence` or equivalent reference binding to `KnowledgeDocumentRevision` and `KnowledgeCitation` (Domain).
- `AssertionConflict` or equivalent durable conflict representation (Domain).
- `IEngineeringAssertionRepository` and conflict/evidence repositories only where separate ownership is justified (Application ports).
- `PromoteEngineeringAssertion` command/use case and result (Application).
- `LoadEngineeringAssertionSnapshot` bounded query (Application).
- Authority policy extension or assertion-specific policy only if the existing policy cannot represent the required governed classification without distortion.
- SQLite records, repositories, value converters, and one forward migration after design approval.
- Desktop executable workflow result and formatter extensions using Application DTOs only.

## 7. Known Risks

- Creating a second knowledge model instead of a normalized assertion layer.
- Identity drift caused by including mutable display text or excluding semantically relevant qualifiers.
- Over-normalization that erases engineering meaning; under-normalization that creates duplicate assertions.
- Caller-controlled authority escalation.
- Replay based on source IDs or diagnostics rather than immutable promotion identity.
- Linking assertions directly to non-authoritative fragments.
- Automatic contradiction resolution hidden inside promotion.
- Unit conversion that changes meaning or silently rounds values.
- Partial persistence of assertion, evidence, conflict, provenance, audit, or diagnostic state.
- Fresh-schema-only migration testing.
- Unbounded snapshots or leakage of source text/sensitive paths.
- Documentation claiming Rule Engine capability before rule evaluation exists.

## 8. Review Focus

- Is the assertion/knowledge boundary clear?
- Is identity deterministic, complete, normalized, and versioned?
- Is authority derived and auditable?
- Is replay exclusive to immutable identity?
- Is provenance complete to revision, citation, actor, policy, and decision?
- Are conflicts explicit and non-destructive?
- Are authoritative writes atomic?
- Are migrations upgrade-safe and released migrations untouched?
- Are query contracts bounded, project-scoped, and sanitized?
- Does the executable proof use the real composition root without presentation business logic?
- Does continuity accurately distinguish implemented behavior from deferred Rule Engine work?

## 9. Package Review Checklist

- [ ] Assertion identity is deterministic and tested across normalization variations.
- [ ] Assertions reference authoritative revisions and evidence only.
- [ ] Authority is derived and unauthorized elevation is rejected.
- [ ] Successful replay uses assertion promotion identity only.
- [ ] Each non-replay execution has its own attempt/outcome record if an attempt model is used.
- [ ] Duplicate and contradiction outcomes are durable and explainable.
- [ ] No automatic conflict resolution occurs.
- [ ] Atomicity tests verify no partial state on commit failure/concurrency conflict.
- [ ] Historical-schema migration tests include representative assertions and duplicates.
- [ ] Queries enforce project isolation and bounds.
- [ ] Restart/provider recreation preserves assertions, evidence, conflicts, and audit history.
- [ ] Architecture tests prevent forbidden dependencies.
- [ ] Assertion promotion is a distinct governed human decision; knowledge authority alone does not create an authoritative assertion.
- [ ] The reviewer is presented with the canonical assertion and supporting authoritative evidence.
- [ ] Successful promotion records reviewer identity, review timestamp, authority basis, authority-policy version, source revision, supporting citations/evidence, canonical identity, and promotion/attempt history.
- [ ] Failed promotions remain retryable through immutable attempt history.
- [ ] Successful replay is AssertionIdentity-based only; AI/parsing/normalization/extraction cannot bypass assertion review.
- [ ] Continuity and prototype review match current code.
- [ ] No tag or release occurs before explicit approval.

## 10. Future Integration Notes

- The Rule Engine will consume authoritative assertions and evidence references; it shall not need parser or document-storage dependencies.
- Retrieval will index assertions while preserving links back to knowledge revisions and citations.
- Context manifests may include assertions according to authority, lifecycle, conflict, and scope policy.
- AI execution may propose non-authoritative assertion candidates using governed context, but promotion remains human-controlled.
- Authoritative reports may cite assertions and their evidence while preserving revision-level provenance.

## 11. Lessons Carried Forward

- Use one identity model and one replay path.
- Do not deduplicate failures by candidate/source ID.
- Do not expose raw failure or source content through result DTOs.
- Prove database concurrency with two stale contexts, not a deliberately wrong token.
- Prove migrations against actual prior schemas and representative data.
- Keep governance claims behind executable proof.
- Split large remediation work into reviewable checkpoints.
- Use medium-detail architect work orders; do not leave foundational semantics to implementation improvisation.

## WO0 — Architectural Reconnaissance

Header metadata:

- Package: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Checkpoint: `ASSERTION-ARCHITECTURE-RECONNAISSANCE-CHECKPOINT`
- Released baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Change authority: This work order only
- Next action: Stop for architectural review

### Objective

Perform a read-only physical review of the released repository and produce the approved Slice Design Brief for Engineering Assertion Promotion.

### Architectural Rationale

This checkpoint prevents a parallel assertion model from being invented without reference to the released Knowledge, Promotion, Audit, Authority, and persistence foundations.

### Current Baseline

- Released Fragment-to-Knowledge Promotion workflow and RC review.
- `KnowledgeDocument`, `KnowledgeDocumentRevision`, `KnowledgeCitation`, `KnowledgeRelationship`, and promotion provenance/attempt models.
- Authority policies and source authority levels.
- Existing rule-engine boundary specifications and empty Rules project.
- Application command/query/repository patterns and bounded snapshots.
- SQLite migration history, model configuration, and integrity tests.

### Repository Baseline Verification

Before any design direction is trusted, WO0 SHALL verify against the actual repository:

- Released baseline/tag existence for `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`.
- Current aggregate/interface/repository names referenced by this package.
- Current migration count and migration integrity (released migrations byte-for-byte).
- Current test baseline (passing counts by project).
- Any intervening releases or governance changes since the baseline.
- Continued validity of every package assumption in Sections 3–5.

If any material assumption is stale, WO0 stops and produces a design discrepancy report for architectural resolution. WO0 SHALL NOT silently follow stale planning language and SHALL NOT silently reinterpret repository behavior.

### Scope

- Read all relevant source, tests, specs, ADRs/EDRs, roadmap, continuity, and migration artifacts.
- Inventory reusable abstractions and identify where assertion concepts belong.
- Propose assertion identity inputs and normalization risks.
- Propose lifecycle, authority, provenance, evidence, conflict, supersession, replay, and concurrency semantics.
- List design alternatives and select a recommendation with tradeoffs.
- Produce a one-to-three page Slice Design Brief and a repository-grounded implementation map.

### Explicit Non-Goals

- No production code, test code, schema, migration, project, or DI changes.
- No continuity/release-state changes.
- No finalization of public names before the design brief review.
- No Rule Engine implementation.

### Architecture Gate

Before introducing any new aggregate, repository, table, policy, interface, or dependency, document why an existing component cannot be extended without weakening ownership or invariants. No new project is permitted in this work order unless explicitly approved after the gate.

### Design Direction

- Assertions must extend the authoritative Knowledge flow rather than bypass it.
- Identify whether a new aggregate is required and why a value object or `KnowledgeRelationship` alone is insufficient.
- Separate assertion semantics from rule semantics.
- Define what must be versioned in the assertion contract.
- Identify the minimal set of persisted entities needed for the vertical slice.

### Behavioral Requirements

- SHALL produce no executable behavior changes.
- SHALL identify every existing abstraction considered for reuse.
- SHALL verify released baseline/tag existence, current aggregate/interface/repository names, current migration count and integrity, current test baseline, intervening releases/governance changes, and continued validity of all package assumptions.
- SHALL stop with a design discrepancy report when any material package assumption is stale.
- SHALL include unresolved questions rather than silently choosing assumptions.
- SHALL recommend whether WO1 requires an EDR/ADR update.

### Required Test Matrix

- No code tests; validate that referenced files/classes/migrations actually exist.
- Released baseline/tag, current aggregate/interface/repository names, current migration count/integrity, and current test baseline verified against the repository.
- Cross-check roadmap and current continuity for stale assumptions.
- Confirm the proposed design can support duplicate and contradiction handling without Rule Engine logic.

### Deliverables

- Slice Design Brief.
- Baseline inventory with file/class references.
- Decision list: accepted, rejected, deferred.
- Risk register and recommended WO1 boundaries.

### Validation Gate

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore --verify-no-changes`
- `dotnet build SPINbuster.sln --no-restore -m:1`
- Run focused tests for each changed project
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
- Confirm no released migration changed
- Confirm scope contains only this work order

### Stop Gate

Commit this checkpoint only after all required behavior and validation pass. Do not begin the next work order, create a release tag, update release state, or broaden scope. Await architectural review with a clean worktree.

### Architectural Watch Items

- Inventing an assertion model from generic knowledge rather than repository reality.
- Confusing assertions with `KnowledgeRelationship` or `RuleResult`.
- Allowing parser fragments to become authoritative evidence directly.
- Designing persistence before identity and lifecycle are approved.

### Required Completion Report

- Files changed by layer
- Behavior implemented
- Design deviations and justification
- Tests added and what they prove
- Validation results
- Migration status
- Known limitations/deferred work
- Confirmation that no tag/release/next WO was started

## WO1 — Assertion Domain Specification

Header metadata:

- Package: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Checkpoint: `ASSERTION-DOMAIN-SPECIFICATION-CHECKPOINT`
- Released baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Change authority: This work order only
- Next action: Stop for architectural review

### Objective

Freeze the assertion semantics in specifications and decision records before executable implementation.

### Architectural Rationale

Identity, lifecycle, evidence, authority, and contradiction semantics are expensive to change after persistence. This checkpoint establishes the contract the remaining work orders implement.

### Current Baseline

- Approved WO0 Slice Design Brief.
- Existing Domain validation conventions and auditable entity model.
- Knowledge authority, citation, relationship, and promotion invariants.
- Existing EDR/ADR structure and specification organization.

### Scope

- Define `EngineeringAssertion` and supporting value objects conceptually.
- Define deterministic identity formula and normalization rules.
- Define subject, predicate, value, unit/measurement, qualifier, modality, temporal/applicability scope, authority, and lifecycle semantics.
- Define evidence/provenance requirements to revision and citation.
- Define duplicate, compatible, superseding, contradictory, ambiguous, unsupported, and insufficient-authority outcomes.
- Define the closed deterministic comparison contract that WO4 implements.
- Define replay, attempt, audit, supersession, and immutability behavior.
- Update specification indexes and create/resolve EDRs as needed.

### Explicit Non-Goals

- No Domain/Application implementation.
- No migrations or EF records.
- No automatic extraction, unit conversion engine, rule evaluation, or contradiction resolution.
- No speculative UI contracts.

### Architecture Gate

Before introducing any new aggregate, repository, table, policy, interface, or dependency, document why an existing component cannot be extended without weakening ownership or invariants. No new project is permitted in this work order unless explicitly approved after the gate.

### Design Direction

- Use a canonical assertion representation with explicit contract version.
- Define which fields participate in identity and which are display metadata.
- Define normalization without erasing engineering distinctions.
- Define exact authority requirements for initial promotion and supersession.
- Define assertion promotion as a distinct governed human decision that does not follow automatically from knowledge authority.
- Define whether assertion correction uses supersession, rejection, or withdrawal and how history remains intact.
- Define bounded lengths and allowed nullability.

### Closed Deterministic Comparison Contract

WO1 SHALL define a closed deterministic comparison contract before WO4 starts, covering:

- Predicates eligible for comparison.
- Supported value types.
- Canonical-unit requirements.
- Numeric equality/tolerance policy, if any.
- Modality comparison.
- Applicability/scope/qualifier comparison.
- Temporal comparison.
- Abstention behavior.

When deterministic comparison is not defined by the approved assertion contract, the system SHALL return `ComparisonUnsupported` or `ReviewRequired`. It SHALL NOT infer compatibility, contradiction, or supersession.

### Behavioral Requirements

- SHALL document one authoritative identity algorithm.
- SHALL require at least one authoritative evidence reference.
- SHALL prohibit direct authoritative creation from AI/parser outputs.
- SHALL define assertion promotion as a distinct governed human decision that does not follow automatically from knowledge authority.
- SHALL specify the promotion record: reviewer identity, review timestamp, authority basis, authority-policy version, source `KnowledgeDocumentRevision`, supporting citation/evidence, canonical `AssertionIdentity`, and promotion/attempt history.
- SHALL specify that AI, parsing, normalization, and extraction cannot bypass assertion review.
- SHALL specify retryability of failed promotions through immutable attempt history and `AssertionIdentity`-based successful replay.
- SHALL define the closed deterministic comparison contract and the `ComparisonUnsupported`/`ReviewRequired` abstention rule.
- SHALL define terminal and non-terminal lifecycle transitions.
- SHALL define explicit outcomes for every conflict category.
- SHALL define atomic persistence expectations and replay behavior.
- SHALL define query-safe projections and sensitive-data exclusions.

### Required Test Matrix

- Spec examples showing equivalent normalization yields same identity.
- Spec examples showing materially different qualifier/unit/modality yields different identity.
- Lifecycle transition table including invalid transitions.
- Conflict decision table.
- Authority matrix.
- Closed comparison contract table with the abstention behavior for unsupported predicates/value types.
- Promotion-record completeness example (reviewer, timestamp, authority basis, policy version, source revision, evidence, canonical identity).
- Provenance chain example from assertion to knowledge revision/citation/imported source/reviewer.

### Deliverables

- Assertion foundation specification.
- Updated specification README/index.
- EDR/ADR updates with alternatives and rationale.
- Approved glossary and contract-version rule.

### Validation Gate

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore --verify-no-changes`
- `dotnet build SPINbuster.sln --no-restore -m:1`
- Run focused tests for each changed project
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
- Confirm no released migration changed
- Confirm scope contains only this work order

### Stop Gate

Commit this checkpoint only after all required behavior and validation pass. Do not begin the next work order, create a release tag, update release state, or broaden scope. Await architectural review with a clean worktree.

### Architectural Watch Items

- Identity fields that are mutable or presentation-specific.
- Implicit units or modality.
- Authority encoded as a command parameter.
- Using contradiction as a boolean without counterpart linkage/evidence.
- Leaving supersession semantics to WO4.

### Required Completion Report

- Files changed by layer
- Behavior implemented
- Design deviations and justification
- Tests added and what they prove
- Validation results
- Migration status
- Known limitations/deferred work
- Confirmation that no tag/release/next WO was started

## WO2 — Domain and Application Foundation

Header metadata:

- Package: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Checkpoint: `ASSERTION-DOMAIN-APPLICATION-CHECKPOINT`
- Released baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Change authority: This work order only
- Next action: Stop for architectural review

### Objective

Implement the approved assertion Domain model and Application workflows, entirely in-memory/testable without persistence-specific behavior.

### Architectural Rationale

This checkpoint proves business semantics and orchestration before schema commitments. It should reveal identity, lifecycle, authority, replay, and provenance defects while changes are still inexpensive.

### Current Baseline

- Approved WO1 specification and decision records.
- Existing `AuditableEntity`/`AuditEvent` patterns.
- Authority policy, current user, clock, unit of work, repository port conventions.
- Knowledge revision/citation repositories and project lifecycle checks.
- `PromotionIdentity`/`Attempt` lessons and bounded snapshot patterns.

### Scope

- Implement strongly typed IDs, assertion aggregate/entity, value objects, identity calculation, lifecycle, evidence references, and conflict representation approved by WO1.
- Implement repository ports and fakes.
- Implement `PromoteEngineeringAssertion` command/use case/result.
- Implement `LoadEngineeringAssertionSnapshot` bounded query.
- Implement authority classification/authorization by extending the existing policy model where possible.
- Implement idempotent replay using assertion promotion identity only.
- Implement immutable attempt/outcome history if approved by WO1.
- Implement structured logging and audit staging.
- Add focused Domain, Application, and Architecture tests.

### Explicit Non-Goals

- No EF records, migration, SQLite repository, Desktop workflow, Rule Engine, AI provider, OCR, or reporting changes.
- No new project.
- No automatic contradiction resolution.
- No raw source text in public results.

### Architecture Gate

Before introducing any new aggregate, repository, table, policy, interface, or dependency, document why an existing component cannot be extended without weakening ownership or invariants. No new project is permitted in this work order unless explicitly approved after the gate.

### Design Direction

- Domain owns identity derivation and lifecycle invariants.
- Application verifies project, source revision/citation ownership, authority, and promotion eligibility.
- Create the replay record/identity before mutable eligibility checks only where historical successful replay is safe and specified.
- Every new execution that is not successful replay gets a distinct attempt/outcome.
- Failure handling must not reuse prior diagnostics by candidate/evidence ID.
- All success artifacts are staged for one atomic `UnitOfWork` commit.
- Queries return summaries rather than unrestricted source text.

### Behavioral Requirements

- SHALL create an assertion only from an authoritative knowledge revision and valid evidence.
- SHALL reject wrong-project evidence without leaking metadata.
- SHALL derive authority through policy and reject unauthorized elevation.
- SHALL record reviewer identity, review timestamp, authority basis, authority-policy version, source `KnowledgeDocumentRevision`, supporting evidence, and canonical `AssertionIdentity` for every successful promotion.
- SHALL reject any path where knowledge authority alone implies assertion authority without the governed human promotion decision.
- SHALL keep failed promotions retryable through immutable attempt history.
- SHALL produce deterministic identity for equivalent normalized input.
- SHALL produce different identity for semantically distinct input.
- SHALL replay successful identity without mutation or reevaluation of mutable eligibility.
- SHALL allow retry after retryable failure and preserve each attempt.
- SHALL preserve immutable provenance and audit events.
- SHALL enforce bounded snapshot results and failure summaries.

### Required Test Matrix

- Identity reproducibility and divergence matrix.
- All lifecycle transitions and invalid-transition non-emission of audit events.
- Happy-path promotion and exact provenance.
- Promotion record completeness (reviewer, timestamp, authority basis, policy version, source revision, evidence, canonical identity).
- Knowledge authority alone does not create an authoritative assertion; the governed human promotion decision is required.
- Failed promotion -> correction -> success retry through immutable attempt history.
- Wrong project, inactive project, missing revision, non-authoritative revision, missing citation/evidence.
- Unauthorized authority elevation.
- Successful replay before mutable state validation.
- Failure -> failure -> success attempt history.
- Different target assertion identity remains independent.
- Commit failure does not report success and leaves no partial in-memory repository state.
- Logging EventIds/scopes and sensitive-data exclusion.
- Architecture dependency guards.

### Deliverables

- Domain and Application implementation.
- Focused fakes organized by subsystem.
- Updated DI registrations only for Domain/Application services where needed.
- Test report and checkpoint summary.

### Validation Gate

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore --verify-no-changes`
- `dotnet build SPINbuster.sln --no-restore -m:1`
- Run focused tests for each changed project
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
- Confirm no released migration changed
- Confirm scope contains only this work order

### Stop Gate

Commit this checkpoint only after all required behavior and validation pass. Do not begin the next work order, create a release tag, update release state, or broaden scope. Await architectural review with a clean worktree.

### Architectural Watch Items

- Copying knowledge promotion code instead of extracting/reusing shared mechanics where ownership permits.
- Generic abstractions that hide assertion domain intent.
- Dual replay paths.
- Diagnostics that block retries.
- Leaking full evidence text or file paths.
- Creating a migration before this checkpoint is approved.

### Required Completion Report

- Files changed by layer
- Behavior implemented
- Design deviations and justification
- Tests added and what they prove
- Validation results
- Migration status
- Known limitations/deferred work
- Confirmation that no tag/release/next WO was started

## WO3 — SQLite Persistence Foundation

Header metadata:

- Package: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Checkpoint: `ASSERTION-PERSISTENCE-CHECKPOINT`
- Released baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Change authority: This work order only
- Next action: Stop for architectural review

### Objective

Persist the approved assertion model, promotion identity/attempts, evidence/provenance, and conflicts safely through SQLite.

### Architectural Rationale

Persistence must preserve the Domain contract exactly, prove upgrade safety, and maintain atomicity and concurrency without introducing business logic into repositories.

### Current Baseline

- Approved WO2 Domain/Application contracts and tests.
- Existing `DbContext`, model configuration, strongly typed ID converters, repository patterns, audit persistence, concurrency token pattern, migration integrity tests.
- Latest released schema at the beginning of this slice.

### Scope

- Create persistence records and mappings.
- Implement SQLite repositories and DI registration.
- Create the minimum forward migration(s) required by the approved model.
- Add identity/uniqueness indexes and optimistic concurrency where necessary.
- Implement atomic commit behavior for assertion, evidence, provenance, conflicts, diagnostics, attempts, and audits.
- Add rehydration mapping validation.
- Add fresh-schema, historical-upgrade, provider-recreation, concurrency, rollback, ordering, and bounds tests.
- Add released-migration integrity guard.

### Explicit Non-Goals

- No new assertion semantics.
- No changes to released migrations.
- No Desktop workflow or Rule Engine.
- No database-provider abstraction beyond existing project patterns unless the Architecture Gate proves necessity.

### Architecture Gate

Before introducing any new aggregate, repository, table, policy, interface, or dependency, document why an existing component cannot be extended without weakening ownership or invariants. No new project is permitted in this work order unless explicitly approved after the gate.

### Design Direction

- Use one authoritative canonical identity hash derived from Domain-defined canonical material.
- Backfill/upgrade migrations must compute real values before creating non-null/unique constraints.
- Detect duplicates explicitly during upgrade.
- Use true stale-context concurrency tests.
- On failed commit, rollback and clear tracked/staged state before recording a failure in a clean transaction if failure history is required.
- Queries order deterministically and apply bounds in the database where practical.

### Behavioral Requirements

- SHALL round-trip every assertion field, evidence link, authority basis, lifecycle, identity, audit event, and conflict link.
- SHALL enforce deterministic identity uniqueness at the database boundary.
- SHALL preserve independent attempt histories for retries and different targets.
- SHALL prevent partial assertion graphs on failure.
- SHALL preserve data after provider disposal/recreation.
- SHALL upgrade from the released schema containing multiple projects, knowledge revisions, citations, and representative duplicate candidates.
- SHALL leave no pending model changes.

### Required Test Matrix

- Fresh migration creates expected tables/indexes/FKs.
- Historical upgrade with multiple distinct assertions succeeds.
- Deliberate duplicate canonical identity produces explicit failure/reconciliation behavior.
- All IDs and value objects round-trip.
- Two stale contexts cause one concurrency failure.
- Commit failure leaves no assertion/evidence/provenance/conflict residue.
- Retry history reloads in chronological order.
- Project-scoped bounded queries prevent cross-project leakage.
- All migrations apply twice safely where applicable and snapshot matches model.
- Released migration drift guard.

### Deliverables

- Records, mappings, repositories, converters, DI.
- Forward migration(s) and designer/snapshot.
- Infrastructure integration tests.
- Migration integrity report.

### Validation Gate

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore --verify-no-changes`
- `dotnet build SPINbuster.sln --no-restore -m:1`
- Run focused tests for each changed project
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
- Confirm no released migration changed
- Confirm scope contains only this work order
- `dotnet tool run dotnet-ef migrations has-pending-model-changes` using the repository-approved startup project/context
- Apply migrations from fresh database and latest released schema fixture
- Verify released migration files byte-for-byte

### Stop Gate

Commit this checkpoint only after all required behavior and validation pass. Do not begin the next work order, create a release tag, update release state, or broaden scope. Await architectural review with a clean worktree.

### Architectural Watch Items

- Empty-string/default backfills before unique indexes.
- In-memory concurrency checks presented as database concurrency.
- Repositories making authority/conflict decisions.
- Modifying released migration designer files through formatting.
- Application results exposing persistence-only fields.

### Required Completion Report

- Files changed by layer
- Behavior implemented
- Design deviations and justification
- Tests added and what they prove
- Validation results
- Migration status
- Known limitations/deferred work
- Confirmation that no tag/release/next WO was started

## WO4 — Conflict and Contradiction Handling

Header metadata:

- Package: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Checkpoint: `ASSERTION-CONFLICT-HANDLING-CHECKPOINT`
- Released baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Change authority: This work order only
- Next action: Stop for architectural review

### Objective

Implement explicit, durable, authority-aware assertion comparison outcomes without automatic resolution.

### Architectural Rationale

The Rule Engine and human review depend on conflicts remaining visible and explainable. Promotion must not hide contradictions by overwrite or arbitrary selection.

### Current Baseline

- Persisted assertion foundation from WO3.
- WO1 conflict decision table and authority matrix.
- Existing `KnowledgeRelationship` patterns and promotion conflict lessons.
- Current optimistic concurrency and attempt history.

### Scope

- Implement comparison/classification behavior for exact duplicate, compatible, superseding, contradictory, ambiguous subject, unsupported value/unit, insufficient authority, and temporal-order conflicts.
- Persist conflict records or relationships with both assertion identities, evidence, classification, actor/policy, timestamps, and status.
- Integrate conflict classification into promotion atomically.
- Implement bounded conflict snapshot/query.
- Implement explicit human disposition lifecycle only if approved in WO1; otherwise preserve unresolved conflicts.
- Add Domain, Application, Infrastructure, and architecture tests.

### Explicit Non-Goals

- No rule evaluation.
- No automatic winner selection or contradiction resolution.
- No semantic/LLM comparison.
- No broad unit-conversion engine.
- No UI.

### Architecture Gate

Before introducing any new aggregate, repository, table, policy, interface, or dependency, document why an existing component cannot be extended without weakening ownership or invariants. No new project is permitted in this work order unless explicitly approved after the gate.

### Design Direction

- Comparison is deterministic and based on canonical assertion semantics.
- WO4 implements only the approved closed comparison matrix from WO1; it is never a general rule engine.
- When comparison semantics are unsupported or ambiguous, abstain with `ComparisonUnsupported`/`ReviewRequired`; do not infer compatibility, contradiction, or supersession.
- Exact duplicates replay or return duplicate outcome without a new authoritative assertion, according to WO1.
- Compatible assertions remain distinct only when identity/semantics justify it.
- Supersession requires higher authority or explicit governed human action and preserves prior assertion history.
- Contradictions link both sides and their evidence; neither is silently deleted; both are preserved.
- Ambiguity is an explicit outcome, never `FirstOrDefault` behavior.
- Conflict creation and assertion promotion are one atomic business transaction.
- Retries create immutable attempts; successful replay remains identity-based.

### Behavioral Requirements

- SHALL classify every approved conflict category explicitly.
- SHALL implement only the approved closed comparison matrix.
- SHALL preserve both assertions and evidence when a contradiction is established.
- SHALL abstain when semantics are unsupported or ambiguous (`ComparisonUnsupported`/`ReviewRequired`).
- SHALL NOT mutate any authoritative assertion during abstention.
- SHALL avoid general rule evaluation, semantic/AI comparison, and automatic winner selection.
- SHALL reject equal/higher-authority silent supersession.
- SHALL reject unauthorized override.
- SHALL preserve deterministic result after restart.
- SHALL prevent duplicate conflict records through conflict identity/replay rules.
- SHALL keep conflict query project-scoped and bounded.

### Required Test Matrix

- Supported exact comparison (duplicate normalized assertion).
- Supported contradiction with opposing values.
- Modality contradiction (e.g., required vs. forbidden modality).
- Scope/qualifier mismatch classified per the closed matrix.
- Unit mismatch classified per canonical-unit rules or abstained.
- Unsupported predicate returns `ComparisonUnsupported`/`ReviewRequired`.
- Unsupported value type returns `ComparisonUnsupported`/`ReviewRequired`.
- Abstention/review-required outcome preserves both assertions and their evidence.
- Proof that no authoritative assertion is mutated during abstention.
- Compatible assertion with different applicability/qualifier.
- Higher-authority supersession.
- Equal-authority conflict.
- Higher-existing-authority conflict.
- Direct contradiction with opposing values/modalities.
- Ambiguous subject match.
- Unsupported unit/value representation.
- Temporal ordering violation.
- Replay of prior conflict.
- Concurrent conflict creation.
- Commit failure/rollback leaves no partial conflict or assertion.
- Provider recreation preserves conflict graph and ordering.

### Deliverables

- Conflict Domain/Application implementation.
- Persistence additions and forward migration only if WO1/WO3 design requires them; otherwise reuse existing structures.
- Bounded conflict query.
- Focused tests and checkpoint report.

### Validation Gate

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore --verify-no-changes`
- `dotnet build SPINbuster.sln --no-restore -m:1`
- Run focused tests for each changed project
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
- Confirm no released migration changed
- Confirm scope contains only this work order
- Run true concurrent/stale-context conflict tests
- Verify no partial rows in every affected table after failed conflict transaction

### Stop Gate

Commit this checkpoint only after all required behavior and validation pass. Do not begin the next work order, create a release tag, update release state, or broaden scope. Await architectural review with a clean worktree.

### Architectural Watch Items

- Embedding Rule Engine logic in conflict classification.
- Using AI to decide compatibility/contradiction.
- Collapsing all conflict types into a generic failure.
- Allowing a command to select authority/winner without policy.
- Creating relationship explosion without deterministic uniqueness.

### Required Completion Report

- Files changed by layer
- Behavior implemented
- Design deviations and justification
- Tests added and what they prove
- Validation results
- Migration status
- Known limitations/deferred work
- Confirmation that no tag/release/next WO was started

## WO5 — Executable Vertical Slice and RC

Header metadata:

- Package: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Checkpoint: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
- Released baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
- Change authority: This work order only
- Next action: Stop for architectural review

### Objective

Prove the complete assertion-promotion capability through the real composition root and prepare an honest release-candidate review.

### Architectural Rationale

The executable workflow is the acceptance proof that Domain, Application, persistence, authority, replay, provenance, conflict handling, restart behavior, and presentation boundaries work together.

### Current Baseline

- Approved WO0–WO4 checkpoints with clean history.
- Real Desktop composition root and existing import/parse/review/knowledge-promotion workflows.
- Bounded assertion/conflict snapshot queries and SQLite persistence.
- Current governance/continuity/review-document conventions.

### Scope

- Extend/create a Desktop executable workflow using Application commands/queries only.
- Demonstrate import -> parse -> human accept -> knowledge promotion -> assertion promotion -> reload.
- Demonstrate successful replay.
- Demonstrate duplicate and contradiction outcomes.
- Demonstrate unauthorized authority elevation rejection.
- Demonstrate retryable failure -> correction -> success with persisted attempt history.
- Demonstrate restart/provider recreation and repeated-run preservation.
- Demonstrate project isolation and bounded outputs.
- Create console formatter output that exposes IDs and bounded summaries only.
- Create RC prototype review, update specs/readmes/continuity/roadmap accurately, and run full validation.

### Explicit Non-Goals

- No Rule Engine.
- No release tag or release-state change.
- No UI beyond executable proof formatting.
- No AI assertion extraction.
- No unrelated technical-debt refactoring.

### Architecture Gate

Before introducing any new aggregate, repository, table, policy, interface, or dependency, document why an existing component cannot be extended without weakening ownership or invariants. No new project is permitted in this work order unless explicitly approved after the gate.

### Design Direction

- Use the real DI composition root and persistent SQLite/file storage paths.
- Executable results must distinguish created, replayed, duplicate, conflict, failed, and retried outcomes.
- Authority isolation must be visible: AI/parser cannot create authoritative assertions.
- The promotion step must be the explicit human reviewer decision, presented with the canonical assertion and supporting authoritative evidence, and must record reviewer identity, review timestamp, authority basis, authority-policy version, source revision, evidence, and canonical identity.
- Provenance output must trace assertion -> knowledge revision -> citation -> source/reviewer/policy without exposing paths or raw content.
- RC review must list real gaps and deferred work; do not mark Rule Engine or AI extraction as implemented.
- Continuity must use exact released/RC names, test counts, migration counts, and architectural terminology.

### Behavioral Requirements

- SHALL complete the end-to-end workflow through assertion reload.
- SHALL prove assertion identity replay across repeated runs.
- SHALL prove duplicate/contradiction persistence and reload.
- SHALL prove failure/retry attempt history after restart.
- SHALL prove that knowledge authority alone does not create an authoritative assertion; the governed human promotion decision is required and recorded.
- SHALL demonstrate that the reviewer is presented with the canonical assertion and supporting authoritative evidence.
- SHALL prove no authoritative mutation from unreviewed or AI-only input.
- SHALL prove no cross-project query leakage.
- SHALL produce a prototype review with no unsupported completion claims.
- SHALL stop at RC awaiting architectural review.

### Required Test Matrix

- Happy path full workflow.
- Promotion record contains reviewer identity, review timestamp, authority basis, authority-policy version, source revision, supporting evidence, and canonical identity.
- Failed promotion retries through immutable attempt history after correction.
- Idempotent replay produces no additional assertion/evidence/conflict/audit rows.
- Duplicate outcome.
- Contradiction outcome with both evidence chains.
- Unauthorized authority escalation.
- Inactive project or unavailable source/revision then retry success.
- Commit/concurrency failure leaves prior authoritative state intact.
- Provider recreation and second execution preserve prior data.
- Console output excludes absolute paths, source text, secrets, and unbounded failure details.
- All architecture and migration integrity guards remain green.

### Deliverables

- Desktop runner/bootstrapper/result/formatter and tests.
- Prototype review document.
- Updated specifications and indexes.
- Updated `.ai` continuity, `PROJECT_STATE`, `ROADMAP` Current State, `IMPLEMENTATION_LOG`.
- Full validation report and release recommendation.
- No tag.

### Validation Gate

- `dotnet restore SPINbuster.sln --configfile NuGet.Config`
- `dotnet format SPINbuster.sln --no-restore --verify-no-changes`
- `dotnet build SPINbuster.sln --no-restore -m:1`
- Run focused tests for each changed project
- `dotnet test SPINbuster.sln --no-build -m:1`
- `git diff --check`
- Confirm no released migration changed
- Confirm scope contains only this work order
- Run the assertion executable workflow at least twice against the same database/provider root
- Verify provider recreation/restart proof
- Verify active continuity contains no stale architecture terms
- Verify RC review and roadmap describe deferred Rule Engine/AI extraction accurately

### Stop Gate

Commit this checkpoint only after all required behavior and validation pass. Do not begin the next work order, create a release tag, update release state, or broaden scope. Await architectural review with a clean worktree.

### Architectural Watch Items

- Calling repository/DbContext directly from Desktop.
- Using executable fixtures that bypass production authority policy.
- Claiming successful supersession/contradiction resolution that production policy blocks.
- Updating continuity before the final code state is known.
- Using test count alone as release evidence.

### Required Completion Report

- Files changed by layer
- Behavior implemented
- Design deviations and justification
- Tests added and what they prove
- Validation results
- Migration status
- Known limitations/deferred work
- Confirmation that no tag/release/next WO was started

## 12. Package Completion Criteria

- All six work orders completed and individually reviewed.
- Full solution builds and tests pass with no unexplained new warnings.
- All migrations apply from fresh and released-schema databases; no pending model changes.
- Released migrations are unchanged.
- Executable workflow passes repeated-run and provider-recreation proof.
- Authority, replay, provenance, conflict, atomicity, query security, and documentation review pass.
- Prototype review identifies any remaining gaps honestly.
- Assertion promotion is verified as a distinct governed human decision with the full promotion record (reviewer identity, review timestamp, authority basis, authority-policy version, source revision, supporting evidence, canonical identity).
- Failed promotions retry through immutable attempt history; successful replay is AssertionIdentity-based only.
- Package remains an RC until explicit release approval.
