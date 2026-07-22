# EDR-DE-006: Fragment Identity Contract-Version Choice

Status: Accepted
Supersedes: part of `EDR-KE-010`
Target: Document Engine fragment candidate identity derivation
Checkpoint: `FRAGMENT-INTEGRITY-HARDENING-CHECKPOINT`

## Decision

Canonical fragment identity is derived from `ParserContractVersion`, not from `ParserVersion`.

The identity key formula:

```
{ImportedSourceId}:{ParserKey}@{ParserContractVersion}:{LocatorType}:{NormalizedLocatorValue}
```

`ParserVersion` (the implementation version) is deliberately excluded. Two parser implementations with different `ParserVersion` values but the same `ParserKey` and `ParserContractVersion` produce identical fragment identity keys for the same source and locator.

## Rationale

The parser contract defines the observable behavior: what fragments are produced, what locators are assigned, and what content is extracted. The contract version is the governed surface that downstream consumers reason about.

Parser implementation changes that do not alter observable behavior (performance fixes, internal refactoring, dependency updates) should not create new fragment identities. If an implementation change does alter behavior, the contract version must be bumped to reflect the new contract generation.

This follows the contract-identity philosophy:

- **Contract identity** (chosen): identity tracks the governed behavior specification. Implementation changes that retain the same contract produce the same identity. This encourages stability and avoids unnecessary identity churn.
- **Implementation identity** (rejected): any implementation change creates new identity. This would fragment identity on every parser update even when output is identical, making downstream review and promotion noisier.

## Consequences

- Parser maintainers must bump `ParserContractVersion` whenever a parser change alters fragment output (different locators, different extracted text, different content boundaries).
- Parser maintainers may bump `ParserVersion` freely for internal changes that do not alter observable output, without affecting fragment identity.
- The 5-column replay key `(ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash)` still includes `ParserVersion` to prevent replaying an older implementation over a newer one. Identity and replay serve different purposes.

## Specification Reference

See `spec/documents/parsing-and-fragment-foundation.md` for the identity derivation formula.
See `EDR-KE-010` for the original candidate-stage identity decision.
