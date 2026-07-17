# Document Engine

## Purpose

`spec/documents/` defines the boundary and first durable foundation for the Document Engine.

The Document Engine will convert binary source material into non-authoritative candidates that can later be reviewed and promoted through Knowledge Engine workflows.

## Current boundary

The repository now contains the released durable Document Engine foundation and the first executable Document Engine review candidate.

Current foundation responsibilities include:

- binary import
- immutable import records
- MIME and content validation
- stable file identity
- content hashing
- exact duplicate detection
- import-session persistence
- processing-attempt persistence
- non-authoritative candidate persistence

Future responsibilities still include:

- parser and OCR adapter orchestration
- fragment candidate production
- citation candidate production
- relationship and assertion candidate production

The Document Engine does not own authoritative engineering truth.

Authoritative promotion remains an Application and Knowledge Engine workflow decision.

## Executable review-candidate status

The active review candidate validates:

- multi-source batch import
- exact duplicate handling
- privacy-safe cross-project duplicate reporting
- deterministic processing-attempt and candidate persistence
- explicit review disposition for non-authoritative candidates
- Application-only Desktop composition
- authority isolation from Knowledge, Report, and AI records

See `docs/decisions/status/DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-PROTOTYPE-REVIEW.md` for the current prototype assessment and next-package recommendation.

## Current non-goals

- authoritative knowledge creation without review
- direct repository mutation from parser adapters
- live AI extraction pipelines
- OCR provider selection
- object-store selection
- vector database selection

See `spec/documents/document-engine-boundary.md` for the conceptual pipeline and `spec/documents/document-engine-foundation.md` for the current implementation boundary.
