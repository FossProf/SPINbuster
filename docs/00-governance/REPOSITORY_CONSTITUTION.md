# Repository Constitution

## Constitutional Rules

1. The Git repository is the source of truth.
2. Domain owns business truth.
3. Application owns orchestration.
4. Infrastructure owns persistence and external implementation concerns.
5. Presentation layers consume Application contracts.
6. AI advises and never owns authoritative engineering state.
7. Knowledge records are authoritative and project-scoped.
8. Raw observations and evidence remain immutable.
9. Historical revisions are superseded, never silently overwritten.
10. State transitions must be explicit.
11. Audit-worthy changes must be traceable.
12. Provider-specific types must remain outside Domain and Application.
13. Cross-project leakage is prohibited.
14. Database changes require migrations and populated-database upgrade tests.
15. New features must preserve Tier 0 no-AI operation.
16. UI must not become a business-logic layer.
17. Significant decisions require ADR or EDR records.
18. Milestones require validation, continuity updates, review, and an intentional release decision.

## Authority Hierarchy

When sources conflict, use this order:

1. Current code and tests
2. Accepted ADRs and EDRs
3. Authoritative `spec/`
4. `PROJECT_STATE.md` and continuity files
5. Governance documents
6. Historical Drive design archive
7. Chat history

Higher levels override lower levels.

## Change Control

No decision record:

- local refactors that preserve released behavior and boundaries
- test additions that do not change architecture or policy

EDR required:

- subsystem policy choices
- deferred-scope declarations
- implementation-boundary decisions
- release-boundary clarifications
- substantive governance-document changes

ADR required:

- architectural responsibility shifts
- layer-boundary changes
- major persistence or hosting strategy changes
- durable constitutional or authority-hierarchy changes

Migration review required:

- schema changes
- migration snapshots
- upgrade-path validation against existing data

Prototype review required:

- released vertical slices
- executable workflows that validate a full path through the system

Baseline release required:

- named milestones
- passing validation evidence
- continuity updates
- explicit human approval

## Prohibited Practices

- direct `DbContext` usage outside Infrastructure
- mutable raw evidence
- provider SDK types in Application
- silent schema changes
- AI-generated source fabrication
- automatic report approval
- hidden cross-layer dependencies
- unreviewed changes to released invariants
