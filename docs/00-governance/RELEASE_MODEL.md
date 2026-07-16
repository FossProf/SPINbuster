# Release Model

## Standard Development Lifecycle

```text
Package Definition
→ Implementation
→ Local Validation
→ Release Candidate
→ Architecture Review
→ Hardening
→ Release
→ Prototype Review
```

## Stage Meanings

Package Definition:
The intended scope, boundaries, and acceptance criteria are defined.

Implementation:
The code, tests, and documents for the package are created.

Local Validation:
Restore, format, build, and targeted tests are run locally.

Release Candidate:
The package is recorded as validated but pending review and release approval.

Architecture Review:
The package is reviewed for boundary correctness, lifecycle rules, migration behavior, and future cost of change.

Hardening:
Review findings are addressed, edge cases are covered, and documentation is updated.

Release:
The baseline is intentionally approved, committed, tagged, and recorded.

Prototype Review:
A released executable slice is evaluated for validated assumptions, defects, friction, and next-step recommendations.

## Naming Patterns

```text
SKELETON-0.1
DOMAIN-0.1
APPLICATION-0.1
INFRASTRUCTURE-0.1
<FEATURE>-SLICE-0.1
<FEATURE>-FOUNDATION-0.1
<FEATURE>-PERSISTENCE-0.1
```

Release-candidate naming:

```text
<MILESTONE>-RC
```

## Mandatory Release Evidence

- clean build
- zero warnings unless explicitly accepted
- focused tests
- architecture tests
- full solution tests
- EF pending-model-change check for schema work
- empty-database migration test
- populated-database upgrade test when applicable
- rollback or atomicity tests when applicable
- continuity-file updates
- prototype review when a full vertical slice is released
- explicit human release approval
- commit and tag

> Codex and other coding agents must not tag or release milestones automatically unless explicitly instructed.

## Expected Continuity Updates

```text
PROJECT_STATE.md
.ai/current-priority.md
.ai/handoff.md
.ai/repository-map.md
docs/03-implementation/IMPLEMENTATION_LOG.md
```

## Prototype Reviews

Prototype reviews are required for released vertical slices and other end-to-end executable milestones.

They should capture:

- validated assumptions
- defects uncovered
- architectural friction
- deferred decisions becoming urgent
- migration findings
- usability or operational observations
- next recommended slice
