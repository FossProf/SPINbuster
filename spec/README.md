# Specification Index

Purpose: Provide a fast navigation map for the authoritative engineering specifications in `spec/`.

## How To Use This Directory

- Start here when onboarding to the specification layer.
- Read only the subsystem specifications relevant to the task at hand.
- Treat `spec/` as authoritative engineering intent.
- Treat `docs/` as reader-friendly explanation derived from or aligned with the specifications.

## Specification Areas

- `spec/ai/` defines how AI works inside SPINbuster, including provider boundaries, proposal behavior, validation, and security constraints.
- `spec/api/` is reserved for future API contracts and service-boundary specifications.
- `spec/architecture/` defines cross-cutting system structure, subsystem boundaries, and architectural intent.
- `spec/database/` defines persistence expectations, database behavior, and migration-oriented specifications.
- `spec/documents/` defines the Document Engine boundary and current durable foundation for import, hashing, processing attempts, and non-authoritative candidates.
- `spec/knowledge/` defines the Knowledge Engine and the conceptual engineering knowledge model used across the repository.
- `spec/requirements/` is reserved for future formal requirements specifications and milestone-scoped requirement sets.
- `spec/rules/` defines the deterministic Rule Engine boundary and executable-rule ownership model.

## Recommended Reading Order

1. `spec/architecture/` for the relevant subsystem boundary
2. The subsystem README for the area being changed
3. The authoritative deep-dive specification for that subsystem
4. Supporting decision records under `docs/decisions/edr/` when an active tradeoff or deferred boundary is involved

## Knowledge Model Entry Points

For the common engineering language of SPINbuster, start with:

1. `spec/knowledge/README.md`
2. `spec/knowledge/engineering-object-model.md`
3. `spec/knowledge/engineering-knowledge-model.md`
