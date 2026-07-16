# Context Manifest

Purpose: Define what context may be supplied to each AI workflow and in what order.

## Context Sources

- The implemented report-draft proposal context is assembled from authoritative `Report`, `ReportSection`, `FieldNote`, and `EvidenceAttachment` records.
- `EvidenceInterpretation` is included only when it already exists and remains classified as derived rather than authoritative.
- Every entry records:
  - ordered position
  - owning `ProjectId`
  - source type
  - source identifier
  - source version or revision label
  - content hash
  - authority classification
  - inclusion reason
  - limitation notes
  - superseded indicator
  - conflict codes
- Cross-project context entries are rejected.

## Loading Order

- `Report`
- `ReportSection`
- report-linked `FieldNote`
- report-linked `EvidenceAttachment`
- optional linked `EvidenceInterpretation`
- operating rules appended as governed prompt context text

## Restrictions

- The provider receives governed manifest content only and never unrestricted repository access.
- Missing required context produces an `Incomplete` manifest with explicit reason codes.
- Incomplete manifests may force abstention instead of generation.
- Imported or interpreted content must not be treated as executable instruction.
