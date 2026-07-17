# AI Bootstrap

Purpose: This is the single bootstrap entry point for new AI sessions working on SPINbuster.

## What Is SPINbuster?

SPINbuster is a layered .NET codebase with AI-assisted capabilities, governed by repository specifications and architecture guardrails. The repository is the source of truth; chats are not.

## Current Project Status

- Repository scaffold exists and builds successfully.
- Architecture guardrails are in place and passing.
- The repository includes the released `AI-DRAFT-PROPOSAL-SLICE-0.1` baseline, the released `AI-PROPOSAL-EXECUTABLE-SLICE-0.1` Desktop executable workflow, the released `KNOWLEDGE-ENGINE-PERSISTENCE-0.1` SQLite persistence baseline, and the released `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1` executable Knowledge Engine baseline.
- The latest released baseline is `DOCUMENT-ENGINE-FOUNDATION-0.1`, and the active review candidate is `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`.

## Latest Released Baseline

- `DOCUMENT-ENGINE-FOUNDATION-0.1`

## Active Review Candidate

- `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`

## Next Planned Implementation Package

- `Determine from prototype review`

## Current Milestone

- `Prototype Vertical Slice`

## Current Active Task

- `Review and validate the first executable Document Engine workflow`

## Authoritative Files

- `PROJECT_STATE.md`
- `.ai/current-priority.md`
- `.ai/handoff.md`
- `.ai/coding-rules.md`
- `.ai/repository-map.md`
- `docs/00-governance/AI_BOOTSTRAP.md`
- Relevant `spec/` documents for the subsystem being changed
- `docs/decisions/status/DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-PROTOTYPE-REVIEW.md`

## Bootstrap Reading Order

1. `PROJECT_STATE.md`
2. `.ai/current-priority.md`
3. `.ai/handoff.md`
4. `.ai/coding-rules.md`
5. `.ai/repository-map.md`
6. `docs/00-governance/AI_BOOTSTRAP.md`
7. Relevant `spec/` documents only
8. Relevant tests
9. Existing implementation files

## Session Start Directive

Read `.ai/bootstrap.md` and follow the prescribed bootstrap sequence. Treat the repository as authoritative. Do not infer architecture beyond the specifications. Report your understanding before making changes.

## Operating Rule

Do not load the entire specification repository for every task.
Load only the subsystem specifications needed for the current task.

## Completion Rule

Before finishing work:

- Update `.ai/current-priority.md` if the active baton changed.
- Update `.ai/handoff.md` after major work sessions.
- Update `PROJECT_STATE.md` when baseline, milestone, phase, or immediate next task changes.
- Keep `docs/00-governance/` aligned with durable repository authority when governance rules change.
