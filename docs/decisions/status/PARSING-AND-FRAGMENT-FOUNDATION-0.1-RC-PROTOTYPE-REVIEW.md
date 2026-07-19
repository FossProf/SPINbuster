# PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC Prototype Review

Date: 2026-07-18
Status: Review Candidate
Active implementation package: `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`
Next active package: `PARSING-EXECUTABLE-SLICE-0.1-RC` (recommended)

## Validation snapshot

- Domain tests: `152/152`
- Application tests: `156/156`
- Documents tests: `28/28`
- Infrastructure tests: `42/42`
- Architecture tests: `24/24`
- Desktop tests: `34/34`
- Total tests: `442/442`

## Checkpoints completed

- Prompt 1: Domain types (ParserEngine, FragmentIds) — parser-run lifecycle, fragment-candidate identity, locator model, audit events
- Prompt 2: Application layer (IDocumentParser port, RequestDocumentParsing, LoadParsingSnapshot, DI, logging)
- Prompt 3: Deterministic parser adapter (PlainTextDocumentParser), SQLite persistence (EF Core records, repositories, migration), replay key hardening (5-column unique index)
- Prompt 4: Integration, governance, and RC validation (this review)

## Behavior validated

### Executable proof

- Import a controlled text source through the Application import pipeline.
- Request deterministic parsing through `RequestDocumentParsingCommand` with the real `PlainTextDocumentParser`.
- Persist parser run, fragment candidates, and audit history through real SQLite migration.
- Reload parser run and candidates through `LoadParsingSnapshotQuery`.
- Print IDs, locators, bounded summaries, parser key/version, status, and audit history.
- Run twice against the same database and prove prior data is preserved (idempotent replay).
- Dispose and recreate the service provider and reload from persisted state.
- Demonstrate unsupported media type, cancelled parse, and malformed output as expected failures without crashing.
- Verify authoritative Knowledge, Report, and AI records remain unchanged.

### Candidate identity reproducibility

- Fragment candidate identity is deterministically derived from `{ImportedSourceId}:{ParserKey}@{ParserContractVersion}:{LocatorType}:{NormalizedLocatorValue}`.
- Replay of the same parser against the same source produces identical fragment candidate IDs and identity key hashes.
- The 5-column unique index `(ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash)` prevents accidental replay across different parser versions.

### Parser version coexistence

- Parser runs are keyed by source + parser key + parser version + contract version + contract hash.
- Updating the parser implementation while retaining the same contract version correctly creates a new run rather than replaying an older one.
- Historical candidates from prior parser versions remain preserved.

### Locator expressiveness

- The `FragmentLocator` value object supports WholeDocument, Page, Paragraph, LineRange, and StructuralPath locator types.
- The `PlainTextDocumentParser` produces WholeDocument (`"*"`), Paragraph (`"1:N"`), and LineRange (`"start-end"`) locators.
- Locators are parser-specific strings that do not bind Domain to any parser SDK.

### Failure semantics

- Unsupported media types produce a terminal `Failed` run with descriptive failure reason.
- Cancelled parser runs produce a terminal `Cancelled` run.
- Malformed or empty content produces a terminal `Failed` run.
- All failure scenarios are durable and reviewable through audit history.
- The parser run boundary remains non-authoritative: failures do not widen the Knowledge, Report, or AI boundaries.

### Authority isolation

- Document parsing does not create or mutate `KnowledgeDocument`, `KnowledgeDocumentRevision`, `KnowledgeRelationship`, `KnowledgeCitation`, Report, or AI Proposal records.
- Authority isolation remained intact across success, failure, reload, and repeated execution flows.

## Desktop composition boundary

- The Desktop host composes the executable parsing workflow through Application commands and queries only.
- The `PlainTextDocumentParser` is registered as `IDocumentParser` in the Documents adapter layer.
- The Desktop host does not directly access Domain entities, EF Core DbContext, or parser internals.
- Startup migration continues through the Infrastructure migrator abstraction.

## Static initialization fix

- The `PlainTextDocumentParser` had a static field initialization order issue: `ContractHash` referenced `SupportedMediaTypes` before it was initialized.
- Fixed by reordering `SupportedMediaTypes` before `ContractHash`.
- This was caught by the Desktop integration tests and resolved before release.

## Replay key hardening

- The original 3-column unique index `(ImportedSourceId, ParserKey, ParserContractVersion)` was insufficient.
- Updating the parser implementation while retaining the same contract version would incorrectly replay an older run.
- Expanded to 5-column index: `(ImportedSourceId, ParserKey, ParserVersion, ParserContractVersion, ParserContractHash)`.
- This change required threading `ParserVersion` through the domain entity, EF record, mapper, repository query, use case, and snapshot result.

## Specification alignment

- `spec/documents/parsing-and-fragment-foundation.md` defines the Domain boundary.
- `spec/documents/README.md` now reflects the executable proof status.
- The identity derivation formula in the spec matches the implementation.

## Prototype review questions

### Is candidate identity reproducible and sufficiently stable within one source revision?

Yes. Identity is deterministically derived from four inputs (ImportedSourceId, parser key, parser contract version, and normalized locator type+value). Replay of the same parser against the same source produces identical fragment candidate IDs and identity key hashes. Identity is intentionally not stable across document revisions — this is a deliberate design decision documented in `EDR-KE-010`.

### Are locators expressive without binding Domain to a parser SDK?

Yes. Locators are simple string-typed value objects with a typed enum. The Domain defines the locator types (WholeDocument, Page, Paragraph, LineRange, StructuralPath) and normalization rules. Parser adapters produce locator strings that conform to these types without importing any parser-specific SDK. The `PlainTextDocumentParser` demonstrates this by producing plain-text-appropriate locators.

### Can parser versions coexist and preserve historical candidates?

Yes. The 5-column unique index ensures that different parser versions create separate parser runs. Historical candidates from prior parser versions are preserved in the database and accessible through the snapshot query. The `LoadParsingSnapshotQuery` returns all parser runs for a given source, preserving version history.

### Are candidate limits and failure semantics adequate for real documents?

Adequate for the foundation. The `MaxFragmentCandidates` limit (10,000) prevents unbounded fragment production. Terminal failure states (Failed, Cancelled) are durable and auditable. Failure reasons are descriptive. The `PlainTextDocumentParser` handles empty content, oversized content, and unsupported media types gracefully. Real document adapters (PDF, OCR) will need additional limits and failure classifications, but the foundation boundary is sufficient.

### Does the parsing boundary remain non-authoritative?

Yes. Fragment candidates cannot become KnowledgeDocumentRevision, KnowledgeAssertion, Requirement, Citation, or report content directly. They require explicit human review and Application-level promotion. The executable proof verified that parsing does not create or mutate any authoritative Knowledge, Report, or AI records.

### What must be resolved before PDF/text-layout adapters or Knowledge promotion?

1. **PDF adapter integration**: The `IDocumentParser` port is ready; a PDF adapter needs to implement it with appropriate supported media types and fragment extraction logic.
2. **Text-layout awareness**: LineRange and Paragraph locators need layout-aware extraction for PDFs with columns, tables, and figures.
3. **Promotion design**: Fragment-to-knowledge promotion workflows remain deferred per `EDR-KE-011`. The foundation provides durable candidates; promotion design should follow once richer parser adapters produce more meaningful fragments.
4. **Candidate review workflow**: The current `RecordDocumentCandidateReview` command handles `DocumentCandidate` types. Fragment candidates need a review workflow that bridges parsing output to knowledge promotion.
5. **Cross-revision fragment equivalence**: Intentionally deferred per `EDR-KE-010`. Becomes relevant when documents are re-parsed after revision updates.

## Known friction

- SQLite provider-specific query-shaping for `DateTimeOffset` ordering remains a known implementation concern.
- The `MapFailureClassification` in `RequestDocumentParsingUseCase` maps parser failure reasons through string matching, which loses the original parser-specific classification. This is acceptable for the foundation but should be refined before production.
- The `PlainTextDocumentParser` is a simple UTF-8 text parser. Real document adapters will require more sophisticated content extraction, but the parser port boundary is ready.

## Environmental note

- On the current Windows review machine, the generated `SPINbuster.Desktop.exe` apphost may fail to launch with `Access is denied` even when the managed DLL runs successfully.
- The executable proof validates through the managed DLL path.
- Treat this as an environment-specific Windows/apphost policy issue for the temporary bootstrap host.

## Recommended next package

Recommendation: `PARSING-EXECUTABLE-SLICE-0.1-RC`

Rationale:

- The foundation is now validated end-to-end: import, parse, persist, reload, idempotent replay, failure handling, and authority isolation.
- The next increment should extend the Desktop executable proof to exercise the full parsing workflow with more realistic content and demonstrate the complete Document Understanding pipeline.
- This package stays focused on the executable proof without prematurely broadening into Knowledge promotion, OCR, or AI extraction.

Follow-on order after that:

1. `PARSING-EXECUTABLE-SLICE-0.1-RC` — extend Desktop proof for multi-source parsing, version coexistence, and review
2. `FRAGMENT-PERSISTENCE-AND-REVIEW-0.1-RC` — candidate review workflow and promotion prerequisites
3. PDF/text-layout adapter integration
4. Knowledge promotion from reviewed candidates
