# AI Bootstrap

Purpose: This is the single bootstrap entry point for new AI sessions working on SPINbuster.

## What Is SPINbuster?

SPINbuster is a layered .NET codebase with AI-assisted capabilities, governed by repository specifications and architecture guardrails. The repository is the source of truth; chats are not.

## Current Project Status

- Repository scaffold exists and builds successfully.
- Architecture guardrails are in place and passing.
- The repository is ready to begin Domain foundation work.

## Current Baseline

- `SKELETON-0.1`

## Current Milestone

- `Prototype Vertical Slice`

## Current Active Task

- `Implement Domain foundation`

## Authoritative Files

- `PROJECT_STATE.md`
- `.ai/current-priority.md`
- `.ai/handoff.md`
- `.ai/coding-rules.md`
- `.ai/repository-map.md`
- Relevant `spec/` documents for the subsystem being changed

## Bootstrap Reading Order

1. `PROJECT_STATE.md`
2. `.ai/current-priority.md`
3. `.ai/handoff.md`
4. `.ai/coding-rules.md`
5. `.ai/repository-map.md`
6. Relevant `spec/` documents only
7. Relevant tests
8. Existing implementation files

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

