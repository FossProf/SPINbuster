# REPORT-DRAFT-SLICE-0.1 Prototype Review

Date: `2026-07-15`  
Baseline: `REPORT-DRAFT-SLICE-0.1`  
Status: `Reviewed after release`

## What The Slice Validated

- The released local vertical slice can extend from persisted field material into authoritative report-draft creation without breaking the existing `VERTICAL-SLICE-0.1` workflow.
- Report drafts persist as authoritative `Draft` records with domain-controlled initial revision `1`.
- Report provenance persists and reloads through Application-facing contracts, including source field-note references, source evidence references, structured sections, and report audit history.
- `CreateReportDraftCommand` preserves the authoritative boundary while `GenerateReportDraftRequest` remains a side-effect-free context-assembly query.
- The temporary Desktop host remains sufficient for deterministic end-to-end validation of the report-draft path.

## Migration Behavior

- Empty-database migration, repeated migration, and no-pending-model-change checks all passed before release.
- SQLite's table-rebuild warning for the report-table shape change was confirmed as expected provider behavior rather than an unreviewed defect.
- The release hardening pass added an explicit backfill to set legacy report revisions to `1` during migration so no hidden `0` revision survives upgrade.
- A populated `VERTICAL-SLICE-0.1` database upgrade was verified:
  existing `Project`, `InspectionSession`, `FieldNote`, and audit history remained intact;
  the original reload path still worked afterward;
  and a new authoritative report draft could be created successfully after migration.

## Idempotency Review

- `CreateReportDraftCommand` is the first authoritative application mutation that uses explicit `OperationId` idempotency.
- Infrastructure enforces uniqueness at the database level through the persisted operation-to-report mapping.
- Retrying the same operation returns the original `ReportId` instead of creating a second draft.
- A reused `OperationId` with different request content is rejected.
- Duplicate retries do not append duplicate report audit events.
- This is sufficient for the current local baseline; broader command-receipt and replay handling can remain deferred until synchronization work begins.

## Provenance Validation Review

- The released slice correctly rejects nonexistent source IDs and source references outside the selected inspection session.
- Duplicate field-note and evidence source references are now rejected instead of being silently normalized.
- Empty report sections remain invalid.
- Evidence interpretation remains separate from raw evidence and does not overwrite the underlying source record.
- The current provenance model is adequate for authoritative draft creation, reload, and auditability in the local baseline.

## Structured Section Assessment

- The current `ReportDraftSection` structure is sufficient for deterministic draft ownership, revision incrementing, and future proposal-to-authoritative acceptance workflows.
- The model is intentionally conservative: heading plus content only, without premature layout, rendering, or AI-provider concerns.
- Future revisioning will likely need more explicit section identity or semantic typing if AI proposals begin targeting partial section acceptance rather than whole-draft replacement.
- That need is not release-blocking for `REPORT-DRAFT-SLICE-0.1`, but it is the main report-model question to watch before the first AI-assisted proposal slice.

## Defects Uncovered And Resolved Before Release

- The initial report migration left legacy report revisions vulnerable to default `0` values during table rebuild.
  Resolution: fixed before release with explicit migration backfill and rehydration guardrails.
- Duplicate provenance references were initially accepted and normalized.
  Resolution: fixed before release by promoting duplicate source references to Domain invariant violations.
- The initial release-hardening review lacked a populated-database migration test.
  Resolution: fixed before release by adding an explicit upgrade test from the `VERTICAL-SLICE-0.1` database shape.

## Recommended Next Direction

- The next slice should introduce the first local AI-assisted report proposal while preserving the current authoritative report boundary.
- AI output should remain a separately stored proposal linked to model identity, model digest, prompt version, schema version, context-manifest hash, source references, uncertainty/confidence, and model-run audit facts.
- The recommended target baseline is `AI-DRAFT-PROPOSAL-SLICE-0.1`.
