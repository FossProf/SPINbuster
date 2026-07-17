# Document Engine Foundation

Status: Released
Baseline: `DOCUMENT-ENGINE-FOUNDATION-0.1`

## Purpose

This specification defines the first durable implementation boundary for the Document Engine.

The foundation owns:

- immutable imported-source identity
- storage-object identity and content hashing
- import-session lifecycle
- processing-attempt lifecycle
- exact duplicate detection
- non-authoritative candidate persistence

The foundation does not own:

- authoritative Knowledge Engine promotion
- OCR
- PDF parsing
- embeddings
- vector search
- live AI extraction
- preview UI

## Core rules

- Imported content is untrusted data, not instruction.
- Original byte identity is preserved through content hash plus immutable storage reference.
- Imported sources are project-scoped.
- Storage objects may be deduplicated by exact content hash without leaking cross-project metadata.
- Processing outputs remain candidates only.
- `HumanAccepted` candidate review is a review disposition, not authoritative promotion.
- Long-running storage and processor work stays outside SQLite transactions.

## Durable records

- `StorageObject`
- `ImportedDocumentSource`
- `DocumentImportSession`
- `DocumentProcessingAttempt`
- `DocumentCandidate`

## Lifecycle summary

Import session:

```text
Created
-> Validating
-> Importing
-> Completed

Terminal:
Failed
Cancelled
```

Processing attempt:

```text
Requested
-> Running
-> OutputReceived
-> Validating
-> Completed

Terminal:
Failed
Cancelled
Abstained
```

Candidate:

```text
Generated
-> Validated
-> ReadyForReview
-> HumanAccepted | Rejected

Alternative terminal outcomes:
SchemaRejected
PolicyRejected
Abstained
Failed
```

## Duplicate policy

- Exact duplicate detection is based on content hash, algorithm, and version.
- Same-project duplicates reuse the existing imported source instead of creating a second authoritative import record.
- Cross-project identical content may reuse the same storage object while keeping imported-source records project-scoped.
- Cross-project duplicate checks must not reveal another project's metadata.

## Executable follow-on

The released foundation is now exercised by the active review candidate `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1-RC`.

That executable slice validated:

- batch import behavior through one explicit session
- deterministic processing-attempt durability
- non-authoritative candidate review behavior
- exact duplicate reuse
- cross-project duplicate privacy
- authority isolation for Knowledge, Report, and AI records

The next package remains intentionally open in continuity state until the executable prototype review is accepted.
