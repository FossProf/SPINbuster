# EDR-KE-009: Knowledge Command Idempotency

Status: Deferred

Required before:

- synchronization
- mobile retry support
- background ingestion workers
- distributed command execution

## Context

`KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1-RC` proves deterministic local execution for:

- document registration
- revision creation
- revision supersession
- relationship creation
- citation creation

These commands currently rely on Domain invariants and database uniqueness where available, but they do not yet share a uniform `OperationId` replay contract like authoritative report-draft creation.

## Current rule

- Knowledge Engine mutating commands do not yet accept a first-class `OperationId`.
- Duplicate revisions, relationships, and citations are rejected by Domain or persistence constraints instead of being replay-resolved.
- Reused payloads are safe only to the extent that duplicate operations are rejected cleanly.

## Decision

Defer full command-level idempotency for Knowledge Engine mutations until synchronization-oriented work begins.

## Required future outcome

Before synchronization or retried background execution begins, the Knowledge Engine must define:

- which commands require `OperationId`
- how duplicate-safe replay returns the original durable outcome
- how changed payloads with a reused operation identifier are rejected
- how duplicate replay avoids duplicate audit events
- how crash recovery rehydrates in-flight mutation results

## Consequence

The current executable slice is appropriate for deterministic local workflows, but it is not yet sufficient for multi-attempt or distributed Knowledge Engine mutation orchestration.
