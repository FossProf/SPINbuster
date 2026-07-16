# Security And Privacy

Purpose: Define security boundaries, privacy constraints, and data handling rules for AI features.

## Data Classification

- Raw field notes, evidence metadata, and authoritative report content are governed engineering records.
- AI proposals are advisory derivative artifacts and must remain distinguishable from authoritative records.
- Provider telemetry is non-authoritative and must be normalized before persistence.

## Privacy Rules

- The implemented Tier 0 provider does not send data off-machine.
- Future live providers must be integrated through provider-neutral contracts and explicit review.
- AI audit records must retain only the metadata needed for traceability within the approved boundary.

## Security Controls

- AI receives governed prompt context only.
- Application validates structured output before any proposal becomes reviewable.
- Prohibited authority language is rejected.
- AI output cannot approve, issue, or persist authoritative report revisions in this slice.
- Audit records for context manifests, model runs, and proposals commit through the existing unit-of-work boundary.
