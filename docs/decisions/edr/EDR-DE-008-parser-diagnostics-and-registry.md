# EDR-DE-008: Parser Diagnostics and Registry

Date: 2026-07-22
Status: Accepted
Package: `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC`

## Decision

Establish a parser diagnostics model and parser registry for deterministic document parsing.

## Context

The parsing and fragment foundation (PARSING-AND-FRAGMENT-FOUNDATION-0.1) established parser runs, fragment candidates, and authority isolation. The next increment adds structural text extraction, which requires:

1. A way to report parser-specific warnings and informational messages without failing the parse.
2. A way to resolve parser adapters by key at runtime, supporting multiple parsers in the same application.

## Decision Details

### Parser Execution Status

Replace the boolean success flag with a three-state enum:

- `Completed(0)` — parser finished successfully with no warnings.
- `CompletedWithWarnings(1)` — parser finished successfully but produced diagnostics.
- `Failed(2)` — parser failed to produce usable output.

This distinguishes full completion from degraded-but-successful extraction.

### Diagnostic Severity

Three severity levels:

- `Info(0)` — informational messages about parser behavior.
- `Warning(1)` — conditions that degraded quality but did not prevent extraction.
- `Error(2)` — reserved for Failed status only; not carried on successful results.

### Diagnostic Reference Types

Diagnostics can reference specific fragments within a parser run:

- `Ordinal(0)` — references a fragment by its position within the parser output sequence.
- `NormalizedLocator(1)` — references a fragment by its normalized locator value.

Application resolves these references to `FragmentCandidate.IdentityKey` after construction.

### Parser Diagnostic Entity

`ParserDiagnostic` is immutable, not independently auditable, and carries no review lifecycle. Diagnostics are durable parser evidence with provenance through `ParserRunId`. They are:

- Created during parser execution and persisted alongside parser runs.
- Loaded through `LoadParsingSnapshotUseCase` as part of `ParserRunSnapshot.Diagnostics`.
- Per-candidate diagnostics loaded through `LoadFragmentReviewSnapshotUseCase`.
- Maximum 100 diagnostics per parser run (enforced by `RequestDocumentParsingUseCase`).

### Parser Registry

`DocumentParserRegistry` collects all `IDocumentParser` implementations from DI and resolves by parser key. This supports:

- Multiple parsers in the same application (e.g., PlainTextDocumentParser, StructuredTextDocumentParser).
- Runtime parser resolution by key in Application use cases.
- Parser discovery through `List()` for capability queries.

## Consequences

- Parser diagnostics are part of the parser run lifecycle, not a separate domain concept.
- Diagnostics do not create Knowledge, Report, or AI records.
- The parser registry is a thin adapter over DI, not a new abstraction layer.
- `CompletedWithWarnings` status is distinguishable from `Completed` for monitoring and dashboards.
- Diagnostic aggregation, export, and cross-run comparison are deferred to production concerns.

## Related

- `EDR-DE-006` Fragment identity contract-version choice
- `EDR-DE-007` Fragment candidate review disposition and promotion prerequisites
- `EDR-KE-010` Knowledge fragment identity
