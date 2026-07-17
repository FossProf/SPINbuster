# DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1 Prototype Review

Date: 2026-07-17
Status: Released
Latest released baseline: `DOCUMENT-ENGINE-EXECUTABLE-SLICE-0.1`
Next active package: `LOCAL-FILESYSTEM-STORAGE-ADAPTER-0.1-RC`

## Validation snapshot

- Domain tests: `53/53`
- Application tests: `74/74`
- Documents tests: `5/5`
- Infrastructure tests: `27/27`
- Architecture tests: `20/20`
- Desktop tests: `16/16`

## Behavior validated

- Multi-source batch import works through one explicit import session with deterministic session completion.
- Same-project exact duplicates reuse the existing project-scoped imported source instead of creating a second authoritative import record.
- Cross-project exact duplicates are surfaced as a privacy-safe boolean without disclosing Project A metadata to Project B.
- Immutable storage objects can reopen exact original bytes and preserve stable content hashes across reloads.
- Deterministic processing requests persist durable processing-attempt history before provider work proceeds.
- Terminal processing outcomes now persist explicitly for success, failure, schema rejection, policy rejection, malformed candidate handling, storage unavailability, unexpected processor failure, and cancellation.
- Candidate review remains non-authoritative: `HumanAccepted` and `Rejected` are durable review dispositions only.
- Knowledge, Report, and AI Proposal state remain unchanged across the entire executable document workflow.
- The Desktop host stays thin and Application-driven for document commands and queries.

## Multi-source batch import behavior

- The slice proved that one import session can accept multiple sources before explicit completion.
- A domain hardening pass was required because the original session lifecycle prevented repeated validation and import transitions within one active batch.
- The final rule is now clear: a batch import session may cycle through validation and import work multiple times while remaining active, and only explicit completion closes the session.

## Exact duplicate handling

- Exact duplicate detection remains based on content hash, hash algorithm, and algorithm version.
- Different filenames with byte-identical content are treated as exact duplicates.
- Same filenames with different bytes are not treated as duplicates.
- Same-project duplicates reuse the original imported-source identity and increment duplicate counts rather than broadening authoritative state.

## Cross-project duplicate privacy

- Cross-project duplicate reporting is limited to a non-sensitive signal that identical content exists elsewhere.
- The executable path does not leak the other project's imported-source identifiers, filenames, storage keys, or audit metadata.
- This boundary held across deterministic Desktop execution and reload.

## Storage and orphan behavior

- The current deterministic in-memory immutable content store is sufficient for executable validation but remains a fixture adapter, not a production-oriented local storage implementation.
- Exact-byte reopen and content-hash stability were validated successfully.
- Commit-failure testing confirmed that authoritative SQLite state does not partially commit after content storage succeeds.
- Orphan content may still exist after a forced commit failure, which is consistent with the current deferred reconciliation policy and should remain visible rather than silently deleted.

## Processing-attempt durability

- Processing attempts are persisted before provider execution begins.
- Terminal attempt states are now durably recorded for all scripted deterministic outcomes rather than leaving an attempt stuck in `Running` or `Validating`.
- The hardening pass also corrected audit staging so repeated aggregate updates stage only delta audit events instead of re-staging complete historical trails.

## Failure and cancellation handling

- Expected failure demonstrations now surface as explicit workflow outcomes rather than crashing the scripted success path.
- Storage-unavailable, storage-read-throws, structured processor failure, malformed candidate, schema rejection, policy rejection, invalid review transition, size-limit failure, unsupported media, and post-session duplicate import are all covered.
- Cancellation required a targeted hardening pass so a deterministic provider cancellation produces a durable cancelled attempt without poisoning later workflow execution in the same host scope.

## Candidate review semantics

- `HumanAccepted` remains a review outcome only.
- `Rejected` remains terminal.
- Neither review action creates authoritative Knowledge records, Report revisions, or AI Proposal mutations.
- Invalid terminal review transitions are correctly rejected.

## Desktop composition boundary

- The Desktop host composes the executable workflow through Application commands and queries only.
- A small Infrastructure migrator abstraction was introduced so the Desktop host no longer reaches directly into `SpinbusterDbContext` for migration work.
- This keeps the composition root thin while preserving startup migration behavior.

## Infrastructure migrator abstraction

- The new migrator abstraction is a useful stabilization point for temporary hosts and future server-side startup flows.
- It preserves the current SQLite migration path while reducing direct EF Core leakage into the host layer.
- This abstraction should remain narrow and should not become a general-purpose data access escape hatch.

## Audit-delta staging fix

- The executable slice surfaced a real bug in document-session audit staging.
- Some handlers were re-staging entire aggregate audit trails after each mutation, which created duplicate EF-tracked audit records and invalid replay behavior.
- The fix now stages only newly appended audit events for repeated aggregate mutations, which restores correct audit ordering and durable delta semantics.

## SQLite DateTimeOffset query-shaping friction

- SQLite provider translation rejected several `DateTimeOffset` ordering expressions inside repository queries.
- The executable slice was hardened by filtering in SQL and then applying bounded ordering client-side for the affected document repositories.
- This is acceptable for the current local deterministic slice, but it is explicit technical friction that should remain documented.

## Authority isolation

- The executable slice confirmed that document import, processing, and review do not create or mutate:
  - `KnowledgeDocument`
  - `KnowledgeDocumentRevision`
  - Knowledge relationships or citations
  - authoritative Report records
  - AI Proposal records
- Authority isolation remained intact across success, failure, reload, and review flows.

## Defects found during the RC hardening pass

- Import-session lifecycle originally blocked multi-source batch import.
- Audit staging originally re-staged complete aggregate trails instead of deltas.
- Desktop startup originally reached directly into EF Core for migration execution.
- SQLite query translation failed on several `DateTimeOffset` sort expressions.
- The deterministic cancellation scenario originally destabilized later workflow execution in the same host scope.
- One reload test originally assumed the later deterministic scenario data would not exist, which did not match the actual workflow shape.

## Import-session friction

- The main friction was not user-facing workflow complexity but lifecycle expressiveness.
- The original model captured session phases but was too restrictive for repeated batch imports.
- That friction is now resolved without broadening the authoritative boundary.

## Persistence and query friction

- SQLite remains viable for this slice, but provider-specific query-shaping constraints are now a known implementation concern.
- The presentation-safe project snapshot query is useful and appropriately bounded, but it is now important to keep it free of provider-specific leakage and over-eager materialization.

## Is the foundation ready for a real local filesystem adapter?

- Yes.
- The current executable slice proved exact-byte reopen, content-hash stability, duplicate semantics, import-session behavior, and orphan-state expectations well enough to support replacing the deterministic in-memory store with a real local filesystem adapter.
- That change can remain infrastructure-local and should not require widening the Application or Domain boundaries.

## Deferred decisions becoming more urgent

- Orphan-content reconciliation policy remains deferred and will matter more once storage leaves the in-memory fixture world.
- Parser and fragment-candidate contracts are the next major content-model design pressure after a real filesystem adapter exists.
- Candidate-promotion design should remain deferred until richer parsing and fragment semantics exist.

## Environmental note: Windows apphost execution

- On the current Windows review machine, the generated `SPINbuster.Desktop.exe` apphost may still fail to launch with `Access is denied` even when the managed `SPINbuster.Desktop.dll` runs successfully.
- The executable slice itself is not dependent on that apphost behavior: the managed DLL path exercises startup migration, repeated execution, persistence reload, and authority-isolation behavior successfully.
- Treat this as an environment-specific Windows/apphost policy issue for the temporary bootstrap host, not as a product defect in the Document Engine workflow.
- Continue validating the workflow through the managed DLL path until the local machine policy or apphost behavior is resolved.

## Recommended next package

Recommendation: `a. local filesystem storage adapter`

Rationale:

- The executable slice validated the current storage boundary thoroughly enough to justify a real local adapter.
- That package is lower risk than jumping straight into parsing and fragment promotion.
- It keeps the next increment infrastructure-focused while preserving the current non-authoritative document boundary.

Follow-on order after that:

1. local filesystem storage adapter
2. parsing and fragment foundation
3. candidate-promotion design
