# DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC Prototype Review

Date: 2026-07-22
Status: Release Candidate (not released)
Active implementation package: `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC`
Released baseline: `FRAGMENT-CANDIDATE-REVIEW-SLICE-0.1`
Next active package: TBD after review

## Validation snapshot

- Domain tests: `181/181`
- Application tests: `184/184`
- Documents tests: `63/63`
- Infrastructure tests: `61/61`
- Architecture tests: `24/24`
- AI tests: `6/6`
- Desktop tests: `45/45`
- Total tests: `564/564` (hardening pass — new tests pending)

## Checkpoints completed

- Prompt 1 (Domain & Application substrate): Created `ParserExecutionStatus` enum (Completed, CompletedWithWarnings, Failed), `DiagnosticSeverity` enum (Info, Warning, Error), `DiagnosticRefType` enum (Ordinal, NormalizedLocator), `ParserDiagnostic` immutable entity, `ParserDiagnosticId` typed ID, `IParserDiagnosticRepository` port, `DocumentParserRegistry` with `GetRequired` and `List`, `ParserDescriptor` with `ParserDeterminism`, updated `ParserExecutionResult` to carry `IReadOnlyList<ParserDiagnosticResult>` diagnostics, updated `RequestDocumentParsingUseCase` to resolve parser via registry and persist diagnostics, updated `LoadParsingSnapshotUseCase` to load diagnostics per run, updated `LoadFragmentReviewSnapshotUseCase` to load per-candidate diagnostics, created `AddParserDiagnostics` EF migration, created `ParserDiagnosticRecord` with EF Core mapping, registered repositories in DI, added 25 domain tests and 25 Application tests for diagnostics and registry.
- Prompt 2 (Documents Adapter — StructuredTextDocumentParser): Implemented `StructuredTextDocumentParser` with heading extraction (`#{1,6}` regex), numbered clause extraction (dotted and single-dot patterns), lettered clause extraction (`a)` pattern), pipe-delimited table extraction, line-range overlap detection, `OVERLAPPING_CONTENT` diagnostic emission, contract hash via SHA-256. Registered as `IDocumentParser` singleton. Added `.md` to `text/markdown` extension mapping in `BasicImportedContentInspector`. Added 25 structured text parser tests.
- Prompt 3 (Infrastructure & Executable Proof): Extended `ParsingExecutableWorkflowRunner` with structured text source import and parsing using `structured-text-deterministic` parser key. Updated `ParsingExecutableWorkflowResult` with structured text fields. Added diagnostics display to `ParsingExecutableWorkflowConsoleFormatter`. Added 5 infrastructure persistence tests for `parser_diagnostics` table. Added 6 Desktop integration tests for structured text parsing, diagnostics round-trip, and restart validation with diagnostics.

## Behavior validated

### Structured text parsing

- `StructuredTextDocumentParser` extracts headings (levels 1-6), numbered clauses (dotted like `1.1` and single like `1.`), lettered clauses (like `a)`), and pipe-delimited tables.
- Heading regex: `^(#{1,6})\s+(.+)$` with multiline mode.
- Clause regex alternation handles both `1.1 text` and `1. text` patterns.
- Table detection requires at least 3 lines: header row, separator row, and one data row.
- Tables are extracted as `ContentKind.Table` with `High` confidence.

### Overlapping-fragment policy

- Line-range overlap detection runs across all structural fragments (headings, clauses, tables).
- Whole-document fragments are excluded from overlap tracking by design.
- Overlapping fragments emit `OVERLAPPING_CONTENT` diagnostic with `Warning` severity.
- Diagnostic references the first overlapping fragment by ordinal and includes both locator values and line ranges.
- When diagnostics are present, parser status is `CompletedWithWarnings` (not `Failed`).

### Parser diagnostics model

- `ParserDiagnostic` is immutable, not independently auditable, no review lifecycle.
- Diagnostics carry provenance through `ParserRunId`, not through a separate lifecycle.
- `DiagnosticSeverity.Info` and `Warning` are carried on successful results; `Error` is reserved for `Failed` status only.
- `DiagnosticRefType.Ordinal` references fragments by position within the parser run.
- `DiagnosticRefType.NormalizedLocator` references fragments by normalized locator value.
- Application resolves diagnostic references to `FragmentCandidate.IdentityKey` after construction.
- Diagnostics are persisted through `IParserDiagnosticRepository` and loaded through `LoadParsingSnapshotUseCase`.
- Maximum 100 diagnostics per parser run (enforced by `RequestDocumentParsingUseCase`).

### Parser registry

- `DocumentParserRegistry` collects all `IDocumentParser` implementations from DI.
- `GetRequired(parserKey)` returns the parser or throws if not found.
- `List()` returns all registered parsers for discovery.
- `ParserDescriptor` includes `ParserKey`, `ParserVersion`, `ContractVersion`, `ContractHash`, `SupportedMediaTypes`, `ContentKind`, and `Determinism`.

### Desktop executable proof

- Imports structured text source with `text/markdown` media type.
- Parses with `structured-text-deterministic` parser key through the full Application pipeline.
- Structured text parser produces heading, clause, and table fragments.
- `OVERLAPPING_CONTENT` diagnostics are produced when structural fragments overlap.
- Diagnostics are persisted in SQLite and survive provider recreation.
- Diagnostics are loaded through `LoadParsingSnapshotUseCase` and displayed in console output.
- Structured text parse result is distinct from plain text parse result (separate parser key, separate source).
- Restart validation confirms diagnostics survive dispose/recreate of the DI provider.

### Diagnostics persistence

- `parser_diagnostics` table exists after migration with 10 total migrations.
- `AddRangeAsync` persists diagnostics and `GetByParserRunAsync` reloads them with all fields preserved.
- `GetByParserRunAndCandidateAsync` filters diagnostics by parser run and candidate reference value.
- Diagnostics are isolated between parser runs (same source parsed by different parsers produces separate diagnostic sets).
- Empty diagnostic sets are correctly returned for parser runs with no diagnostics.

### Canonical contract descriptor

- Both parsers compute `ContractHash` from a canonical descriptor containing every behavior-affecting rule.
- StructuredTextDocumentParser descriptor includes: parser identity/version, supported media types, fragment mappings, content kind, regex patterns (heading, clause, table row, table separator), table extraction rules (min lines, separator requirement), clause termination behavior, overlap detection policy, and diagnostic codes.
- PlainTextDocumentParser descriptor includes: parser identity/version, supported media types, fragment mappings, content kind, max content length, lines per group, paragraph split behavior, and strict UTF-8 flag.
- Changing any rule in the descriptor changes the `ContractHash`.
- Unchanged rule ordering produces the same hash (deterministic serialization).

### Parser execution status durability

- `ParserExecutionStatus` (Completed, CompletedWithWarnings, Failed) is persisted with `ParserRun` in the `parser_runs` table.
- Forward EF migration `AddParserExecutionStatus` adds the column with default value 0 (Completed).
- Execution status is included in `RequestDocumentParsingResult`, `ParserRunSnapshot`, and console output.
- Idempotent replay preserves the original execution status from the persisted run.
- Domain entity exposes `SetExecutionStatus()` method for Application layer to set after parsing.

### Strict UTF-8 decoding

- Both parsers use `UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)`.
- Invalid UTF-8 bytes cause a `DecoderFallbackException` caught by the parser, producing a terminal `Failed` status with `MalformedOutput` classification.
- BOM bytes are accepted via `detectEncodingFromByteOrderMarks: true` in `StreamReader`.

### Authority isolation

- Parser diagnostics do not create Knowledge, Report, or AI records.
- Structured text parsing through the Desktop workflow does not widen authority boundaries.
- Diagnostic persistence is scoped to the parser run lifecycle.

## Desktop composition boundary

- The Desktop host composes the structured text parsing workflow through Application commands and queries only.
- `StructuredTextDocumentParser` is registered as `IDocumentParser` in the Documents adapter layer via `DocumentFoundationAdapters`.
- `BasicImportedContentInspector` handles `.md` extension to `text/markdown` mapping.
- The Desktop host does not directly access Domain entities, EF Core DbContext, or parser internals.

## Migration status

- 10 total EF Core migrations.
- `AddParserDiagnostics` was created during Prompt 1 for the diagnostics table.
- No additional migrations were needed for Prompt 2 or Prompt 3.

## Prototype review questions

### Does structured text parsing produce reliable structural fragments?

Yes. The `StructuredTextDocumentParser` extracts headings, clauses, and tables from markdown content using deterministic regex patterns. Each fragment is bound to a line range and carries a `StructuralPath` locator type. The parser produces a whole-document fragment plus structural fragments, and overlap detection identifies conflicting line ranges.

### Does the overlapping-fragment policy work correctly?

Yes. When structural fragments (headings, clauses, tables) share line ranges, the overlap detector emits `OVERLAPPING_CONTENT` diagnostics with `Warning` severity. The parser status becomes `CompletedWithWarnings`. The whole-document fragment is intentionally excluded from overlap tracking. This matches the spec's requirement that overlap is informational, not a failure.

### Are parser diagnostics durable and reloadable?

Yes. Diagnostics are persisted to the `parser_diagnostics` table through `IParserDiagnosticRepository.AddRangeAsync`. They survive provider recreation and are loaded through `LoadParsingSnapshotUseCase` as part of `ParserRunSnapshot.Diagnostics`. The Desktop executable proof demonstrates this through restart validation.

### Does the parser registry correctly resolve parser keys?

Yes. `DocumentParserRegistry` collects all `IDocumentParser` implementations from DI and resolves by parser key. The workflow runner uses `structured-text-deterministic` for the structured text source, and `plain-text-deterministic` for plain text sources. The registry ensures the correct parser is invoked for each source.

### What must be resolved before production diagnostics?

1. **Diagnostic aggregation**: The current model stores individual diagnostics per parser run. Aggregate summary views (e.g., warning counts by severity) would be useful for production dashboards.
2. **Diagnostic export**: Compliance and audit workflows may need diagnostic export beyond the current snapshot model.
3. **Cross-run diagnostic comparison**: When parsing the same source with different parser versions, comparing diagnostic sets could reveal parser regressions.

## Known friction

- The `MapFailureClassification` in `RequestDocumentParsingUseCase` maps parser failure reasons through string matching, which loses the original parser-specific classification. Acceptable for the foundation but should be refined before production.
- Pre-existing CA1848 warnings throughout Application use cases (LoggerMessage delegates) are acknowledged technical debt.
- The structured text parser's clause continuation logic consumes non-blank, non-structural lines as part of the clause. This means a clause that precedes a table without a blank line separator will overlap with the table. This is by design (the overlap diagnostic captures it), but content authors should be aware.
- The `text/*` wildcard media types were removed from `PlainTextDocumentParser.SupportedMediaTypes` per refinement. Exact types only.

## Recommended next package

Recommendation: TBD after review

Rationale:

- The `DOCUMENT-UNDERSTANDING-TEXT-ADAPTER-0.1-RC` validates structural text extraction, parser diagnostics, parser registry, and overlapping-fragment policy through the full Application pipeline and Desktop executable proof.
- The next increment should focus on either Knowledge promotion from reviewed candidates or broader Document Understanding capabilities (e.g., PDF extraction, OCR boundary).

Follow-on order:

1. Knowledge promotion from reviewed candidates (per `EDR-KE-011`)
2. PDF/text-layout adapter integration
3. Parser diagnostics aggregation and export
4. Cross-run diagnostic comparison for parser regression detection
