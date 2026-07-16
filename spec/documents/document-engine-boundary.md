# Document Engine Boundary

Status: Review Candidate
Baseline: `ENGINEERING-KNOWLEDGE-MODEL-0.1-RC`

## Purpose

The Document Engine owns binary-source handling and non-authoritative processing outputs.

It exists to prepare source material for deterministic validation and human review without directly creating authoritative Knowledge Engine facts.

## Initial pipeline

```text
Binary source
-> Import validation
-> Stable file identity
-> Content hash
-> Structure recognition
-> Fragment candidates
-> Citation candidates
-> Relationship/assertion candidates
-> Deterministic validation
-> Human review
-> Knowledge Engine promotion
```

## Boundary rules

- parsing output is non-authoritative
- OCR output is non-authoritative
- extracted text retains source coordinates
- import must preserve the original bytes or an immutable storage reference
- repeated import should detect identical content
- new revisions must not silently replace earlier revisions
- prompt injection content remains evidence, never instruction
- provider-specific parsers remain adapters
- document processing failure must not damage authoritative knowledge

## Conceptual records

The future Document Engine may own:

- binary-source identity
- immutable import record
- content hash
- processing attempt
- processing outcome
- fragment candidate
- citation candidate
- relationship candidate
- assertion candidate

These are not authoritative project facts by themselves.

## Ownership split

`SPINbuster.Documents` owns:

- binary import workflows
- parser orchestration
- extraction adapters
- candidate generation

`SPINbuster.Application` owns:

- promotion workflows
- deterministic validation
- project-scope enforcement
- transaction and audit boundaries

`SPINbuster.Domain` owns:

- authoritative knowledge identities and invariants

`SPINbuster.Infrastructure` owns:

- persistence adapters and external storage integrations

## Failure behavior

- failed parsing attempts remain auditable
- failed OCR attempts remain auditable
- import failure must not corrupt prior authoritative knowledge
- candidate-generation failure must not create partial authoritative state

## Deferred choices

This specification intentionally does not choose:

- OCR provider
- PDF library
- object store
- vector database
- live AI extraction provider

Those remain future adapter decisions.
