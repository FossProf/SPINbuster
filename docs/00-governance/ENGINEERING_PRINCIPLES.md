# Engineering Principles

## Core Orientation

- Offline-first
- Human-first
- Deterministic before probabilistic
- Provenance over inference
- Traceability over convenience
- Immutable history
- Explicit state transitions
- Audit important operations
- Project isolation
- Provider independence
- Backward-compatible evolution
- Test before expansion
- Thin presentation layers
- Knowledge before AI
- Rules before recommendations
- Graceful degradation
- Security and privacy by default

## Governing Principles

> SPINbuster shall remain operationally correct without AI.

> Knowledge is authoritative; rules are deterministic; AI is advisory; UI is a consumer.

> Every engineering conclusion shall be traceable to evidence.

> Artificial intelligence may propose, classify, summarize, extract, rank, and recommend. It shall not independently approve, certify, issue, or persist authoritative engineering conclusions.

## Practical Meaning

Offline-first:
The system must remain useful in local and degraded environments.

Human-first:
Engineering judgment, review, and approval remain human-owned.

Deterministic before probabilistic:
Rules, invariants, and traceable workflows should exist before probabilistic assistance expands.

Provenance over inference:
The system should retain source lineage even when higher-level conclusions are available.

Traceability over convenience:
Convenience features must not obscure how a conclusion was formed.

Immutable history:
Raw observations, evidence, and superseded knowledge must remain inspectable.

Explicit state transitions:
Lifecycle changes must occur through named operations, not incidental mutation.

Audit important operations:
State changes that matter operationally or legally must leave durable audit facts.

Project isolation:
Knowledge, evidence, and context must not leak across projects unless explicitly modeled later.

Provider independence:
Core layers should not depend on SQLite, PostgreSQL, Ollama, or provider SDK types.

Backward-compatible evolution:
New slices should extend the model without casually breaking released invariants.

Test before expansion:
Each slice should be validated before broader feature growth.

Thin presentation layers:
Desktop, Server, and future UI surfaces consume Application contracts rather than owning business logic.

Knowledge before AI:
Authoritative project knowledge must be modeled before AI is trusted to consume it.

Rules before recommendations:
Deterministic rule evaluation should constrain or guide advisory outputs.

Graceful degradation:
Optional AI or provider failures must not make core workflows incorrect.

Security and privacy by default:
Project data should be handled conservatively, with least-necessary exposure and explicit boundaries.
