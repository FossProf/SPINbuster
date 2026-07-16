# EDR-AI-001

## Title

Authoritative report revision creation from accepted AI proposals

## Status

Deferred

## Target

Post `AI-DRAFT-PROPOSAL-SLICE-0.1-RC`

## Current Rule

- AI proposals are advisory only.
- `HumanAccepted` or `Rejected` proposal states do not create or mutate authoritative `Report` revisions in this slice.
- Human review disposition is persisted separately from authoritative report revisioning.

## Reason

The authoritative acceptance path needs a separate design for revision ownership, provenance carry-forward, human reviewer accountability, and idempotent report mutation semantics.
