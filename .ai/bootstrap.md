# AI Bootstrap

Purpose: This is the single bootstrap entry point for new AI sessions working on SPINbuster.

## What Is SPINbuster?

SPINbuster is an offline-first engineering knowledge platform with layered code architecture and engine-based capability architecture. The repository is the source of truth; chats are not.

## Current Project Status

- Repository scaffold exists and builds successfully.
- Architecture guardrails are in place and passing.
- The repository includes released foundations for Application, Infrastructure, AI proposal workflows, Knowledge Engine workflows, Document Engine workflows, the local filesystem storage adapter, the parsing and fragment foundation, and the fragment-to-knowledge promotion workflow.
- The latest software baseline is `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`.
- The latest governance baseline is `ARCHITECTURE-VISION-2.0`.
- The roadmap is organized by long-term capability evolution.

## Latest Governance Baseline

- `ARCHITECTURE-VISION-2.0`

## Latest Software Baseline

- `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`

## Active Implementation Package

- `ENGINEERING-WORKFLOW-STANDARD-1.0-RC` — documentation-only governance package (installs the Engineering Work Order Standard on the review branch; awaiting architectural adoption approval; not released)
- `ENGINEERING-ASSERTION-PROMOTION-0.1-RC` — planned; implementation has not begun

## Current Milestone

- `Prototype Vertical Slice`

## Current Active Task

- Governance package review: `ENGINEERING-WORKFLOW-STANDARD-1.0-RC` — install the work-order standard and the Engineering Assertion Promotion planning package (documentation only, awaiting architectural adoption approval)

## Authoritative Files

- `PROJECT_STATE.md`
- `.ai/current-priority.md`
- `.ai/handoff.md`
- `.ai/coding-rules.md`
- `.ai/repository-map.md`
- `docs/00-governance/ARCHITECTURE_VISION.md`
- `docs/00-governance/ENGINEERING_WORK_ORDER_STANDARD.md`
- `docs/00-governance/CAPABILITY_MATRIX.md`
- `docs/00-governance/PLATFORM_COMPLETION_CRITERIA.md`
- `docs/00-governance/AI_BOOTSTRAP.md`
- Relevant `spec/` documents for the subsystem being changed
- `docs/00-governance/ROADMAP.md`
- `docs/03-implementation/work-orders/ENGINEERING-ASSERTION-PROMOTION-0.1-RC.md`
- `docs/decisions/adr/ADR-ARCH-001-platform-engine-model.md`

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
