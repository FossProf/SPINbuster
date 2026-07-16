# EDR-AI-002

## Title

AI proposal request idempotency and recovery semantics

## Status

Deferred

## Required Before

Live Ollama integration, non-deterministic local providers, or broader retry orchestration

## Current Rule

- `RequestReportDraftProposalCommand` accepts an explicit `OperationId`.
- Repeating the same operation with the same request fingerprint replays the original `ModelRunId` and `ProposalId` when available.
- Reusing the same `OperationId` with different request content is rejected.
- The requested `ModelRun` and `ContextManifest` are committed before provider execution so cancellation and provider failures do not disappear.

## Deferred Work

- deterministic concurrent duplicate resolution beyond the current database uniqueness guard
- explicit recovery policy for runs stranded after host termination during provider execution
- multi-attempt replay policy for long-running or leased AI executions

## Reason

The current local deterministic provider slice is safe for review-candidate evaluation, but live provider integration needs stronger duplicate-resolution and crash-recovery rules than the first Tier 0 path requires.
