# Fragment-to-Knowledge Promotion

Status: Draft
Baseline: `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1` (source of candidates), `KNOWLEDGE-ENGINE-FOUNDATION-0.1-RC` (target of promotion)

## Purpose

This specification defines how human-accepted fragment candidates become authoritative Knowledge Engine records through a deterministic, auditable, idempotent promotion workflow.

It establishes:

- the promotion lifecycle from accepted candidate to authoritative knowledge
- provenance chain from fragment candidate through to knowledge revision and citation
- idempotency semantics preventing duplicate authoritative records
- supersession handling when a promoted fragment competes with existing authoritative content
- conflict detection and visibility rules
- citation creation and content-hash binding
- invariants that keep AI out of the authority-decision path

## Governing Principle

**Knowledge promotion is strictly deterministic.** AI may propose, but only humans create engineering truth. The promotion workflow is a consequence of explicit human review plus deterministic rules. No AI model, embedding, or inference participates in deciding what becomes authoritative.

This principle is non-negotiable and extends an existing architectural invariant: parsing success does not create authority, and AI generation success does not create authority.

## Scope

This promotion slice owns:

- promotion eligibility validation (preconditions checked deterministically)
- Knowledge Document creation for first-time promotion of a candidate source
- Knowledge Revision creation as an immutable historical record
- Knowledge Citation creation binding a promoted fragment to its revision
- Knowledge Relationship creation for `DerivedFrom` provenance
- provenance metadata carried through promotion (source hashes, parser identity, review metadata)
- idempotent promotion (re-promoting the same candidate does not create duplicate records)
- supersession when a promoted fragment replaces an existing current-authoritative revision
- conflict detection when promotion would create ambiguous authoritative state
- audit events for every promotion lifecycle transition
- Domain invariants enforced within the promotion boundary

This slice does not own:

- AI-assisted classification or extraction
- OCR
- vector search or embedding-based retrieval
- cross-project knowledge sharing
- rule evaluation or constraint checking
- report assembly or proposal generation
- fragment-to-fragment equivalence across revisions
- binary file storage or re-import

## Existing Baseline Dependencies

This slice depends on the following released capabilities:

- `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1`: deterministic parser-run identity, fragment-candidate identity, locator model, review lifecycle, strict UTF-8, parser diagnostics
- `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`: fragment-candidate review disposition (Generated, HumanAccepted, Rejected) with audit trail
- `KNOWLEDGE-ENGINE-FOUNDATION-0.1-RC`: KnowledgeDocument, KnowledgeDocumentRevision, KnowledgeRelationship, KnowledgeCitation domain model and persistence

## Promotion Lifecycle

### State Machine

```text
Eligible  (preconditions met)
-> Promoted (authoritative records created)
-> Verified (optional human verification)

Terminal:
Superseded (by a later promotion of the same conceptual content)
Withdrawn (explicit human withdrawal from authority)
Failed (precondition failure, no records created)
```

### Lifecycle Transitions

```text
Eligible -> Promoted     (deterministic Application workflow succeeds)
Eligible -> Failed       (precondition not met or invariant violation)
Promoted -> Verified     (optional human verification of promoted content)
Promoted -> Withdrawn    (explicit human withdrawal; revision set to Withdrawn lifecycle)
Promoted -> Superseded   (a later promotion replaces this revision as current-authoritative)
```

### Precondition Checklist (Eligible)

All of the following must hold before promotion begins. The Application workflow checks these deterministically and rejects the promotion request if any precondition fails.

1. **Fragment candidate exists** — the `FragmentCandidateId` must resolve to a persisted candidate.
2. **ReviewState is HumanAccepted** — only human-accepted candidates are eligible.
3. **ReviewedBy and ReviewedAtUtc are populated** — the review disposition must carry actor and timestamp.
4. **Parser run completed successfully** — the source `ParserRun` must be in `Completed` state with `ExecutionStatus` of `Completed` or `CompletedWithWarnings`.
5. **Source content is available** — the `ImportedDocumentSource` must be in `Available` status.
6. **Project is Active** — the `Project` must be in `Active` lifecycle state.
7. **Content hash integrity** — the fragment candidate's `SourceContentHash` must match the parser run's `SourceContentHash`.
8. **Identity key integrity** — the fragment candidate's `IdentityKeyHash` must match the SHA-256 of its `IdentityKey`.

### Precondition Failure Behavior

When any precondition fails:

- No authoritative Knowledge Engine records are created or modified.
- A promotion diagnostic is emitted (see `PromotionDiagnostic` below).
- The promotion attempt is recorded with `Failed` status and a failure reason.
- The fragment candidate remains in `HumanAccepted` state (failure does not alter review disposition).
- An append-only audit event is emitted.

## Promotion Records Created

A successful promotion of a single fragment candidate creates the following authoritative records in a single Application-level transaction:

### 1. Knowledge Document (conditional)

A new `KnowledgeDocument` is created when:

- no existing KnowledgeDocument in the project already represents the same conceptual engineering record

A KnowledgeDocument is NOT created when:

- the promoted fragment belongs to a document that already has a KnowledgeDocument (determined by application-level matching, see Document Matching below)

The Application layer is responsible for determining whether the promoted fragment belongs to an existing KnowledgeDocument. This determination is:

- deterministic (based on canonical title, external reference number, document type, and project scope)
- auditable (the matching rationale is recorded)
- idempotent (re-running the same promotion produces the same KnowledgeDocument)

### 2. Knowledge Revision (always)

A new `KnowledgeDocumentRevision` is always created for every successful promotion.

The revision captures:

- `RevisionLabel` — derived deterministically from parser metadata and fragment locator (e.g., `"v1-parsed-1"`)
- `EffectiveDate` — null for parsed content (effective date is an external human-supplied concept)
- `ReceivedAtUtc` — the promotion timestamp
- `SourceAuthority` — `Informational` for parsed content (parsed fragments are informational until human verification upgrades authority)
- `ContentHash` — the `SourceContentHash` of the fragment candidate (preserves binding to exact input bytes)
- `MetadataHash` — a deterministic hash of the parser identity and locator metadata
- `KnowledgeSourceId` — a newly created source record linking to the parser run and fragment candidate provenance

The revision starts in `Received` lifecycle state and `Unverified` verification status.

### 3. Knowledge Citation (always)

A new `KnowledgeCitation` is always created for every successful promotion.

The citation binds:

- `CitedRevisionId` — the newly created revision
- `LocatorType` — mapped from the fragment candidate's `FragmentLocatorType`
- `LocatorValue` — the fragment candidate's normalized locator value
- `RevisionContentHash` — the `SourceContentHash` of the fragment candidate
- `QuotedOrSummarizedText` — null for parsed content (the citation points to the revision, not to an inline quote)

### 4. Knowledge Relationship: DerivedFrom (always)

A `DerivedFrom` relationship is always created:

- `Source` — the KnowledgeSubjectReference for the new revision
- `Target` — a KnowledgeSubjectReference pointing to the ImportedDocumentSource (or a future knowledge source identity)
- `RelationshipType` — `KnowledgeRelationshipType.DerivedFrom`
- `EvidenceOrRationale` — `"Promoted from fragment candidate {FragmentCandidateId} produced by parser {ParserKey}@{ParserContractVersion}"`

This relationship preserves the provenance chain from authoritative revision back to its non-authoritative origin.

### 5. Knowledge Relationship: Supersedes (conditional)

When a promoted revision replaces an existing current-authoritative revision on the same document:

- `SupersedeCurrentRevision` is called on the `KnowledgeDocument`
- The previous revision is marked as `Superseded`
- A `Supersedes` relationship is created between the new and old revision

## Document Matching

The Application layer determines whether a promoted fragment belongs to an existing KnowledgeDocument using deterministic matching rules:

### Matching Strategy

A promoted fragment candidate is considered to belong to an existing KnowledgeDocument when ALL of the following match:

1. **Project scope** — both belong to the same `ProjectId`
2. **Document type** — the fragment's source document type matches (or can be mapped to) the KnowledgeDocument's `DocumentType`
3. **Canonical title equivalence** — the fragment's source metadata yields a canonical title that matches the KnowledgeDocument's `CanonicalTitle` (case-insensitive, whitespace-normalized)
4. **External reference** — when both carry an external reference number, they match; when one is null, the match proceeds on title alone

### No-Match Behavior

When no existing KnowledgeDocument matches:

- A new KnowledgeDocument is created with the first revision as its initial authoritative revision
- No supersession occurs

### Ambiguous Match Behavior

When multiple KnowledgeDocuments match (same title, same project, same type):

- The promotion fails with `AmbiguousDocumentMatch` diagnostic
- No records are created
- The human reviewer must resolve the ambiguity before retrying

## Idempotency

### Promotion Idempotency Key

Each promotion attempt is identified by the tuple:

```
(FragmentCandidateId, ParserRunId)
```

This tuple is unique per candidate and captures the exact parser run that produced the candidate.

### Idempotent Replay

Re-executing a promotion with the same `(FragmentCandidateId, ParserRunId)`:

- if the previous promotion succeeded: returns the existing KnowledgeRevision and KnowledgeCitation without creating new records
- if the previous promotion failed: re-runs the promotion from Eligible state

### Duplicate Prevention

The Application layer enforces idempotency by:

1. Querying for an existing `KnowledgeCitation` whose `RevisionContentHash` matches the candidate's `SourceContentHash` and whose locator matches the candidate's normalized locator
2. If found, returning the existing citation and its revision without creating new records
3. If not found, proceeding with normal promotion

This ensures that the same parsed content is never promoted twice, regardless of how many times the promotion workflow is invoked.

### Partial Failure Recovery

If a promotion succeeds in creating a KnowledgeDocument but fails before creating the citation:

- On retry, the idempotency check finds no existing citation
- The KnowledgeDocument already exists, so document matching finds it
- A new revision is created for the same document (idempotent revision creation based on content hash)
- The citation is created on the second attempt

This is safe because:

- revision creation is append-only (duplicate revisions with the same content hash are detectable)
- citation creation is deterministic and auditable

## Supersession Rules

### When Supersession Occurs

Supersession occurs when a promoted fragment candidate's content should replace an existing current-authoritative revision on the same KnowledgeDocument.

Supersession is triggered when ALL of the following hold:

1. The promoted fragment maps to an existing KnowledgeDocument (via document matching)
2. The KnowledgeDocument already has a `CurrentAuthoritativeRevisionId`
3. The new content hash differs from the current authoritative revision's content hash (same document, different content)

### Supersession Semantics

- The previous revision's lifecycle transitions from `CurrentAuthoritative` to `Superseded`
- The previous revision's verification status transitions to `Superseded`
- The new revision's lifecycle transitions from `Received` to `CurrentAuthoritative`
- The new revision's `SupersedesRevisionId` points to the previous revision
- A `Supersedes` relationship is created between the new and old revision
- Both revisions remain queryable and auditable

### Non-Supersession Cases

Supersession does NOT occur when:

- the promoted content has the same content hash as the current authoritative revision (idempotent re-promotion, no-op)
- the KnowledgeDocument has no current authoritative revision (first promotion creates initial revision)
- the fragment maps to a new KnowledgeDocument (no existing revision to supersede)

### Conflict Detection on Supersession

When a promoted revision would supersede an existing current-authoritative revision, the Application layer checks:

1. **Content hash difference** — if hashes match, no supersession is needed (idempotent)
2. **Source authority comparison** — if the existing revision has higher source authority, a `HigherAuthorityExists` diagnostic is emitted and the promotion fails
3. **Temporal ordering** — the new revision's `ReceivedAtUtc` must be >= the existing revision's `ReceivedAtUtc`

## Conflict Handling

### Conflict Types

The promotion slice detects the following conflict conditions:

| Conflict | Detection | Resolution |
|---|---|---|
| `AmbiguousDocumentMatch` | Multiple KnowledgeDocuments match the promoted fragment | Human resolves before retrying |
| `HigherAuthorityExists` | Existing revision has higher `SourceAuthority` than the promoted fragment | Human overrides explicitly or rejects promotion |
| `ContentHashCollision` | Different fragment candidates produce the same `ContentHash` on the same document | Allowed; both are valid historical revisions |
| `ConcurrentPromotion` | Two promotions for the same fragment run concurrently | Second promotion fails with `DuplicatePromotion` |
| `SupersessionChainBroken` | A promotion attempts to supersede a revision that is already superseded | Promotion fails; human must promote against the current authoritative revision |

### Conflict Visibility

All conflicts are:

- recorded as `PromotionDiagnostic` records with severity `Warning` or `Error`
- emitted as append-only audit events on the promotion record
- visible in the project's knowledge snapshot
- NOT automatically resolved

### No Automatic Conflict Resolution

The promotion workflow never silently resolves conflicts. Every conflict requires explicit human disposition. This preserves the principle that AI and automation do not create engineering truth.

## Citation Rules

### Citation Creation

Every successful promotion creates exactly one `KnowledgeCitation`:

- the citation points to the newly created revision
- the citation carries the locator from the fragment candidate
- the citation carries the content hash from the fragment candidate
- the citation is immutable once created

### Citation Stability

Citations remain valid after supersession because they point to a specific historical revision, not to the current authoritative revision. This is a fundamental property: citations are provenance, not authority.

### Citation Content Hash

The `RevisionContentHash` on the citation must exactly match the `SourceContentHash` of the promoted fragment candidate. This ensures:

- the citation is traceable to exact input bytes
- content tampering is detectable
- reproducibility verification is possible

### Citation Text

For parsed content, `QuotedOrSummarizedText` is null. The citation points to a revision and locator, not to inline content. Future slices may populate this field for human-curated citations.

## Provenance Chain

Every promoted fragment carries a full provenance chain:

```text
FragmentCandidate (non-authoritative)
  |-- ParserRun
  |     |-- ImportedSource (source file)
  |     |-- ParserKey, ParserVersion, ParserContractVersion, ParserContractHash
  |     |-- SourceContentHash, SourceHashAlgorithm, SourceHashAlgorithmVersion
  |-- FragmentLocator (type, raw, normalized)
  |-- ConfidenceBand (parser-determined quality signal)
  |-- HumanAccepted review disposition (actor, timestamp, notes)
  |
  v  [Promotion Workflow]
  |
KnowledgeDocument (authoritative, stable identity)
  |
  v
KnowledgeDocumentRevision (immutable historical record)
  |-- ContentHash = SourceContentHash (binding preserved)
  |-- MetadataHash (parser identity hash)
  |-- SourceAuthority = Informational (parsed content)
  |-- VerificationStatus = Unverified (until human verification)
  |-- ReceivedAtUtc = Promotion timestamp
  |-- SupersedesRevisionId (if superseding)
  |
  v
KnowledgeCitation (durable pointer)
  |-- LocatorType, LocatorValue (from fragment)
  |-- RevisionContentHash (from fragment candidate)
  |
  v
KnowledgeRelationship: DerivedFrom (provenance edge)
  |-- Source: Revision
  |-- Target: Source identity
  |-- Evidence: Parser identity + fragment candidate ID
```

Every downstream artifact can trace its lineage back to the exact parser run, source bytes, and human review decision that produced it.

## Promotion Diagnostic

A promotion diagnostic captures the outcome of a promotion attempt:

```text
PromotionDiagnostic
  |-- Id (PromotionDiagnosticId)
  |-- FragmentCandidateId
  |-- ParserRunId
  |-- ProjectId
  |-- PromotedAtUtc
  |-- Status (Eligible, Promoted, Failed)
  |-- FailureReason (populated on Failed)
  |-- KnowledgeDocumentId (populated on Promoted)
  |-- KnowledgeDocumentRevisionId (populated on Promoted)
  |-- KnowledgeCitationId (populated on Promoted)
  |-- AuditTrail (append-only)
```

This is a durable record, not transient logging. Promotion diagnostics are queryable and visible in the project workflow snapshot.

## Domain Invariants

### INV-PROMO-001: Human Review Required

A fragment candidate must be in `HumanAccepted` review state before promotion is eligible. This is checked deterministically. No AI, embedding, or automated classification can substitute for human review.

### INV-PROMO-002: Parser Run Must Be Completed

The source parser run must be in `Completed` state with `ExecutionStatus` of `Completed` or `CompletedWithWarnings`. Failed or cancelled parser runs cannot produce promotable candidates.

### INV-PROMO-003: Content Hash Integrity

The fragment candidate's `SourceContentHash` must match the parser run's `SourceContentHash` at promotion time. This prevents silent content substitution between review and promotion.

### INV-PROMO-004: Source Availability

The `ImportedDocumentSource` must be in `Available` status. If the source has been marked unavailable, promotion is rejected.

### INV-PROMO-005: Project Active

The project must be in `Active` lifecycle state. Draft, completed, or archived projects cannot receive new promotions.

### INV-PROMO-006: One Citation Per Promotion

Each successful promotion creates exactly one citation. No promotion creates zero or multiple citations.

### INV-PROMO-007: Revision Is Immutable Once Created

A `KnowledgeDocumentRevision` created by promotion cannot be modified after creation. Its `ContentHash`, `MetadataHash`, `SourceAuthority`, and `RevisionLabel` are set at creation time and never change.

### INV-PROMO-008: Supersession Is Explicit

Supersession only occurs when a new revision explicitly points to the revision it supersedes via `SupersedesRevisionId`. No implicit or automatic supersession.

### INV-PROMO-009: Idempotency Preserved

Re-promoting the same fragment candidate with the same parser run identity produces no duplicate authoritative records.

### INV-PROMO-010: AI Excluded From Authority Decisions

No AI model, embedding, inference, or automated classification participates in the promotion decision. The promotion workflow is entirely deterministic and human-initiated.

### INV-PROMO-011: Conflicts Remain Visible

All detected conflicts are recorded and remain visible until explicitly resolved by a human. No conflict is silently resolved or hidden.

### INV-PROMO-012: Provenance Chain Unbroken

Every promoted revision must maintain an unbroken provenance chain back to the originating fragment candidate, parser run, and imported source. No promotion creates an orphaned authoritative record.

## Audit Events

### PromotionWorkflowStarted

Emitted when a promotion workflow begins processing a fragment candidate.

### PromotionPreconditionSucceeded

Emitted when all promotion preconditions are satisfied.

### PromotionPreconditionFailed

Emitted when a promotion precondition fails, with the specific failure reason.

### KnowledgeDocumentCreatedForPromotion

Emitted when a new KnowledgeDocument is created during promotion.

### KnowledgeRevisionCreatedForPromotion

Emitted when a new KnowledgeDocumentRevision is created during promotion.

### KnowledgeCitationCreatedForPromotion

Emitted when a KnowledgeCitation is created during promotion.

### KnowledgeRelationshipCreatedDerivedFrom

Emitted when a `DerivedFrom` relationship is created during promotion.

### KnowledgeRevisionSupersededByPromotion

Emitted when a promotion supersedes an existing current-authoritative revision.

### PromotionConflictDetected

Emitted when a conflict is detected during promotion.

### PromotionCompleted

Emitted when the promotion workflow completes successfully.

### PromotionFailed

Emitted when the promotion workflow fails, with the failure reason.

## Relationship To Existing Types

### Domain Layer

- `KnowledgeDocument` — aggregate root; receives new revisions via `AddInitialRevision` or `SupersedeCurrentRevision`
- `KnowledgeDocumentRevision` — immutable historical record; created by promotion
- `KnowledgeCitation` — durable pointer; created by promotion
- `KnowledgeRelationship` — `DerivedFrom` and `Supersedes` edges; created by promotion
- `FragmentCandidate` — source of promotion; read but not mutated by promotion
- `ParserRun` — source of provenance metadata; read but not mutated by promotion
- `ImportedDocumentSource` — source availability check; read but not mutated by promotion

### Application Layer

- `PromoteFragmentCandidateUseCase` — orchestrates the promotion workflow (new use case)
- `LoadPromotionDiagnosticUseCase` — loads promotion outcome records (new use case)
- Repository contracts for reading/writing promotion records (new repository interfaces)

### Infrastructure Layer

- SQLite persistence for `PromotionDiagnostic`
- Promotion diagnostic repository implementation

## Non-Responsibilities

This specification intentionally does not define:

- AI-assisted content extraction or classification
- OCR integration
- fragment-to-fragment equivalence across revisions
- cross-project knowledge sharing
- vector search or embedding-based retrieval
- rule evaluation or constraint checking
- report assembly
- binary file storage
- UI or presentation contracts
- API contracts
- database persistence schemas (deferred to migration slice)
- file path handling

## Decision Records

This specification resolves the following deferred EDRs:

- `EDR-KE-010` (Knowledge Fragment Identity): the promotion boundary defines how candidate-stage identity transitions to authoritative knowledge identity
- `EDR-KE-011` (Engineering Assertion Promotion): this slice does not implement assertion promotion; assertions remain deferred

This specification does NOT resolve:

- `EDR-KE-006` (AI-Generated Relationship Promotion): AI relationship promotion remains deferred
- `EDR-KE-005` (Automatic Authority Classification): automatic authority classification remains deferred
- `EDR-KE-008` (Multi-Current-Revision Conflict Resolution): multi-current conflict resolution remains deferred

## Review Checks

This specification is successful if it helps future work answer:

- What preconditions must hold before a fragment candidate can be promoted?
- What authoritative records are created during promotion?
- How is idempotency preserved across promotion retries?
- How does promotion handle supersession of existing revisions?
- How are conflicts detected and preserved?
- How does the provenance chain trace from authoritative revision back to source bytes?
- How does this specification enforce the deterministic-only authority principle?
- What prevents AI from participating in the authority-decision path?
