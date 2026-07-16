# JSON Schemas

Purpose: Describe the structured output schemas required from AI components.

## Schema Inventory

- `schemas/ai/report-draft-proposal.schema.json`
  - schema ID: `report-draft-proposal`
  - schema version: `1.0.0`
  - purpose: non-authoritative AI proposal for advisory report-draft content

## Validation Rules

- JSON must parse successfully.
- Required top-level fields must exist.
- `confidenceBand` must be one of `None`, `Low`, `Medium`, or `High`.
- Every section must contain non-empty `heading` and `content`.
- Every source reference must identify a supported source type and an existing governed source.
- Duplicate source references are rejected.
- High confidence cannot be paired with uncertainty codes.
- Abstention requires `confidenceBand = None` and no proposed sections.
- Prohibited authority language prevents reviewable status.
