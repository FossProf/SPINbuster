# Document Engine

## Purpose

`spec/documents/` defines the boundary for the future Document Engine.

The Document Engine will convert binary source material into non-authoritative candidates that can later be reviewed and promoted through Knowledge Engine workflows.

## Current boundary

The Document Engine is not yet implemented.

Its future responsibilities include:

- binary import
- immutable import records
- MIME and content validation
- stable file identity
- content hashing
- parser and OCR adapter orchestration
- fragment candidate production
- citation candidate production
- relationship and assertion candidate production

The Document Engine does not own authoritative engineering truth.

Authoritative promotion remains an Application and Knowledge Engine workflow decision.

## Current non-goals

- authoritative knowledge creation without review
- direct repository mutation from parser adapters
- live AI extraction pipelines
- OCR provider selection
- object-store selection
- vector database selection

See `spec/documents/document-engine-boundary.md` for the conceptual pipeline.
