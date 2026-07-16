# Confidence And Uncertainty

Purpose: Define how AI confidence should be represented, interpreted, and constrained.

## Confidence Model

- Confidence is stored as `None`, `Low`, `Medium`, or `High`.
- Confidence is advisory metadata and never grants authority.
- `High` confidence is reserved for proposals with no uncertainty codes.

## Uncertainty Handling

- Uncertainty is captured as explicit machine-readable codes.
- Warnings and uncertainty codes persist alongside the proposal manifest.
- Uncertainty does not mutate authoritative reports; it informs human review.
- Incomplete context should prefer abstention over fabricated certainty.

## Escalation Thresholds

- Any prohibited authority language forces rejection from reviewable status.
- Missing or out-of-scope source references force rejection from reviewable status.
- Abstention is required when the governed context is incomplete or confidence cannot be justified safely.
