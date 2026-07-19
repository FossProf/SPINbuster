# Document → Fragment → Knowledge Architecture

**Status:** Accepted Guidance
**Applies To:** Architecture Vision 2.0 and later

---

# Purpose

This document explains the conceptual pipeline that transforms raw engineering documents into authoritative engineering knowledge.

The purpose of the pipeline is to ensure that every engineering conclusion within SPINbuster can be traced back to governed source material while maintaining strict separation between deterministic processing, AI assistance, and human engineering judgment.

The pipeline is intentionally layered.

Each stage has a single responsibility.

Each stage produces artifacts that become inputs to the next stage.

No stage is permitted to bypass another.

---

# High-Level Pipeline

```text
Engineering Documents
        │
        ▼
Document Engine
        │
        ▼
Parser Engine
        │
        ▼
Fragment Engine
        │
        ▼
Knowledge Engine
        │
        ▼
Rule Engine
        │
        ▼
Report Engine
        │
        ▼
Human Review
        │
        ▼
Authoritative Engineering Record
```

---

# Stage 1 — Document Engine

## Responsibility

Manage physical engineering documents.

The Document Engine is responsible for:

* importing documents
* immutable storage
* content hashing
* duplicate detection
* processing attempts
* document metadata
* provenance of source files

It is **not** responsible for engineering interpretation.

### Inputs

* PDF
* Word
* Images
* Text
* CAD exports
* Specifications
* RFIs
* Submittals
* Drawings

### Outputs

* Imported Sources
* Immutable Storage Objects
* Processing Attempts

---

# Stage 2 — Parser Engine

## Responsibility

Transform governed source material into deterministic structural information.

The Parser Engine never decides engineering meaning.

It answers questions such as:

* What pages exist?
* What paragraphs exist?
* What tables exist?
* What line ranges exist?
* What drawing regions exist?

Parser output must be deterministic.

Running the same parser against the same governed source shall always produce identical parser output.

### Outputs

* Parser Runs
* Fragment Candidates

---

# Stage 3 — Fragment Engine

## Responsibility

Represent structural portions of a document.

Fragments are revision-bound.

Fragments are locator-addressable.

Fragments are **not authoritative engineering knowledge.**

Examples include:

* Paragraph
* Section
* Line Range
* Table
* Figure
* Drawing Region

Each fragment maintains:

* source revision
* parser version
* normalized locator
* provenance
* deterministic identity

Fragments are reusable structural units.

They are not engineering conclusions.

---

# Stage 4 — Knowledge Engine

## Responsibility

Convert governed evidence into engineering knowledge.

Knowledge is promoted only through governed workflows.

Knowledge objects include:

* observations
* requirements
* constraints
* assertions
* interpretations
* citations
* relationships

Knowledge always references its supporting fragments.

No knowledge object may exist without provenance.

---

# Stage 5 — Rule Engine

## Responsibility

Apply deterministic engineering rules.

Rules evaluate knowledge.

Rules never invent knowledge.

Rules may produce:

* validation findings
* conflicts
* missing information
* consistency checks
* applicability results

Rules remain deterministic.

AI recommendations are not rules.

---

# Stage 6 — Report Engine

## Responsibility

Assemble engineering reports.

Reports consume:

* knowledge
* rules
* citations
* observations
* evidence

Reports never read raw documents directly unless explicitly requested.

The report is a presentation layer over authoritative engineering knowledge.

---

# AI's Role

AI operates alongside the pipeline.

It does not replace it.

AI may assist with:

* summarization
* extraction proposals
* draft generation
* organization
* semantic suggestions

AI outputs remain proposals until accepted by an authorized human.

AI never creates authoritative engineering truth.

---

# Human Authority

Human approval is the only mechanism that promotes information into authoritative engineering records.

No parser

No deterministic rule

No AI model

No external service

may bypass this requirement.

Authority requires:

* provenance
* validation
* scope compliance
* audit trail
* explicit human acceptance

---

# Architectural Principles

The pipeline is governed by the following principles:

1. Every stage has a single responsibility.
2. Every output is traceable to governed inputs.
3. Authority always flows upward, never downward.
4. Documents become fragments.
5. Fragments support knowledge.
6. Knowledge feeds rules.
7. Rules inform reports.
8. Reports communicate engineering decisions.
9. AI assists every stage but authorizes none.
10. Every engineering conclusion must remain explainable years after it was created.

---

# Future Expansion

The pipeline intentionally supports future capabilities without altering its architecture.

Examples include:

* PDF parsers
* OCR parsers
* CAD parsers
* Blueprint parsers
* Semantic retrieval
* Embedding search
* Multi-model AI providers
* Cloud synchronization
* Multi-user collaboration

These capabilities extend existing stages.

They do not replace them.

---

# Summary

SPINbuster is not fundamentally a report-generation application.

It is an engineering knowledge platform.

Reports are one product of that platform.

The enduring asset is governed engineering knowledge derived from authoritative source documents through deterministic processing, verified provenance, and human engineering judgment.
