# Architecture Summary

Purpose: Give coding agents a fast architectural orientation without duplicating full specifications.

## Summary

The application owns persistence and workflow orchestration.

Planned subsystem direction:

- SPINbuster will eventually need a `Knowledge Engine` subsystem distinct from `AI`.
- The Knowledge Engine is the engineering-intelligence layer that organizes project knowledge before AI consumes it.
- The intended flow is:
  `Project -> Specifications -> Drawings -> Submittals -> RFIs -> Photos -> Daily Reports -> Inspections -> Reports -> AI Context`
- AI sits after knowledge, not before it.
- A likely future knowledge model centers on `Project` with connected specifications, drawings, details, RFIs, change orders, mix designs, submittals, materials, equipment, field notes, photos, reports, AI proposals, and engineering rules.
- This subsystem is intended to support deterministic engineering queries such as material usage, asset references, threshold exceedances, and impact tracing across reports.

AI models:

- return structured proposals only
- cannot approve reports
- cannot write authoritative records
- cannot broaden project scope

See `spec/architecture/` and `spec/ai/` for the complete governing specifications.
