# AI Bootstrap

`.ai/` tells an AI agent how to work on SPINbuster.
`spec/ai/` defines how AI works inside SPINbuster.

## Read Order

1. `.ai/current-priority.md`
2. `.ai/coding-rules.md`
3. `.ai/repository-map.md`
4. Relevant specification under `spec/`
5. Relevant tests
6. Existing implementation files

## Operating Rule

Do not load the entire specification repository for every task.
Load only the subsystem specifications needed for the current task.

## Authoritative Sources

Use `.ai/` for navigation, current priorities, and repository operating guidance.
Use `spec/` for authoritative engineering specifications.
Do not redefine specification content here when a governing spec already exists.

## Minimum Completion Checks

Before finishing work:

- Run the relevant tests for the changed area.
- Verify any changed contracts or schemas still match their specifications.
- Confirm changes stayed within the allowed file boundaries in `.ai/coding-rules.md`.

