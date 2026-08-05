# ENGINEERING-ASSERTION-PROMOTION-0.1-RC — WO0 Slice Design Brief

Status: For architectural review (WO0 deliverable)
Package: `ENGINEERING-ASSERTION-PROMOTION-0.1-RC`
Checkpoint: `ASSERTION-ARCHITECTURE-RECONNAISSANCE-CHECKPOINT`
Released baseline: `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1`
Governance baseline: `ENGINEERING-WORKFLOW-STANDARD-1.0`
Baseline commit: `main` = `d4cfc0b`; release tag `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1` = `af3381a`

## 1. Baseline Verification (repository-grounded)

All package assumptions in Sections 3–5 of the work-order package were verified against the actual repository. No material assumption is stale; this checkpoint proceeds without a design discrepancy report.

- Release tag `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1` exists (`af3381a`); governance tag `ENGINEERING-WORKFLOW-STANDARD-1.0` exists; `main` = `d4cfc0b`. Worktree clean except two untracked source PDFs in `docs/`.
- Current aggregates/interfaces/repositories referenced by the package exist: `KnowledgeDocument`, `KnowledgeDocumentRevision`, `KnowledgeCitation`, `KnowledgeRelationship`, `PromotionIdentity`/`PromotionRecord`/`PromotionAttempt`/`PromotionDiagnostic`/`PromotionProvenance` (all in `src/SPINbuster.Domain/KnowledgeEngine.cs` and `PromotionEngine.cs`), `IAuthorityPolicy` (`src/SPINbuster.Domain/IAuthorityPolicy.cs`), `IUnitOfWork`, `IClock`, `ICurrentUser`, `IAuditRecorder` (`src/SPINbuster.Application/Abstractions/`).
- Migration count is 16 forward migrations (`src/SPINbuster.Infrastructure/Persistence/Migrations/`), unchanged since release. Migration integrity guards exist: `ReleasedKnowledgeEngineMigrationFilesRemainByteStable` (`tests/SPINbuster.Infrastructure.Tests/SqliteKnowledgeEnginePersistenceTests.cs:25`, SHA-256 `86F56D32...`, `835C1089...`) and `NoReleasedMigrationDrift` in `SqliteParsingPersistenceTests.cs:759`, `SqlitePromotionDiagnosticPersistenceTests.cs:687`, `SqlitePromotionProvenancePersistenceTests.cs`.
- Test baseline passes 754/754: Domain 234, Application 233, Documents 78, Infrastructure 104, Architecture 24, AI 6, Desktop 75 (`dotnet test --no-build -m:1`, validated). Build 0 errors; `dotnet format --verify-no-changes` clean; only pre-existing CA1848 warnings.
- No intervening releases or governance changes since the baseline. `.ai/current-priority.md` records `ENGINEERING-ASSERTION-PROMOTION-0.1-RC` as planned but inactive; WO0 is the next action.
- `SPINbuster.Rules` exists but is empty (project-only, `src/SPINbuster.Rules/SPINbuster.Rules.csproj`); the Rule Engine is deferred by design. Rule boundary spec `spec/rules/rule-engine-boundary.md` (Review Candidate, baseline `ENGINEERING-KNOWLEDGE-MODEL-0.1-RC`) confirms Rules owns deterministic evaluation; assertions belong upstream.
- `EDR-KE-011-engineering-assertion-promotion.md` is Deferred and lists the exact open items this package resolves (identity/versioning, supersession/retraction, verification workflow, parsed/AI-proposed promotion, assertion-observation-requirement interaction).

## 2. Architecture Gate — why a new aggregate is required

The gate requires documenting why existing components cannot be extended without weakening ownership or invariants.

- **`KnowledgeDocumentRevision` cannot hold assertions.** It preserves authoritative source content (content/metadata hashes, source authority, revision lifecycle). It has no subject/predicate/value typing, no unit/measurement representation, no qualifier/modality, no comparison semantics. Adding assertion semantics would corrupt the knowledge-preservation invariant.
- **`KnowledgeRelationship` cannot hold assertions.** It links two `KnowledgeSubjectReference`s (Document/Revision/ImportedSource only) with a single `KnowledgeRelationshipType` and an evidence string. It cannot carry normalized semantic content, identity, lifecycle, or comparison classification. Reusing `Contradicts`/`Supersedes` (both already exist, `KnowledgeRelationshipType`) as the sole conflict carrier would produce relationship explosions without deterministic uniqueness (Watch Item in WO4) and would erase classification/abstention semantics.
- **`FragmentCandidate` cannot be an authority source.** It is non-authoritative by design (`FragmentCandidateReviewState.Generated`/`HumanAccepted`/`Rejected`, `src/SPINbuster.Domain/ParserEngine.cs:407`). Package constraint 4 forbids assertions consuming raw parser output.
- **`PromotionRecord`/`PromotionIdentity` are fragment-promotion-specific**, but their *shape* is the correct reusable template: identity hash + `ContractVersion`, identity-based replay (`TryReplayByIdentityAsync`), immutable attempt history, provenance, and atomic staging.
- **`IAuthorityPolicy` is fragment-typed** (`Classify(FragmentCandidate, ProjectId)`). It cannot classify an assertion without distortion; this is the one abstraction where the package's "extend where possible" guidance may fail (see Section 7, decision to be finalized in WO1).

Conclusion: a new Domain aggregate (`EngineeringAssertion` and supporting value objects) is required, plus assertion-specific identity, promotion record/attempt, evidence, and conflict representations. No new project is required: the existing `SPINbuster.Domain`, `Application`, `Infrastructure`, and `Desktop` projects absorb the slice. The empty `SPINbuster.Rules` project stays untouched (deferred).

## 3. Ownership model (9 concerns)

| Concern | Owner | Reuse / evidence |
|---|---|---|
| Identity | Domain | Pattern: `PromotionIdentity.ComputeHash` (`PromotionEngine.cs:94`) and `KnowledgeDocumentIdentity.ComputeHash` (`KnowledgeEngine.cs:126`) — trim + `ToUpperInvariant` normalization, `"|"`-joined SHA-256 hex, explicit `ContractVersion`. |
| Lifecycle | Domain | Pattern: `KnowledgeRevisionLifecycle` + `PromoteToCurrentAuthoritative`/`MarkSuperseded` guards; `LifecycleTransitionException`; append-only audit via `AuditableEntity`. |
| Authority | Domain (interface) + Application (policy) | `IAuthorityPolicy`/`AuthorityPolicyResult(EffectiveAuthorityLevel, AuthorityBasis)`; `KnowledgeSourceAuthorityLevel` enum; `PolicyVersion` string; `AuthorityPolicy` stub (`src/SPINbuster.Application/AuthorityPolicy.cs`). Assertion classification is a new entry point (Section 7). |
| Evidence/provenance | Domain (value/entity) + Application (verification) | Pattern: `KnowledgeCitation` (revision-bound, locator, content hash) and `PromotionProvenance` full chain (revision→diagnostic→fragment→parser→source→identity→attempt→actor→authority). |
| Replay | Application (use case) over Domain identity | Pattern: `TryReplayByIdentityAsync` + `IPromotionRecordRepository.FindByIdentityHashAsync` + `GetLatestSuccessfulByRecordIdAsync` (`PromoteFragmentCandidateUseCase.cs:715`). |
| Conflict/comparison | Domain (classification contract) + Application (orchestration), WO4 | Existing `PromotionConflictType` and `KnowledgeRelationshipType.Contradicts` are directional inputs; the closed deterministic comparison contract is defined in WO1 and implemented in WO4, never as rule evaluation. |
| Transactions | Infrastructure | `SqliteUnitOfWork` (explicit transaction, staged audit flush, deferred-reference two-pass save, `ConcurrencyConflictException` on stale context). |
| Query bounding | Application | Pattern: `LoadPromotionAttemptsUseCase` (project ownership check, deterministic ordering, `MaxResults`, bounded `FailureSummary`); `LoadPromotionProvenanceUseCase` (keyed, single record). |
| Layer enforcement | Architecture tests | `DependencyGraphTests.cs` exact project set + `AllowedProjectReferences`; any new references are within the existing allowed set. |

## 4. Proposed assertion identity inputs and normalization risks

Identity inputs (per package Section 5, finalized in WO1): project; normalized subject; predicate; normalized value representation; qualifier/modality where semantically relevant; source revision/evidence context (source `KnowledgeDocumentRevision` and supporting citation/evidence); assertion contract version.

Normalization risks (carried to WO1 spec examples):
- Predicate canonicalization (synonyms/aliases) vs. erasing meaning — under/over-normalization is a known risk (package Section 7).
- Value representation: units, case, trailing zeros, sign conventions; unit conversion that changes meaning is out of scope beyond approved normalization.
- Qualifier/modality: excluding a semantically relevant qualifier causes identity drift; including display-only text causes duplicate identity.
- Source context binding: including the source revision in identity means the same statement sourced from a different revision is a different assertion (intended, per package); the citation/evidence reference must be stable (revision-bound, not raw fragment).
- Text normalization: follow the repository convention (trim, `ToUpperInvariant`, exact `"|"` join, SHA-256 hex) plus explicit case/whitespace policy; unicode normalization must be pinned explicitly.

## 5. Lifecycle, supersession, replay, concurrency semantics

- **Lifecycle:** immutable after creation except explicit Domain-governed transitions (package Section 5). Initial authoritative + superseded + withdrawn/retracted disposition; corrections produce superseding assertions or explicit dispositions, never mutation. `EDR-KE-011` and `EDR-KE-008` (Deferred) are the placeholders resolved in WO1.
- **Supersession:** requires higher authority or explicit governed human action; prior assertion history preserved. Knowledge `SupersedeCurrentRevision` (single-UoW) is the template; equal/higher-authority silent supersession rejected (mirror `HigherAuthorityExists` check in `PromoteFragmentCandidateUseCase.cs:224`).
- **Replay:** successful replay is `AssertionPromotionIdentity`-based only, checked before mutable eligibility, idempotent (no new assertion/evidence/conflict/audit rows). Every non-replay execution gets a distinct immutable attempt/outcome; failures never reuse prior diagnostics by candidate/evidence ID.
- **Concurrency:** optimistic concurrency via `ConcurrencyToken`; true stale-context tests; on commit failure rollback + `DiscardChanges` and clear staged state (lessons from `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1` WO6/WO8).
- **Atomicity:** all success artifacts (assertion, evidence, provenance, conflicts, attempts, audits) staged for one atomic `SqliteUnitOfWork` commit.

## 6. Authority, evidence, and promotion-record requirements

- Assertion authority is derived by policy and cannot be caller-escalated; knowledge authority is an input, never automatic inheritance (package constraint 4).
- Every successful promotion records: reviewer identity, review timestamp, authority basis, authority-policy version, source `KnowledgeDocumentRevision`, supporting citation/evidence, canonical `AssertionIdentity`, promotion/attempt history (package Section 5).
- The reviewer is presented with the canonical assertion and its supporting authoritative evidence before promotion is recorded.
- AI, parsing, normalization, and extraction cannot bypass assertion review.

## 7. Design alternatives and recommendation

| Alternative | Decision | Rationale |
|---|---|---|
| Model assertions as `KnowledgeRelationship` | **Rejected** | No semantic content, no identity, no lifecycle/comparison; contradiction/supersession would need a parallel classification layer (Section 2). |
| Model assertions as revision metadata/hash extensions | **Rejected** | Corrupts knowledge-preservation invariant; revision is source-of-record. |
| Reuse `PromotionRecord`/`PromotionIdentity` as-is for assertions | **Rejected** | Fragment-specific fields and identity formula (includes `FragmentIdentityKey`); assertion identity has different inputs. Reuse the *shape* (new `AssertionPromotionIdentity` with own `ContractVersion`). |
| Extend `IAuthorityPolicy.Classify` to accept a general candidate input | **Open (WO1)** | Existing signature is fragment-typed; generalizing the parameter risks weakening the fragment contract. Preferred direction: add a second, assertion-specific classification entry point on a Domain-owned authority abstraction, keeping `PolicyVersion`/`AuthorityBasis` output shape; finalize in WO1 with a decision record. |
| One unified `EngineeringAssertion` aggregate + promotion record/attempt/evidence/conflict value objects | **Accepted (recommended)** | Matches established aggregate/port/persistence conventions; single replay path; minimal surface. |
| Separate `IEngineeringAssertionRepository` vs. fold into knowledge repositories | **Accepted (recommended)** | Assertion repo separate (its own identity/query bounds); conflict/evidence repositories separate only where WO4/WO1 justify distinct ownership (package Section 6). |
| Persist promotion identity/record/attempt/provenance in-memory | **Rejected** | Existing `IPromotionRecordRepository` is `InMemoryPromotionRecordRepository` ("temporary, will be replaced by EF Core"); assertion slice must persist identity/record/attempt/provenance through SQLite from WO3. |
| Rule Engine (Rules project) evaluation of assertions in this slice | **Deferred** | Rules project stays empty; WO4 comparison is a closed deterministic matrix, not rule evaluation. |
| AI assertion extraction/proposal in this slice | **Deferred** | Package explicit deferral; WO1 documents the future non-authoritative proposal path only. |

## 8. Minimum persisted entity set (vertical slice)

Recommendation (finalized by WO1/WO3): `EngineeringAssertion` (aggregate), `AssertionPromotionIdentity`/record (identity-unique), `AssertionPromotionAttempt` (immutable attempts), `AssertionEvidence` (revision + citation binding), `AssertionConflict` (durable classification/abstention), plus `AuditableEntity` audit events through the existing audit pipeline. Provenance fields may fold into the promotion record rather than a separate record, mirroring `PromotionProvenance` if WO1 keeps the full chain requirement.

## 9. Public surface (recommendation)

- Domain: `EngineeringAssertionId`, `AssertionPromotionIdentity`, `EngineeringAssertion`, value objects (subject, predicate, value representation, unit/measurement, qualifier/modality), `AssertionEvidence`, `AssertionLifecycle`, `AssertionConflictType`/conflict record.
- Application: `IEngineeringAssertionRepository`; `PromoteEngineeringAssertion` command/use case/result; `LoadEngineeringAssertionSnapshot` bounded query; authority classification entry point (Section 7).
- Infrastructure: records, converters, repositories, one forward migration after design approval, migration-integrity guard.
- Desktop: workflow runner/result/formatter using Application DTOs only.
- No new project. No Rule Engine changes.

## 10. Risk register (top items; full list per package Section 7)

1. Second knowledge model instead of a normalized assertion layer — mitigated by Section 2 gate.
2. Identity drift from mutable/display text or omitted qualifiers — WO1 normalization spec + identity test matrix.
3. Caller-controlled authority escalation — Domain-derived authority, no authority command parameter.
4. Replay keyed on source IDs/diagnostics instead of immutable identity — single `AssertionPromotionIdentity` replay path.
5. Linking assertions to non-authoritative fragments — evidence bound to revision/citation only.
6. Automatic contradiction resolution — WO4 abstention (`ComparisonUnsupported`/`ReviewRequired`), no winner selection.
7. Partial persistence — one atomic `SqliteUnitOfWork` commit; rollback tests.
8. Fresh-schema-only migration testing — historical-schema upgrade fixtures required (WO3).
9. Unbounded snapshots / raw source leakage — bounded, sanitized DTOs (no source text/paths).
10. Documentation overclaiming Rule Engine capability — RC review distinguishes deferred work.

## 11. Recommended WO1 boundaries

- Freeze `AssertionPromotionIdentity` formula + normalization rules (spec examples: equivalent input → same identity; distinct qualifier/unit/modality → different identity).
- Define lifecycle transition table (including invalid transitions) and supersession/retraction vs. mutation.
- Define authority matrix and promotion-record completeness; finalize the authority classification entry point (Section 7) with a decision record.
- Define the closed deterministic comparison contract (predicates, value types, canonical units, equality/tolerance, modality, applicability/scope, temporal, abstention) — WO4 implements only this.
- Define evidence requirements (revision + ≥1 citation/evidence), provenance chain, and bounded/sanitized query contracts.
- Recommendation: **WO1 requires an EDR update.** Resolve `EDR-KE-011` (Engineering Assertion Promotion) from Deferred to an adopted decision; create/adopt an assertion-identity normalization decision record (analogous to `EDR-DE-006` for fragment identity contract-version choice) and a closed-comparison-contract decision record; update `EDR-KE-008` linkage as supersession semantics are finalized.

## 12. Unresolved questions for architectural review

1. Should `IAuthorityPolicy` gain a second assertion-specific entry point (recommended) or be generalized to a candidate-agnostic input? The existing fragment-typed signature is the blocker.
2. Does the source revision/evidence context belong inside `AssertionPromotionIdentity` (package says yes), and if so should the citation reference or the revision reference be the canonical binding?
3. Should provenance be a separate record (mirroring `PromotionProvenance`) or folded into the promotion record given the field overlap?
4. Which lifecycle disposition set (Draft/CurrentAuthoritative/Superseded/Withdrawn/Retracted) matches the closed comparison contract without over-engineering WO1?
5. Is a separate conflict repository justified, or does a project-scoped query over assertion + conflict records suffice for WO4?

## 13. Repository-grounded implementation map (WO1→WO5)

| WO | Layer | Concrete anchors to extend/reuse |
|---|---|---|
| WO1 | specs/EDRs | `spec/knowledge/*.md` + `EDR-KE-011`; follow `spec/rules/rule-engine-boundary.md` ownership split |
| WO2 | Domain + Application | `AuditableEntity`/`AuditEvent`; `DomainGuards`; ID record-struct pattern (`KnowledgeIds.cs`, `PromotionEngine.cs`); `PromoteFragmentCandidateUseCase` orchestration + replay + staging template; `LoadPromotionAttemptsUseCase`/`LoadPromotionProvenanceUseCase` query/bounds pattern; `ServiceCollectionExtensions` DI (Domain/Application services only) |
| WO3 | Infrastructure | `SpinbusterModelConfiguration` (ConfigureXxx sections), `InfrastructureMapper`, `StronglyTypedIdValueConverters`, `SqliteUnitOfWork`, `KnowledgeDocumentDeferredReferenceHandler`, forward migration + integrity guard tests |
| WO4 | Domain/Application/Infrastructure | Closed comparison contract from WO1; conflict classification at promotion (atomic); conflict snapshot query; concurrency/rollback tests |
| WO5 | Desktop | `KnowledgePromotionWorkflowRunner` composition host pattern, bootstrapper/formatter/result using Application DTOs; repeated-run + provider-recreation proof; RC review |

## 14. Decision list summary

- Accepted: new `EngineeringAssertion` aggregate; assertion-specific identity/promotion-record/attempt/evidence/conflict; one atomic UoW; SQLite persistence of the full assertion graph (incl. identity/attempts) from WO3; bounded sanitized queries; closed deterministic comparison with abstention; no new project; Rules deferred.
- Rejected: `KnowledgeRelationship`-as-assertion; revision-metadata-as-assertion; reusing `PromotionIdentity` formula directly; in-memory persistence of identity/attempts; AI/parsing bypass of review; automatic contradiction resolution.
- Deferred: AI assertion proposals/extraction; unit-conversion engine; Rule Engine evaluation; contradiction auto-resolution; semantic retrieval; UI.

## 15. Completion report

- Files changed: one new document (`docs/02-engineering-design/ENGINEERING-ASSERTION-PROMOTION-0.1-RC-WO0-SLICE-DESIGN-BRIEF.md`). No production code, test code, schema, migration, project, DI, continuity, or release-state changes.
- Behavior implemented: none (read-only checkpoint per WO0).
- Design deviations: none; all package assumptions verified current.
- Tests added: none; baseline revalidated at 754/754 passing by project.
- Validation results: `dotnet restore` OK; `dotnet format --verify-no-changes` clean (pre-existing CA1848 only); `dotnet build` 0 errors; `dotnet test --no-build -m:1` 754/754; `git diff --check` clean.
- Migration status: 16 released migrations byte-stable, unchanged; no new migration.
- Known limitations/deferred: Rule Engine, AI extraction, unit-conversion engine, contradiction auto-resolution, UI (package Section 2 deferrals); `EDR-KE-011` resolution deferred to WO1.
- Confirmation: no tag, no release, no next work order, and no commit were made; awaiting architectural review with a clean worktree (only this brief plus the two pre-existing untracked source PDFs).
