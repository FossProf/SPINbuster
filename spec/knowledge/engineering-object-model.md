# Engineering Object Model

Status: Review Candidate
Baseline: `ENGINEERING-KNOWLEDGE-MODEL-0.1-RC`

## Purpose

This specification defines the core durable nouns inside SPINbuster.

It answers the question:

What are the actual long-lived objects that the system reasons about?

These objects form the shared language across:

- Domain modeling
- Document processing
- Knowledge promotion
- deterministic rules
- report assembly
- AI context construction
- audit and governance

Workflow is important, but workflow is not the object model. This specification focuses on the durable engineering objects that workflows act upon.

## Governing Idea

Documents
-> Knowledge
-> Rules
-> Reports
-> AI

All of those layers should converge on the same engineering object language instead of inventing parallel vocabularies.

## Object Categories

The durable object model divides into:

- project and execution objects
- engineering knowledge objects
- evidence and observation objects
- rule and evaluation objects
- reporting and proposal objects
- graph and provenance objects
- governance and conflict objects

## Core Durable Objects

### Project

The stable top-level scope for all authoritative work.

Owns or scopes:

- inspection sessions
- knowledge documents
- evidence
- reports
- proposals
- rules and evaluations
- citations
- relationships
- conflicts
- audit history

### InspectionSession

A bounded field-work session within a project.

Represents:

- when an inspection occurred
- who performed it
- what raw field material was captured during the session

### KnowledgeDocument

The stable identity of an engineering record across revisions.

Examples:

- specification
- drawing
- RFI
- submittal
- bulletin
- change order
- daily report

### KnowledgeRevision

An immutable historical state of a `KnowledgeDocument`.

Represents:

- revision label
- content hash
- verification state
- authority metadata
- supersession chain

### KnowledgeFragment

An addressable unit inside a specific `KnowledgeRevision`.

Examples:

- specification section
- drawing detail
- table row
- figure callout
- paragraph
- RFI response paragraph

Fragments are revision-bound and exist to support durable citation, extraction, and downstream knowledge promotion.

### Observation

A recorded human-originated statement of what was observed, measured, or encountered in the field.

Observations preserve:

- original wording
- observer context
- observed time
- optional supporting evidence

### FieldNote

A specific raw observation captured during an `InspectionSession`.

This remains part of the released Domain model and should continue to preserve raw text immutably.

### EvidenceAttachment

A raw supporting artifact linked to field or engineering work.

Examples:

- photograph
- attachment
- imported binary
- markup image
- instrument output

Raw evidence remains distinct from later interpretation.

### EvidenceInterpretation

A non-raw analysis of an `EvidenceAttachment`.

It may be produced by a human or AI-assisted workflow, but it is not the source artifact itself.

The current released boundary remains conservative:

- one interpretation may be attached
- existing interpretation cannot be silently replaced
- versioned interpretation history remains deferred under `EDR-DOM-001`

### Requirement

An authoritative statement describing what must, shall, or should occur.

Requirements are typically derived from source material such as:

- specifications
- drawings
- approved RFIs
- code references
- owner criteria

### Constraint

A limiting condition that narrows valid project states or actions.

Examples:

- maximum spacing
- minimum cure time
- approved material family
- environmental operating limit

### Rule

A deterministic executable expression derived from authoritative requirements or approved policy.

Rules are not free-form knowledge. They are operationalized logic.

### RuleEvaluation

A durable record of a rule being evaluated against project inputs.

Represents:

- evaluated rule version
- inputs used
- outcome
- severity
- provenance
- audit trail

### Report

An authoritative project report artifact created through explicit application workflows.

Reports are durable records, not AI chat transcripts.

### ReportRevision

A versioned authoritative state of a report.

The current released implementation supports authoritative draft creation and revision tracking at the report level. Future report revision workflows should continue to preserve immutable historical meaning.

### Proposal

A non-authoritative suggested artifact awaiting validation or human review.

Examples:

- AI report draft proposal
- candidate relationship
- proposed assertion
- proposed requirement extraction

### Citation

A durable pointer from one object to the specific source revision or fragment that supports it.

Citation binds downstream reasoning back to historical source material.

### Relationship

A typed semantic edge connecting two engineering objects.

Examples:

- `Clarifies`
- `References`
- `Supersedes`
- `DerivedFrom`
- `Supports`
- `Contradicts`

### Conflict

A first-class record of materially incompatible project knowledge.

Examples:

- conflicting current revisions
- contradictory requirements
- field observation inconsistent with governing source material

### AuditEvent

An append-only domain or application record of significant lifecycle activity.

Audit history is not just logging. It is part of the durable traceability model of SPINbuster.

### SaveTransaction

A durable transaction-oriented object used to model explicit save preparation and lifecycle state, rather than treating persistence success as an invisible side effect.

### ApplicationUser

An application-level identity concept used for review, acceptance, authorship, and accountability workflows.

It should remain typed and application-facing rather than tied to HTTP claims or provider-specific identity types.

## Common Distinctions

### Durable object versus workflow

Examples of durable objects:

- `KnowledgeDocument`
- `KnowledgeRevision`
- `Observation`
- `EvidenceAttachment`
- `Requirement`
- `Rule`
- `Report`
- `Proposal`

Examples of workflow:

- import document
- parse revision
- run rule
- assemble AI context
- request proposal
- accept proposal
- create report revision

### Source versus interpretation

SPINbuster must keep separate:

- raw source material
- extracted candidates
- validated knowledge
- deterministic evaluations
- advisory proposals

### Authority versus provenance

An object may be well-cited without being authoritative.

An object may be authoritative for one question and not another.

Authority and provenance are related, but they are not interchangeable.

### Object versus representation

The durable object is not the same thing as:

- a database row
- a file on disk
- a JSON payload
- a UI card
- an AI prompt fragment

Representations may change. Object meaning should remain stable.

## Initial Shared Vocabulary

The repository should increasingly standardize on this noun set:

- Project
- InspectionSession
- FieldNote
- Observation
- EvidenceAttachment
- EvidenceInterpretation
- KnowledgeDocument
- KnowledgeRevision
- KnowledgeFragment
- Requirement
- Constraint
- Rule
- RuleEvaluation
- Report
- ReportRevision
- Proposal
- Citation
- Relationship
- Conflict
- AuditEvent
- SaveTransaction

New subsystems should prefer extending this vocabulary over creating parallel terms for the same durable idea.

## Relationship To Other Specifications

- `spec/knowledge/engineering-knowledge-model.md` defines the deeper conceptual semantics, authority model, provenance chain, and subsystem boundaries.
- `spec/documents/document-engine-boundary.md` defines how binary sources and processing attempts may produce non-authoritative candidates around these objects.
- `spec/rules/rule-engine-boundary.md` defines how deterministic rule execution consumes authoritative knowledge and produces durable evaluation records.
- `spec/ai/` defines how AI may consume governed context and produce advisory proposals without becoming an authoritative writer.

## Review Checks

This specification is successful if it helps future work answer:

- What are the durable nouns in SPINbuster?
- Which concepts are source material versus interpretation?
- Which concepts are authoritative versus advisory?
- Which concepts belong to knowledge, rules, reports, and AI collectively?
- Which names should new implementations reuse instead of redefining?
