# EDR-DE-007: Fragment Candidate Review Disposition and Promotion Prerequisites

Status: Accepted

## Context

The `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1-RC` package introduces a non-authoritative fragment candidate review lifecycle to the Document Engine. Fragment candidates are produced by parser engines and are not authoritative until explicitly promoted to Knowledge Engine records via an Application-level workflow.

The review lifecycle follows the established `DocumentCandidate.Accept/Reject` pattern (EDR-DE-005) and adds audit-tracked review disposition to fragment candidates.

## Decision

Fragment candidate review states follow a terminal disposition model:

- `Generated(0)` — default state upon creation; non-authoritative and unreviewed
- `HumanAccepted(1)` — reviewed by a human actor; necessary but insufficient for Knowledge promotion
- `Rejected(2)` — reviewed and dismissed; not eligible for future promotion

### Review Semantics

- `Accept` and `Reject` are terminal transitions from `Generated` only
- No reopen workflow exists; once disposed, the state is permanent
- Each disposition requires a non-empty `ReviewedBy` and a non-default `ReviewedAtUtc`
- Optional `ReviewNotes` are bounded to 2,000 characters and trimmed on set
- Each disposition emits an append-only audit event (`FragmentCandidateHumanAccepted` or `FragmentCandidateRejected`)
- Failed transitions (e.g., double accept) throw `LifecycleTransitionException` without emitting audit events

### Promotion Prerequisites

`HumanAccepted` is a necessary but insufficient condition for Knowledge promotion. The following must all hold for a fragment candidate to be eligible for promotion to Knowledge Engine records:

1. `ReviewState` must be `HumanAccepted`
2. A separate Application-level promotion workflow must explicitly promote the candidate
3. The promotion workflow is responsible for creating authoritative Knowledge, citations, assertions, requirements, report content, and rules

### Non-Mutation Guarantee

Review disposition does not alter:
- `IdentityKey` or `IdentityKeyHash`
- `SourceContentHash`
- `Locator` or `LocatorType`
- `ExtractedText` or `TextLength`
- `ParserRunId` or `ImportedSourceId`
- `ContentKind` or `ConfidenceBand`

## Consequence

- Fragment candidates carry an audit-tracked review lifecycle that mirrors `DocumentCandidate` conventions
- The Domain boundary stays clean: review is a Domain-level state machine; promotion is an Application-level workflow
- `HumanAccepted` does not create authoritative records, preserving the non-authoritative boundary
- The review state is persisted via EF Core migration (`AddFragmentCandidateReviewState`) and persisted through `InfrastructureMapper`
- Infrastructure tests update migration count assertions from 7 to 8 to reflect the new migration
