# Architecture Summary

Purpose: Give coding agents a fast architectural orientation without duplicating full specifications.

## Summary

The application owns persistence and workflow orchestration.

Planned subsystem direction:

- SPINbuster now has the first `Knowledge Engine` Domain and Application foundation distinct from `AI`.
- The Knowledge Engine is the engineering-intelligence layer that organizes project knowledge before AI consumes it.
- The intended flow is:
  `Project -> Specifications -> Drawings -> Submittals -> RFIs -> Photos -> Daily Reports -> Inspections -> Reports -> AI Context`
- AI sits after knowledge, not before it.
- A likely future knowledge model centers on `Project` with connected specifications, drawings, details, RFIs, change orders, mix designs, submittals, materials, equipment, field notes, photos, reports, AI proposals, and engineering rules.
- This subsystem is intended to support deterministic engineering queries such as material usage, asset references, threshold exceedances, and impact tracing across reports.
- The current foundation boundary covers authoritative knowledge documents, immutable revisions, explicit supersession, project-scoped relationships, and precise citations.
- Parsing, OCR, embeddings, vector search, and automatic authority promotion remain deferred outside this foundation slice.

AI models:

- return structured proposals only
- cannot approve reports
- cannot write authoritative records
- cannot broaden project scope

See `spec/architecture/` and `spec/ai/` for the complete governing specifications.
