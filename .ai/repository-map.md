# Repository Map

Purpose: Explain how the repository is organized and where different kinds of work belong.

## Top-Level Structure

- `.ai/` contains agent bootstrap guidance, current priorities, and operating rules.
- `spec/` contains authoritative engineering specifications.
- `docs/` contains human-readable documentation derived from specifications.
- `src/` contains implementation projects.
- `tests/` contains automated verification.
- `database/`, `schemas/`, `prompts/`, `tools/`, and `scripts/` contain supporting assets and workflows.

## Working Rule

Start from `.ai/` for navigation.
Use `spec/` as the source of truth for design and behavior.
Use `docs/` for explanatory and reader-friendly material.

## Change Placement

Put implementation changes in `src/` and `tests/`.
Put engineering rules and subsystem contracts in `spec/`.
Put lightweight agent instructions in `.ai/`.
