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
- citation candidate production
- relationship and assertion candidate production

The Document Engine does not own authoritative engineering truth.

Authoritative promotion remains an Application and Knowledge Engine workflow decision.

## Parsing and fragment foundation

The active review candidate `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC` establishes:

- deterministic parser-run identity and lifecycle
- non-authoritative fragment-candidate identity derived from source revision + parser contract + normalized locator
- immutable locator value objects for whole-document, page, paragraph/line range, and structural path
- source-hash binding for reproducibility verification
- duplicate candidate rejection within a parser run
- append-only audit events for parser-run and fragment-candidate lifecycle

See `spec/documents/parsing-and-fragment-foundation.md` for the complete specification.

## Executable review-candidate status

The active review candidate `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC` validates:

- deterministic parser-run identity and lifecycle through the Domain model
- non-authoritative fragment-candidate identity derived from source + parser contract + normalized locator
- immutable locator value objects for whole-document, page, paragraph/line range, and structural path
- deterministic identity derivation from governed inputs
- source-hash binding for reproducibility verification
- duplicate candidate rejection within a parser run
- append-only audit events for parser-run and fragment-candidate lifecycle
- deterministic PlainTextDocument parser adapter with DI registration
- SQLite persistence of parser runs and fragment candidates
- idempotent replay across provider recreation
- unsupported media, cancelled parse, and malformed output failure handling
- parser version coexistence with historical candidate preservation
- authority isolation from Knowledge, Report, and AI records
- Application-only Desktop composition through parsing workflow runner

See `docs/decisions/status/PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC-PROTOTYPE-REVIEW.md` for the current prototype assessment and next-package recommendation.

## Current non-goals

- authoritative knowledge creation without review
- direct repository mutation from parser adapters
- live AI extraction pipelines
- OCR provider selection
- object-store selection
- vector database selection

See `spec/documents/document-engine-boundary.md` for the conceptual pipeline and `spec/documents/document-engine-foundation.md` for the current implementation boundary.
