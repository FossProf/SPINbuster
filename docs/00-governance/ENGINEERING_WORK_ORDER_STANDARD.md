# SPINbuster Engineering Work Order Standard

Version 1.0 — Normative governance standard

## 1. Purpose and Authority

This standard defines how SPINbuster vertical slices, remediation packages, and governance packages are designed, implemented, reviewed, and released. It is normative for all future implementation work orders. A work order that omits required context, constraints, acceptance criteria, validation, or stop conditions is incomplete and shall not be issued for implementation.

Work orders are architectural implementation specifications, not short prompts. They carry the required context, constraints, acceptance criteria, validation, and stop conditions needed to implement a checkpoint without architectural improvisation.

Primary objective:

- Reduce architectural drift and review churn by resolving design intent before implementation begins, while preserving enough discretion for the implementer to choose sound local code structure.

## 2. Governing Principles

- Specifications lead implementation. Code does not define the architecture retroactively.
- Reuse precedes invention. Existing aggregates, repositories, policies, result contracts, and persistence patterns shall be extended before parallel abstractions are introduced.
- Domain owns business meaning and invariants. Application owns orchestration. Outer adapters own technology-specific behavior. Presentation hosts own composition only.
- Authority is derived from governed context and policy; it is never trusted merely because a caller supplies a value.
- Successful replay is based on immutable deterministic identity. Diagnostics, mutable lifecycle state, or candidate IDs are not substitutes for replay identity.
- Authoritative state changes are atomic. Failed operations must not leak partial revisions, citations, relationships, provenance, audit events, or diagnostics.
- Provenance is explicit, durable, immutable, and queryable.
- Released migrations are immutable. Schema evolution is forward-only and upgrade-tested against realistic prior data.
- Queries are project-scoped, bounded, deterministic, and safe for presentation.
- AI, parsing, OCR, and extraction outputs remain non-authoritative until a governed human decision promotes them.
- Every major capability ends in executable proof through the real composition root.
- Documentation must describe implemented behavior accurately; aspirational behavior is clearly marked as deferred.

### 2.1 Repository Precedence and Baseline Refresh

- The checked-in repository is the evidence of current implementation behavior.
- Planning documents govern intended scope and constraints.
- When the repository and planning package conflict, implementation stops and the discrepancy is reported for architectural resolution.
- The implementer SHALL NOT silently follow stale planning language.
- The implementer SHALL NOT silently reinterpret repository behavior.

## 3. Development Lifecycle

1. Roadmap selection — choose the next capability based on released dependencies and exit criteria.
2. Slice overview — explain why the slice exists, why now, dependencies, unlocks, non-goals, and success measures.
3. Architectural reconnaissance — inspect the actual repository and identify reusable components and conflicts.
4. Design brief — record the proposed model, alternatives, risks, and unresolved decisions.
5. Architecture approval — freeze the design boundary before implementation prompts are issued.
6. Work-order package — issue staged checkpoints with explicit scope, tests, and stop gates.
7. Implementation — complete only the active work order.
8. Checkpoint review — review architecture, correctness, persistence, tests, and continuity before proceeding.
9. Integrated RC — prove the vertical slice through the real composition root and produce an honest prototype review.
10. Release decision — tag only after explicit architectural approval.
11. Lessons learned — update the living process record after release.

## 4. Required Package-Level Sections

| Section | Required content |
|---|---|
| Project Context | Current released software baseline, governance baseline, active architectural decisions, known technical debt, test/migration state, and relevant prior lessons. |
| Slice Overview | Purpose, why now, dependencies, capabilities unlocked, explicit deferrals, success criteria, and expected executable proof. |
| Existing Baseline Analysis | Specific aggregates, repositories, use cases, policies, persistence objects, migrations, tests, and documents that shall be reused or extended. |
| Architectural Constraints | Non-negotiable boundaries that apply across all work orders in the package. |
| Design Decisions Already Made | Established decisions that the implementer shall not reopen without a separate design proposal. |
| Expected Public Surface | Likely Domain/Application contracts and ownership boundaries. Guidance, not permission to create unnecessary abstractions. |
| Known Risks | Identity drift, authority bypass, replay shortcuts, persistence leakage, migration edits, ambiguity, concurrency, query exposure, and documentation drift. |
| Review Focus | The exact architectural and correctness questions the reviewer will use. |
| Package Review Checklist | A reusable checklist completed before RC review. |
| Future Integration Notes | What later engines consume this slice and what compatibility should be preserved. |
| Lessons Carried Forward | Prior slice lessons relevant to this package. |

### 4.1 Package Profiles and Proportional Tailoring

Every package SHALL declare exactly one of the following profiles in its header metadata and Document Status. A package is non-compliant if it silently omits required sections; omitted sections SHALL be declared explicitly with justification for the declared profile.

- Every package SHALL declare its profile.
- Tailoring SHALL remove only sections that are genuinely inapplicable to the declared profile.
- Tailoring SHALL NOT remove architecture, validation, or stop controls.
- A package is non-compliant if it silently omits required sections.

**A. Full Vertical Slice**

- Uses the complete lifecycle (Section 3) and all package-level (Section 4) and work-order (Section 5) sections.
- Requires executable proof and RC review.

**B. Remediation or Hardening Package**

- May begin from approved review findings rather than roadmap selection.
- May omit broad slice-overview and roadmap material when not applicable.
- MUST retain defect evidence, architecture constraints, correction scope, regression tests, validation, and stop gate.

**C. Governance or Documentation Package**

- May omit production-code, migration, and executable-workflow sections when not applicable.
- MUST retain purpose, authority, affected artifacts, compatibility impact, validation, scope guard, and stop gate.

## 5. Required Work-Order Structure

| Work-order section | Requirement |
|---|---|
| Header | Package name, work-order number, checkpoint name, date, released baseline, previous checkpoint, and change authority. |
| Objective | The precise capability and outcome for this checkpoint. |
| Architectural rationale | Why this capability belongs here and how it extends the platform. |
| Current baseline | Concrete repository elements that must be reused. |
| Scope | Included work, ownership boundaries, and expected deliverables. |
| Explicit non-goals | Items intentionally excluded to prevent scope expansion. |
| Architecture gate | Mandatory justification before adding projects, abstractions, dependencies, tables, migrations, or cross-layer references. |
| Design direction | Required semantics, invariants, transaction/replay/authority/provenance behavior, and likely abstractions. |
| Behavioral requirements | Testable SHALL statements. |
| Expected test matrix | Specific success, failure, replay, concurrency, persistence, migration, security, and restart scenarios. |
| Deliverables | Files or classes expected, documentation artifacts, and checkpoint report. |
| Validation gate | Exact commands and integrity checks. |
| Stop gate | Where work ends and what must not begin. |
| Architectural watch items | Known failure modes relevant to the checkpoint. |
| Completion report format | Required structured summary from the implementer. |

## 6. Architecture Justification Gate

Before creating a new abstraction, project, dependency, table, migration, background process, provider contract, or cross-layer reference, the implementer shall document:

- The existing abstraction or pattern that was considered first.
- Why extension or composition is insufficient.
- Which layer owns the new concept and why.
- How the change preserves dependency direction.
- How identity, replay, authority, provenance, transactionality, and query bounding are affected.
- What tests prove the abstraction is necessary and correctly bounded.
- What future work is enabled and what coupling is deliberately avoided.

Simplicity rule:

- If the same requirement can be met by extending an existing contract without weakening ownership or invariants, creating a parallel abstraction is not permitted.

## 7. Validation Standard

- Run repository-defined restore using the committed NuGet configuration.
- Run formatting verification without allowing automatic unrelated changes.
- Build the full solution with no new warnings treated as acceptable by default.
- Run focused tests for every changed layer.
- Run the full solution test suite.
- Run pending EF model-change checks whenever persistence is in scope.
- Apply all migrations from a fresh database and from the latest released schema containing representative existing data.
- Verify no released migration artifact changed byte-for-byte.
- Run `git diff --check` and confirm scope containment.
- Run the executable workflow when the checkpoint affects end-to-end behavior.
- Confirm continuity documents are updated only during governance/RC/release checkpoints.

## 8. Stop-Gate Rules

- One work order is active at a time.
- No subsequent work order begins until the current checkpoint is committed, reviewed, and the worktree is clean.
- No tag or release-state change occurs without explicit approval.
- No governance claims may state a capability is complete before executable evidence and tests prove it.
- Unexpected architectural conflicts trigger a design report, not an improvised implementation.
- A partial, honest checkpoint is preferable to an over-scoped completion claim.

## 9. Completion Report Standard

Every implementation response shall include:

- Checkpoint name and disposition.
- Files added and modified, grouped by layer.
- Behavior implemented.
- Design deviations and their justification.
- Tests added, including what each proves.
- Validation commands and results.
- Migration status and released-migration integrity.
- Known limitations or deferred work.
- Confirmation that no tag/release/next work order was started.

## 10. Review Standard

- Review the actual repository diff, not the completion summary alone.
- Prioritize architecture, authority, identity, replay, provenance, transactionality, migration safety, concurrency, query security, and continuity accuracy.
- Distinguish branch coverage from production-policy proof.
- Require real stale-context concurrency tests, historical-schema migration tests, provider-recreation tests, and workflow-level retry tests when relevant.
- Treat inaccurate continuity as a correctness defect because it directs future work.

## 11. Living Lessons Learned

- Do not trust caller-supplied authority.
- Replay belongs to immutable identity; diagnostics and candidate IDs do not define replay.
- Every execution that is not a successful replay should create its own immutable attempt record.
- Atomic authoritative changes must be proven against partial persistence.
- Fresh-database migration tests do not prove upgrade safety.
- Released migrations are immutable historical artifacts.
- Executable proofs reveal cross-layer failures that unit tests miss.
- Queries must enforce project ownership and output bounds at the Application boundary.
- Bounded aliases do not protect data if the raw property remains public.
- Documentation can drift into obsolete architecture unless reviewed against current code.
- Terse prompts increase implementation variance; over-prescriptive prompts prevent engineering judgment. Architect-level work orders provide the correct middle ground.
- No process improvements are appended after a work-order package is declared final; improvements belong in a subsequent governance revision.

## 12. Versioning and Change Control

This document is versioned. Changes to the engineering workflow occur only through a dedicated governance package. The workflow shall not be modified during an active implementation slice. Minor clarifications increment the minor version; changes to required lifecycle stages or mandatory work-order sections increment the major version.
