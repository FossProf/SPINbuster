# KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1 Prototype Review

Date: 2026-07-16
Status: Review Candidate
Baseline: `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1-RC`

## What This Slice Validated

- The temporary Desktop host can execute a deterministic Knowledge Engine workflow through Application commands and queries only.
- Stable document identity persists across revision changes.
- Superseded revisions remain reloadable while the current authoritative revision updates correctly.
- Project-scoped `Clarifies` relationships persist, reload, and traverse through the presentation-safe snapshot query.
- Revision-scoped citations persist against the expected authoritative revision and reload with audit context.
- The new project knowledge snapshot query returns durable presentation data without exposing mutable Domain aggregates.
- Repeated snapshot queries are side-effect free.
- Existing authoritative report and AI proposal state remains unchanged by Knowledge Engine workflow execution.
- Expected workflow failures are presented as understandable outcomes in the executable path instead of crashing the scripted demo.

## Defects Uncovered

- The first snapshot-query implementation used non-deterministic ordering keys that did not compare safely under LINQ sorting. The query was hardened to order by explicit primitive keys.
- The exact `dotnet restore` validation command required sandbox escalation because the .NET SDK attempted to read the user-level NuGet configuration path outside the workspace.
- Parallel validation runs can transiently produce file-lock copy warnings in test output even when the repo is healthy; the final build was rerun in isolation and completed with `0` warnings.

## Friction Observed

- Revisioning remains intentionally explicit and reviewable, but the current command surface is verbose for future ingestion workflows that will need repeat-safe orchestration.
- Citation audit visibility required an explicit Application-layer audit fact because citations are durable records rather than aggregate-owned auditable entities.
- The read-only project knowledge snapshot necessarily fans out across documents, revisions, citations, relationships, and audit history. This is acceptable for the current local slice, but future ingestion and retrieval workloads will need deliberate query-shaping guidance.
- SQLite migration execution still emits the previously accepted provider warning around the report-table rebuild path during a fresh executable run. The workflow completes successfully, but the warning should remain documented rather than ignored.

## Deferred Decisions Becoming More Urgent

- `EDR-KE-009` Knowledge command idempotency is now required before synchronization, background ingestion, or retry-heavy document workflows.
- Document parsing and chunking (`EDR-KE-002`) is now close enough to implementation that chunk provenance, locator normalization, and retry semantics should be resolved before automated ingestion begins.
- Cross-project knowledge sharing (`EDR-KE-007`) remains deferred, but the new executable graph path makes the project-isolation boundary more valuable and more visible.

## Readiness Assessment

- Ready for continued local deterministic Knowledge Engine work: Yes
- Ready for document ingestion and chunking: Not yet
- Blocking reasons before ingestion:
  - command idempotency is not yet defined for Knowledge Engine mutations
  - parsing and chunking ownership is still deferred
  - locator normalization rules beyond the current conservative validation are not yet specified

## Desktop Host Assessment

`SPINbuster.Desktop` remains adequate as a temporary bootstrap host for one more deterministic local slice. It is still thin, disposable, and Application-driven.

It should not absorb:

- MAUI assumptions
- file-upload workflows
- parsing pipelines
- live AI provider orchestration
- synchronization behavior

## Recommended Next Package

- Governance review of `KNOWLEDGE-ENGINE-EXECUTABLE-SLICE-0.1-RC`
- Then: `KNOWLEDGE-ENGINE-INGESTION-FOUNDATION-0.1-RC` or equivalent parsing/chunking preparation slice, with `EDR-KE-009` and `EDR-KE-002` treated as active design inputs
