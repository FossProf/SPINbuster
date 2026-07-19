# Parsing and Fragment Foundation

Status: Review Candidate (Executable Proof Validated)
Baseline: `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`
Supersedes: `EDR-KE-010` (candidate-stage fragment identity resolved here)

## Purpose

This specification defines the first durable Domain boundary for deterministic document parsing and non-authoritative fragment candidate production.

It establishes:

- parser-run identity and lifecycle
- fragment-candidate identity, locator model, and content binding
- deterministic identity derivation from governed inputs
- non-authoritative boundary enforcement
- invariants that prevent parser outputs from becoming authoritative knowledge without explicit promotion

## Scope

This foundation owns:

- parser-run lifecycle (Created, Running, Completed, Failed, Cancelled)
- parser contract and version identity
- fragment-candidate identity derived from source + parser contract + normalized locator (not source revision; see design note below)
- immutable locator value objects for whole-document, page, paragraph/line range, and structural path
- fragment-candidate content binding (extracted text or bounded payload)
- source-hash binding for reproducibility verification
- append-only audit events for parser-run and fragment-candidate lifecycle
- duplicate candidate rejection within a single parser run
- deterministic PlainTextDocument parser adapter producing WholeDocument, Paragraph, and LineRange locators
- SQLite persistence for parser runs and fragment candidates
- idempotent replay with 5-column unique index preventing accidental cross-version replay
- terminal failure states (Failed, Cancelled) with descriptive reasons and audit trail
- authority isolation: fragment candidates cannot become Knowledge, Report, or AI records directly

This foundation does not own:

- OCR
- layout reconstruction
- semantic interpretation
- AI extraction
- authoritative Knowledge Document or Knowledge Revision promotion
- cross-revision fragment equivalence or matching
- retrieval ranking
- parser SDK selection or adapter implementation
- EF Core persistence
- file path handling
- presentation or UI

## Design Decisions

### Fragment Identity: Candidate Stage

Fragment identity at the candidate stage is deterministically derived from three inputs:

1. **Imported source identity**: the `ImportedSourceId` of the parsed source
2. **Parser contract identity**: parser key and parser contract version string
3. **Normalized locator**: the locator type plus normalized locator value

The deterministic identity key is:

```
{ImportedSourceId}:{ParserKey}@{ParserContractVersion}:{LocatorType}:{NormalizedLocatorValue}
```

This identity is intentionally parser-run-scoped and not revision-stable. When a new revision of a document is parsed, fragment candidates receive new identity keys even if the locator appears equivalent. Cross-revision fragment equivalence remains a later comparison and promotion concern.

### Collision Handling

Duplicate candidates within the same parser run are rejected by identity key. If two fragments within a single run produce the same identity key, the second is rejected with a `DomainInvariantException`. This enforces that parser output is deterministic and non-ambiguous within a single run.

### Parser Contract Versioning

Each parser declares:

- a **parser key** (stable string identifier, e.g. `pdf-text-extractor`)
- a **parser contract version** (semver-style string, e.g. `1.0.0`)
- a **parser contract hash** (SHA-256 of the contract definition, for audit traceability)

Changing the parser contract version creates a new parser contract generation. Re-running the same parser version on the same source hash with the same content should produce identical fragment candidates.

### Source Hash Binding

Every parser run binds to:

- the `ImportedSourceId` of the parsed source
- the content hash of the source bytes
- the hash algorithm and version

This binding ensures that parser output is traceable to exact input bytes and cannot be silently substituted.

### Reproducibility Expectations

A deterministic parser run is expected to produce identical fragment candidates when:

- the same parser key and contract version are used
- the same source content hash is provided
- the same source bytes are available for parsing

Non-deterministic parsers (e.g., those involving AI or OCR) are explicitly out of scope for this foundation. This foundation only defines the durable identity and lifecycle model that deterministic parsers produce.

### Non-Authoritative Boundary

Fragment candidates are explicitly non-authoritative. They:

- cannot become `KnowledgeDocumentRevision` directly
- cannot become `KnowledgeAssertion`, `Requirement`, or `Citation` directly
- cannot become report content directly
- require explicit human review and Application-level promotion to become authoritative

Promotion from fragment candidate to authoritative knowledge is a later workflow concern.

## Domain Types

### Strongly Typed IDs

- `ParserRunId` — identifies a single deterministic parser execution against one imported source
- `FragmentCandidateId` — identifies a single non-authoritative fragment output within a parser run

### ParserRun

An aggregate that tracks the lifecycle of one deterministic parser execution.

Properties:

- `Id` (ParserRunId)
- `ProjectId`
- `ImportedSourceId` — the source being parsed
- `ParserKey` — stable parser identifier
- `ParserContractVersion` — semver-style version string
- `ParserContractHash` — SHA-256 of the parser contract definition
- `SourceContentHash` — hash of the input source bytes
- `ParserVersion` — version of the parser implementation (distinct from contract version)
- `State` (Created, Running, Completed, Failed, Cancelled)
- `StartedAtUtc`
- `CompletedAtUtc`
- `FailureReason` — populated on Failed/Cancelled
- `CreatedAtUtc`
- `CreatedBy`
- `AuditTrail` — append-only lifecycle events

Lifecycle:

```text
Created
-> Running
-> Completed

Terminal:
Failed
Cancelled
```

Invariants:

- Cannot transition from a terminal state
- Running requires Created state
- Completed requires Running state
- Failed requires non-terminal state
- Cancelled requires non-terminal state
- FailureReason must be provided for Failed and Cancelled states

### FragmentCandidate

An aggregate representing a single non-authoritative parsed fragment.

Properties:

- `Id` (FragmentCandidateId)
- `ParserRunId` — the parser run that produced this candidate
- `ProjectId`
- `ImportedSourceId`
- `SourceContentHash` — hash of the parsed source bytes
- `LocatorType` (FragmentLocatorType enum)
- `LocatorValue` — raw locator string before normalization
- `NormalizedLocator` — the normalized locator value used for identity derivation
- `Ordinal` — position within the parser output sequence (1-based, unique within a run)
- `ContentKind` — classifier for the fragment content type (e.g., `PlainText`, `Table`, `Figure`)
- `ExtractedText` — the extracted text content (bounded)
- `TextLength` — character count of extracted text
- `ConfidenceBand` — deterministic quality signal (High, Medium, Low, Unknown)
- `IdentityKey` — the derived deterministic identity string
- `IdentityKeyHash` — SHA-256 of the identity key for indexed lookup
- `CreatedAtUtc`
- `AuditTrail` — append-only lifecycle events

Invariants:

- Locator must be non-empty
- Ordinal must be positive and unique within the parser run
- ExtractedText must be non-empty and bounded (max 100,000 characters)
- SourceContentHash must match the parser run's source content hash
- IdentityKey is derived deterministically and must match the expected derivation
- Duplicate identity keys within the same parser run are rejected
- FragmentCandidate cannot be mutated after creation (immutable content)

### FragmentLocatorType

```text
WholeDocument = 0
Page = 1
Paragraph = 2
LineRange = 3
StructuralPath = 4
```

### FragmentLocator (Value Object)

An immutable value object that normalizes locator input.

Properties:

- `LocatorType` (FragmentLocatorType)
- `RawValue` — the original locator string as produced by the parser
- `NormalizedValue` — the normalized form used for identity derivation

Normalization rules:

- `WholeDocument`: normalized to empty string
- `Page`: normalized to numeric string (e.g., `"3"` for page 3)
- `Paragraph`: normalized to `"page:paragraph"` format
- `LineRange`: normalized to `"startLine-endLine"` format
- `StructuralPath`: normalized to forward-slash-separated path (e.g., `"section/3.1/paragraph/2"`)

Normalization strips leading/trailing whitespace, lowercases where appropriate, and ensures consistent representation for identity derivation.

### ConfidenceBand

```text
High = 0
Medium = 1
Low = 2
Unknown = 3
```

Confidence is a deterministic signal produced by the parser contract. It is not AI-derived and does not require human judgment. It reflects the parser's deterministic assessment of output quality.

### ContentKind

```text
PlainText = 0
Table = 1
Figure = 2
Code = 3
Metadata = 4
```

ContentKind is a parser-declared classifier for the type of extracted content.

## Audit Events

ParserRun emits:

- `ParserRunCreated` — when the run is instantiated
- `ParserRunStarted` — when the run transitions to Running
- `ParserRunCompleted` — when the run transitions to Completed
- `ParserRunFailed` — when the run transitions to Failed
- `ParserRunCancelled` — when the run transitions to Cancelled

FragmentCandidate emits:

- `FragmentCandidateGenerated` — when the candidate is created

Audit events follow the same `AuditableEntity` base class pattern as all other Domain aggregates. SubjectType and SubjectId are declared as `const string` constants.

## Relationship To Existing Types

- `ParserRun` references `ImportedSourceId` (the source being parsed)
- `ParserRun` references `ProjectId` (project scope)
- `FragmentCandidate` references `ParserRunId`, `ImportedSourceId`, and `ProjectId`
- `FragmentCandidate` does NOT reference `KnowledgeDocument`, `KnowledgeRevision`, `KnowledgeCitation`, or any authoritative type
- `FragmentCandidate` is a peer to the existing `DocumentCandidate` type — both are non-authoritative outputs of the Document Engine

## Non-Responsibilities

This specification intentionally does not define:

- parser adapter implementation
- OCR integration
- layout reconstruction
- semantic interpretation
- AI-powered extraction
- authoritative knowledge promotion
- cross-revision fragment equivalence
- fragment-to-citation promotion
- fragment retrieval ranking
- vector embeddings
- database persistence schemas
- EF Core migrations
- UI or presentation contracts

## Review Checks

This specification is successful if it helps future work answer:

- How is a fragment candidate's identity deterministically derived?
- What makes a parser run reproducible?
- How are duplicate candidates within a run prevented?
- What prevents fragment candidates from becoming authoritative knowledge?
- How does fragment identity relate to source revision and parser contract?
