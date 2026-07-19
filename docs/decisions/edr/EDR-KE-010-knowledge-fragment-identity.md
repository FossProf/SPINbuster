# EDR-KE-010: Knowledge Fragment Identity

Status: Accepted (Candidate Stage)
Superseded-By: `PARSING-AND-FRAGMENT-DOMAIN-CHECKPOINT`
Target: Document Engine parsing-and-fragment foundation

## Decision

Candidate-stage fragment identity is deterministically derived from three governed inputs:

1. Source revision identity (`ImportedSourceId` + content hash)
2. Parser contract identity (parser key + contract version)
3. Normalized locator (locator type + normalized locator value)

The derived identity key is:

```
{ImportedSourceId}:{ParserKey}@{ParserContractVersion}:{LocatorType}:{NormalizedLocatorValue}
```

### Key Properties

- Identity is **parser-run-scoped**: it binds fragment candidates to the exact source bytes and parser contract that produced them.
- Identity is **not revision-stable**: when a new document revision is parsed, fragment candidates receive new identity keys even if locators appear equivalent. Cross-revision fragment equivalence remains a separate future concern.
- Identity is **deterministic**: re-running the same parser version on the same source content with the same locator should produce the same identity key.
- Identity is **collision-resistant within a run**: duplicate identity keys within a single parser run are rejected as domain invariant violations.

### What This Resolves

- When a fragment receives a stable ID: at candidate creation time, derived deterministically from governed inputs.
- Whether fragment identity survives equivalent locator changes across revisions: No, by design. Cross-revision equivalence is a later comparison concern.
- How fragment-level supersession is represented: Deferred to the Knowledge Engine promotion boundary.
- How stale locators are preserved historically: The `LocatorValue` and `NormalizedLocator` are stored immutably on the candidate. Historical preservation is a persistence concern.

### What This Does Not Resolve

- Authoritative fragment identity after promotion to Knowledge Engine (remains deferred)
- Cross-revision fragment matching or equivalence detection (remains deferred)
- Fragment supersession representation (remains deferred to Knowledge Engine)

## Rationale

The candidate-stage identity model is intentionally scoped to the Document Engine's non-authoritative boundary. It provides deterministic, reproducible identity for parser outputs without claiming that identity survives the boundary into authoritative knowledge.

This separation allows the Document Engine to:

- produce deterministic, auditable parser outputs
- detect duplicate candidates within a run
- bind fragment identity to exact source bytes and parser contracts
- leave authoritative knowledge identity and promotion to the Application and Knowledge Engine layers

## Specification Reference

See `spec/documents/parsing-and-fragment-foundation.md` for the complete specification.
