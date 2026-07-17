# Architecture Vision

Purpose: Define the long-term architecture of SPINbuster as the platform constitution for future implementation slices.

## Platform Definition

SPINbuster is an offline-first engineering knowledge platform whose authoritative engineering state is deterministic, traceable, governed, and provider-independent.

The platform exists to:

- preserve immutable field evidence and imported source material
- construct authoritative project knowledge with provenance
- evaluate deterministic engineering rules
- assemble governed context for downstream workflows
- permit AI assistance only as an advisory, replaceable, non-authoritative capability
- support long-lived local operation before synchronization, cloud execution, or presentation expansion

## Two Complementary Architectural Views

SPINbuster uses two complementary architectural views.

These views answer different questions and must not be confused with one another.

### Layered Architecture

The layered architecture governs code dependencies, project references, and implementation direction.

Layers:

- Domain
- Application
- Outer Adapters
- Presentation And Composition Hosts

Meaning:

- Domain owns business truth, invariants, and lifecycle rules.
- Application owns orchestration, workflows, and provider-neutral contracts.
- Outer Adapters implement Application ports and isolate concrete external behavior.
- Presentation and Composition Hosts consume Application workflows and do not own business logic.

Current adapter categories and projects include:

- Infrastructure for database persistence, migrations, and repository implementations
- Documents for storage and document-processing implementations
- AI for model-provider implementations
- Reporting for rendering and export implementations
- future synchronization adapters for exchange and transport implementations

Current presentation and composition hosts include:

- Desktop
- Server

This view governs project reference direction.
It is the dependency model.

### Capability Architecture

The capability architecture describes what the platform does and which engine owns each major responsibility.

It is not a dependency graph.
It is an ownership model.

Capability engines:

- Storage Engine
- Document Engine
- Parsing Engine
- Fragment Engine
- Knowledge Engine
- Rule Engine
- Retrieval Engine
- Context Engine
- AI Proposal Layer
- AI Execution Engine
- Provider Adapters
- Reporting Engine
- Synchronization Engine
- Presentation

This view answers:

- which engine owns a capability
- where authoritative state belongs
- which engine is allowed to evolve a given concept
- how future slices should avoid overlap and architectural drift

## Relationship Between The Two Views

- Layered Architecture controls implementation dependency direction.
- Capability Architecture controls functional ownership.
- A single capability may involve multiple layers.
- A single layer may host implementations for multiple engines.
- Capability diagrams must never be interpreted as code dependency diagrams.

Example:

- The Knowledge Engine is a capability owner.
- Its Domain invariants live in Domain.
- Its orchestration lives in Application.
- Its persistence lives in Infrastructure.
- Its screens or host flows live in Presentation.

## Architectural Principles

### Offline-First

- Local operation is primary.
- Correctness must not depend on constant connectivity.
- Synchronization extends local correctness rather than replacing it.

### Authoritative Engineering Knowledge

- Authoritative engineering truth belongs in governed knowledge and reporting workflows.
- Raw evidence, drafts, fragments, rules, and AI proposals are not automatically authoritative.

### Immutable Evidence

- Raw observations, imported sources, and evidence attachments are preserved as historical records.
- Higher-order conclusions do not replace raw evidence.

### Deterministic Business Rules

- Engineering rules remain deterministic and explicit.
- AI may assist around rules, but does not replace rule ownership.

### AI Proposals Are Never Authoritative

- AI may propose, summarize, classify, rank, or draft.
- AI may not independently create engineering truth.

### Human Approval Governs Promotion Into Authority

- AI, parsing, and extraction produce non-authoritative proposals and candidates.
- Governed Application workflows may promote validated content into authoritative project state only through an authorized and auditable human decision.
- Human review does not bypass provenance, validation, scope consistency, lifecycle rules, or project boundaries.
- Approval is a first-class operation, not an incidental side effect.

### Provider Independence

- Local and cloud providers are interchangeable implementation choices behind stable platform contracts.
- Core workflows must not depend on a single model vendor, storage engine, parser, or sync transport.

### Replaceable Storage

- Storage providers are implementation details.
- Authoritative platform behavior must survive storage replacement when contracts are preserved.

### Replaceable AI Providers

- AI execution is provider-swappable.
- Proposal semantics, validation, and audit behavior remain platform-owned.

### Replaceable Presentation

- Presentation surfaces consume Application workflows.
- Desktop, Server, MAUI, web, or mobile clients must not become business-logic owners.

### Bounded Contexts

- Each engine owns a bounded capability area.
- Ownership overlap should be minimized and explicit when collaboration is required.

### Traceability

- Every material engineering conclusion should be explainable from workflow, knowledge, rule, report, and audit state.

### Provenance

- Knowledge, reports, and proposals must retain source lineage.
- Provenance is retained even after supersession or promotion.

### Versioned Authority

- Authority evolves by explicit versioning and supersession.
- Silent overwrite is prohibited.

### Long-Lived Platform Evolution

- The architecture must support years of gradual evolution without collapsing into host-specific or provider-specific coupling.
- New engines and clients should extend the platform constitution rather than bypass it.

## Engine Definitions

### Storage Engine

Purpose:
Own durable byte and persistence-medium concerns for evidence and imported content.

Responsibilities:

- immutable object storage
- storage identity and object addressing
- durability behavior
- corruption detection and availability reporting
- storage-provider replacement boundary

Explicit non-responsibilities:

- parsing content
- promoting knowledge
- approving reports
- constructing AI context

Authoritative objects:

- storage object identity
- immutable object keys
- durability and availability metadata

Primary collaborators:

- Document Engine
- Infrastructure
- Provider Adapters

Future evolution:

- alternate local providers
- encrypted-at-rest strategies
- cloud object storage adapters
- orphan reconciliation support

### Document Engine

Purpose:
Own document import workflows and non-authoritative processing lifecycle around imported sources.

Responsibilities:

- import sessions
- source identity
- duplicate detection
- imported-source lifecycle
- processing-attempt lifecycle
- candidate review state for document-derived outputs

Explicit non-responsibilities:

- authoritative knowledge creation
- deterministic rule evaluation
- final report authority
- direct AI authority

Authoritative objects:

- imported document sources
- import sessions
- processing attempts
- non-authoritative document candidates

Primary collaborators:

- Storage Engine
- Parsing Engine
- Fragment Engine
- Application

Future evolution:

- batch ingestion workflows
- parser orchestration
- multi-stage review gates
- document-family specific pipelines

### Parsing Engine

Purpose:
Turn durable source bytes into normalized, non-authoritative structural parse outputs.

Responsibilities:

- parser orchestration
- parser-provider execution
- parse normalization
- parse failure and abstention handling
- parser capability selection

Explicit non-responsibilities:

- authoritative fragment promotion
- knowledge promotion
- report approval
- AI proposal execution

Authoritative objects:

- normalized parse outputs
- parse-attempt metadata
- parser capability and outcome state

Primary collaborators:

- Document Engine
- Fragment Engine
- Provider Adapters

Future evolution:

- OCR integration
- multimodal parsing
- discipline-specific parsers
- parsing confidence instrumentation

### Fragment Engine

Purpose:
Own document-derived fragment candidates and their provenance-safe review lifecycle.

Responsibilities:

- fragment identity
- fragment candidate persistence
- fragment locators
- fragment review state
- fragment provenance to immutable sources

Explicit non-responsibilities:

- final knowledge authority
- deterministic rule evaluation
- report issuance

Authoritative objects:

- fragment candidates
- fragment locators
- reviewed fragment metadata

Primary collaborators:

- Parsing Engine
- Document Engine
- Knowledge Engine

Future evolution:

- assertion candidates
- citation candidates
- relationship candidates
- promotion workflows

### Knowledge Engine

Purpose:
Own authoritative engineering knowledge and its revisioned, project-scoped graph.

Responsibilities:

- knowledge documents
- knowledge revisions
- relationships
- contradictions
- citations
- supersession and versioned authority

Explicit non-responsibilities:

- raw byte storage
- parser execution
- AI provider execution
- presentation behavior

Authoritative objects:

- knowledge documents
- knowledge revisions
- knowledge relationships
- knowledge citations

Primary collaborators:

- Fragment Engine
- Rule Engine
- Retrieval Engine
- Reporting Engine

Future evolution:

- richer contradiction workflows
- assertion promotion
- conflict review queues
- cross-project sharing policies

### Rule Engine

Purpose:
Evaluate authoritative knowledge and contextual facts through deterministic engineering rules.

Responsibilities:

- rule definitions
- rule execution
- rule result persistence
- deterministic evidence binding
- rule-result lifecycle and review semantics

Explicit non-responsibilities:

- inventing authoritative knowledge
- provider-specific AI execution
- report issuance

Authoritative objects:

- rule definitions
- rule evaluations
- rule findings and supporting evidence links

Primary collaborators:

- Knowledge Engine
- Retrieval Engine
- Reporting Engine

Future evolution:

- discipline-specific rule sets
- configurable rule packs
- remediation workflows
- scheduling and watch conditions

### Retrieval Engine

Purpose:
Provide bounded, explainable access to relevant knowledge, fragments, evidence, and rule outputs.

Responsibilities:

- deterministic retrieval
- semantic retrieval supplementation
- ranking and bounded traversal
- retrieval result shaping with provenance

Explicit non-responsibilities:

- authoritative promotion
- AI proposal generation
- presentation rendering

Authoritative objects:

- retrieval queries
- retrieval result sets
- ranking and match metadata

Primary collaborators:

- Knowledge Engine
- Fragment Engine
- Rule Engine
- Context Engine

Future evolution:

- semantic ranking
- hybrid deterministic plus semantic retrieval
- corpus-scoped retrieval profiles
- retrieval evaluation harnesses

### Context Engine

Purpose:
Assemble governed, replayable context manifests for downstream engines and workflows.

Responsibilities:

- context manifest construction
- inclusion policy
- scope and truncation rules
- manifest hashing and auditability
- replay-safe context definition

Explicit non-responsibilities:

- provider execution
- authoritative promotion
- storage implementation

Authoritative objects:

- context manifests
- manifest source entries
- inclusion rationale

Primary collaborators:

- Retrieval Engine
- Knowledge Engine
- Rule Engine
- AI Proposal Layer
- Reporting Engine

Future evolution:

- multiple context profiles
- policy-controlled context budgets
- scenario-specific manifests for sync, review, and reporting

### AI Proposal Layer

Purpose:
Own advisory AI proposal records as governed, non-authoritative platform artifacts.

Responsibilities:

- proposal identity and lifecycle
- proposal provenance
- proposal review disposition
- proposal linkage to context, sources, provider, and output schema

Explicit non-responsibilities:

- authoritative report mutation
- final engineering approval
- direct provider execution transport

Authoritative objects:

- AI proposals
- proposal review state
- proposal provenance metadata

Primary collaborators:

- Context Engine
- AI Execution Engine
- Reporting Engine

Future evolution:

- multiple proposal types
- proposal comparison workflows
- acceptance-to-authority orchestration

### AI Execution Engine

Purpose:
Execute governed AI workloads while preserving provider independence and auditability.

Responsibilities:

- model-run lifecycle
- attempt lifecycle
- execution auditability
- structured output validation routing
- retry and failure handling

Explicit non-responsibilities:

- declaring authority
- embedding provider SDK types into core layers
- report approval

Authoritative objects:

- model runs
- model run attempts
- execution outcomes

Primary collaborators:

- Context Engine
- AI Proposal Layer
- Provider Adapters

Future evolution:

- local and cloud providers
- execution policies
- advanced retry and recovery
- evaluation and replay tooling

### Provider Adapters

Purpose:
Bridge replaceable external capabilities into stable platform contracts.

Responsibilities:

- provider-specific translation
- transport and SDK isolation
- capability discovery
- provider failure normalization

Explicit non-responsibilities:

- platform authority decisions
- business orchestration
- presentation logic

Authoritative objects:

- provider configuration
- provider capability mappings
- normalized provider responses

Primary collaborators:

- Storage Engine
- Parsing Engine
- AI Execution Engine
- Synchronization Engine

Future evolution:

- additional local providers
- cloud providers
- parser, OCR, and sync adapters
- compatibility matrices

### Reporting Engine

Purpose:
Own authoritative reporting and report revision workflows.

Responsibilities:

- report drafts
- authoritative report revisions
- report provenance
- issuance and export boundaries

Explicit non-responsibilities:

- raw evidence storage
- parser execution
- AI authority

Authoritative objects:

- reports
- report revisions
- report sections
- report provenance links

Primary collaborators:

- Knowledge Engine
- Rule Engine
- Context Engine
- AI Proposal Layer

Future evolution:

- revision chains
- exports
- publication workflows
- acceptance from reviewed AI proposals

### Synchronization Engine

Purpose:
Extend offline-first local correctness into safe exchange across nodes and environments.

Responsibilities:

- sync state
- exchange protocols
- conflict detection
- conflict resolution workflows
- replay and idempotency discipline

Explicit non-responsibilities:

- local business truth ownership
- direct presentation behavior
- rule evaluation logic

Authoritative objects:

- sync operations
- sync cursors
- conflict records
- reconciliation state

Primary collaborators:

- Knowledge Engine
- Reporting Engine
- Provider Adapters
- Storage Engine

Future evolution:

- peer synchronization
- cloud relay
- multi-device consistency
- recovery tooling

### Presentation

Purpose:
Deliver user-facing workflows through stable Application contracts.

Responsibilities:

- user interaction
- workflow presentation
- composition roots
- user input capture
- platform-specific experience concerns

Explicit non-responsibilities:

- business truth ownership
- direct persistence logic
- authority decisions
- provider SDK orchestration

Authoritative objects:

- none; Presentation consumes rather than owns authoritative engineering state

Primary collaborators:

- Application
- Reporting Engine
- Document Engine
- Knowledge Engine

Future evolution:

- desktop clients
- mobile clients
- administrative consoles
- server-facing clients

## Engine Collaboration Rules

- Evidence enters through Storage and Document workflows before higher-order interpretation.
- Parsing and Fragment workflows remain non-authoritative until human-governed promotion occurs.
- Knowledge owns authoritative engineering truth.
- Rule Engine evaluates authoritative knowledge rather than replacing it.
- Retrieval and Context assemble governed inputs for downstream decisions.
- AI Proposal and AI Execution remain advisory and replaceable.
- Reporting converts governed knowledge and accepted workflow outcomes into authoritative reports.
- Synchronization distributes already-governed state; it does not redefine authority.
- Presentation consumes Application workflows and must not become an authority owner.

## Long-Term Platform Direction

SPINbuster is expected to evolve for years without abandoning these boundaries.

The intended long-term path is:

1. durable local evidence and document understanding
2. governed knowledge promotion
3. deterministic rule evaluation
4. bounded retrieval and context assembly
5. interchangeable local and cloud AI execution
6. authoritative reporting from governed workflows
7. synchronization across nodes
8. expanded presentation surfaces and commercial deployment

Every future slice should reinforce this constitution rather than bypass it.
