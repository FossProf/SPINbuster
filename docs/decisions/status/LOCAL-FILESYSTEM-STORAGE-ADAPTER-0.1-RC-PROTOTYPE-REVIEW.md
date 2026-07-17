# LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1-RC Prototype Review

Date: 2026-07-17
Status: Released
Latest released baseline: `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1`
Released baseline: `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1`
Next active package: `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`

## Validation snapshot

- Domain tests: `53/53`
- Application tests: `74/74`
- Documents tests: `28/28`
- Infrastructure tests: `27/27`
- Architecture tests: `21/21`
- Desktop tests: `23/23`

## Scope reviewed

- Local filesystem immutable content storage beneath the existing Document Engine boundary
- Desktop composition and configuration for persisted bytes plus SQLite metadata
- Restart, repeated-run, and orphan visibility behavior
- No widening of authoritative Knowledge, Report, or AI boundaries

## Exact-byte persistence across provider and host restart

- The review candidate now proves that bytes stored by one service provider instance can be reopened by a separately created provider instance against the same SQLite database and storage root.
- The reopened content preserves the authoritative content hash, hash algorithm, algorithm version, and content length.
- Restart validation now uses the real local filesystem adapter instead of the earlier in-memory fixture store.

## SQLite metadata and filesystem byte consistency

- `ImportedDocumentSource` metadata and immutable stored bytes remain aligned through hash, algorithm, version, and content length.
- Processing now revalidates reopened bytes against the authoritative metadata before provider execution continues.
- Corrupted bytes therefore fail explicitly instead of being silently processed as if they were still authoritative.

## Repeated executable workflow against the same database and storage root

- The Desktop executable workflow now runs safely more than once against the same SQLite database and filesystem root.
- Each run scopes its assertions to current-run identities rather than positional assumptions.
- Prior run data remains preserved and queryable after later runs complete.

## Prior-data preservation

- The workflow remains stable in the presence of unrelated preexisting projects and document records.
- A prior incomplete document workflow remains unchanged when a later deterministic workflow runs in the same database.
- The executable review therefore proves both repeatability and non-destructive coexistence.

## Missing and corrupt file handling

- Missing physical files now produce an explicit terminal failed processing attempt with storage-unavailable classification.
- Corrupted physical files now produce an explicit terminal failed processing attempt with validation-failed classification.
- Neither failure path widens authority or produces partial Knowledge, Report, or AI mutations.

## Concurrent-write findings

- Concurrent identical writes converge safely on one immutable object identity.
- Concurrent conflicting writes preserve the first valid immutable object and reject the conflicting content explicitly.
- The design accepts a bounded Windows TOCTOU limitation around reparse-point and file-system race behavior; that limitation is documented rather than hidden.

## Atomic-write and temporary-file behavior

- The adapter stages content to a unique temporary file beneath the controlled root.
- It flushes and closes the staged file, verifies length and hash, then atomically moves it into the final ID-addressed location.
- Known temporary files are removed on bounded failure paths.
- Final object verification can be re-run after move so the adapter does not claim success without deliberate integrity confirmation.

## Object layout

- The adapter uses an ID-addressed layout:
  - `objects/{first-two-hex}/{next-two-hex}/{storage-object-id-n}.blob`
- Imported filenames do not influence the physical storage path.
- Public metadata returns only provider-relative object keys, never absolute paths.

## Orphan visibility

- If immutable file storage succeeds but the SQLite unit of work fails afterward, no authoritative metadata partially commits.
- The resulting orphaned object remains visible through the adapter-specific bounded inventory surface.
- The current package intentionally does not implement deletion or reconciliation, which stays deferred under the existing reconciliation policy.

## Security controls

- The configured root is canonicalized internally.
- Generated final and temporary paths are validated to remain beneath that canonical root.
- Imported filenames remain untrusted metadata only and do not participate in the storage path.
- Public results and Desktop output do not expose absolute paths.
- Existing-path validation rejects malformed layout, directories where files are expected, and reparse points on validated path segments.

## Residual Windows limitations

- Reparse-point defense is bounded rather than absolute because a privileged actor can still race path replacement after validation and before final use.
- The generated Windows apphost may still be blocked by local machine policy even when the managed DLL runs correctly.
- Those behaviors are documented as environmental or platform limitations, not misrepresented as stronger guarantees than the platform provides.

## Authority isolation

- The filesystem adapter and executable workflow do not create or mutate authoritative Knowledge records.
- They do not create or mutate authoritative Report records.
- They do not create or mutate AI proposal records.
- The Document Engine remains limited to imported-source metadata, processing attempts, candidate state, and audit history.

## In-memory adapter boundary

- The in-memory immutable content store remains fixture-only.
- The real Desktop composition now uses the local filesystem adapter for durable byte storage.
- The in-memory adapter remains useful for deterministic tests and fault injection only.

## Friction discovered

- The original Desktop default storage root incorrectly pointed durable content under build output semantics.
- That has been corrected to `%LOCALAPPDATA%\\SPINbuster\\document-content`.
- Introducing real persisted bytes required Application-level failure classification so orchestration can distinguish missing, unavailable, access-denied, integrity, cancellation, and generic I/O outcomes without depending on filesystem-specific types.
- Restart and repeated-run proof required eliminating lingering positional assumptions in workflow formatting and snapshot interpretation.

## Migration and schema impact

- No database migration was required for this review candidate.
- No released migration changed.
- Pending model changes remain clean.

## Recommended next package

Recommendation: `PARSING-AND-FRAGMENT-FOUNDATION-0.1-RC`

Rationale:

- Durable local byte storage is now sufficiently hardened for restart, repeated execution, integrity validation, and orphan visibility.
- The next highest-value increment is to define how deterministic parsing outputs become non-authoritative fragment candidates without widening the authoritative boundary.
- Cleanup, retention, reconciliation, OCR, and AI extraction can remain deferred while the parsing and fragment contracts are established.
