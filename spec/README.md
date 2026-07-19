# Specification Index

Purpose: Provide a fast navigation map for the authoritative engineering specifications in `spec/`.

## How To Use This Directory

- Start here when onboarding to the specification layer.
- Read only the subsystem specifications relevant to the task at hand.
- Treat `spec/` as authoritative engineering intent.
- Treat `docs/` as reader-friendly explanation derived from or aligned with the specifications.
- Use `docs/00-governance/ARCHITECTURE_VISION.md` for the platform constitution and `docs/00-governance/ROADMAP.md` for capability evolution planning.

## Specification Areas

- `spec/ai/` defines how AI works inside SPINbuster, including provider boundaries, proposal behavior, validation, and security constraints.
- `spec/api/` is reserved for future API contracts and service-boundary specifications.
- `spec/architecture/` defines cross-cutting system structure, subsystem boundaries, and architectural intent, to be read alongside the governance-layer distinction between Layered Architecture and Capability Architecture.
- `spec/database/` defines persistence expectations, database behavior, and migration-oriented specifications.
- `spec/documents/` defines the Document Engine boundary, the released durable foundation, the current executable review candidate for import, hashing, processing attempts, and non-authoritative candidates, and the parsing-and-fragment foundation for deterministic parser-run identity and non-authoritative fragment candidates.
- `spec/knowledge/` defines the Knowledge Engine and the conceptual engineering knowledge model used across the repository.
- `spec/requirements/` is reserved for future formal requirements specifications and milestone-scoped requirement sets.
- `spec/rules/` defines the deterministic Rule Engine boundary and executable-rule ownership model.

## Recommended Reading Order

1. `docs/00-governance/ARCHITECTURE_VISION.md` for the platform constitution
2. `docs/00-governance/CAPABILITY_MATRIX.md` for engine ownership
3. `spec/architecture/` for the relevant subsystem boundary
4. The subsystem README for the area being changed
5. The authoritative deep-dive specification for that subsystem
6. Supporting decision records under `docs/decisions/adr/` or `docs/decisions/edr/` when an active tradeoff or deferred boundary is involved

## Knowledge Model Entry Points

For the common engineering language of SPINbuster, start with:

1. `spec/knowledge/README.md`
2. `spec/knowledge/engineering-object-model.md`
3. `spec/knowledge/engineering-knowledge-model.md`
