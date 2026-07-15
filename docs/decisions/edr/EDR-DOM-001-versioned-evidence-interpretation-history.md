# EDR-DOM-001

Title: Versioned evidence interpretation history

Status: Deferred

Target:
`DOMAIN-0.2` or Application vertical slice

Current rule:

- An `EvidenceAttachment` may receive one interpretation.
- An existing interpretation cannot be replaced.
- Additional interpretations require a future versioned model.

Rationale:

The current baseline preserves evidence integrity without prematurely selecting a revision model. Future design may need reviewer identity, model-run provenance, supersession, confidence, and approval state.

