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
- **`IAuthorityPolicy` is fragment-typed** (`Classify(FragmentCandidate, ProjectId)`). It cannot classify an assertion without distortion; this is the one abstraction where the package's "extend where possible" guidance fails. Direction: introduce an assertion-specific authority contract (tentatively `IAssertionAuthorityPolicy`) rather than generalizing the fragment-typed policy (Section 7).

Conclusion: a new Domain aggregate (`EngineeringAssertion` and supporting value objects) is required, plus assertion-specific identity, promotion record/attempt, evidence, and conflict representations. No new project is required: the existing `SPINbuster.Domain`, `Application`, `Infrastructure`, and `Desktop` projects absorb the slice. The empty `SPINbuster.Rules` project stays untouched (deferred).

## 3. Ownership model (9 concerns)

| Concern | Owner | Reuse / evidence |
|---|---|---|
| Identity | Domain | Pattern: `PromotionIdentity.ComputeHash` (`PromotionEngine.cs:94`) and `KnowledgeDocumentIdentity.ComputeHash` (`KnowledgeEngine.cs:126`) — trim + `ToUpperInvariant` normalization, `"|"`-joined SHA-256 hex, explicit `ContractVersion`. |
| Lifecycle | Domain | Pattern: `KnowledgeRevisionLifecycle` + `PromoteToCurrentAuthoritative`/`MarkSuperseded` guards; `LifecycleTransitionException`; append-only audit via `AuditableEntity`. |
| Authority | Domain (interface) + Application (policy) | `IAuthorityPolicy`/`AuthorityPolicyResult(EffectiveAuthorityLevel, AuthorityBasis)`; `KnowledgeSourceAuthorityLevel` enum; `PolicyVersion` string; `AuthorityPolicy` stub (`src/SPINbuster.Application/AuthorityPolicy.cs`). Assertion classification uses a separate assertion-specific contract (tentatively `IAssertionAuthorityPolicy`) reusing the effective authority/authority basis/policy version concepts (Section 7). |
| Evidence/provenance | Domain (value/entity) + Application (verification) | Pattern: `KnowledgeCitation` (revision-bound, locator, content hash) and `PromotionProvenance` full chain (revision→diagnostic→fragment→parser→source→identity→attempt→actor→authority). |
| Replay | Application (use case) over Domain identity | Pattern: `TryReplayByIdentityAsync` + `IPromotionRecordRepository.FindByIdentityHashAsync` + `GetLatestSuccessfulByRecordIdAsync` (`PromoteFragmentCandidateUseCase.cs:715`). |
| Conflict/comparison | Domain (classification contract) + Application (orchestration), WO4 | Existing `PromotionConflictType` and `KnowledgeRelationshipType.Contradicts` are directional inputs; the closed deterministic comparison contract is defined in WO1 and implemented in WO4, never as rule evaluation. |
| Transactions | Infrastructure | `SqliteUnitOfWork` (explicit transaction, staged audit flush, deferred-reference two-pass save, `ConcurrencyConflictException` on stale context). |
| Query bounding | Application | Pattern: `LoadPromotionAttemptsUseCase` (project ownership check, deterministic ordering, `MaxResults`, bounded `FailureSummary`); `LoadPromotionProvenanceUseCase` (keyed, single record). |
| Layer enforcement | Architecture tests | `DependencyGraphTests.cs` exact project set + `AllowedProjectReferences`; any new references are within the existing allowed set. |

## 4. Proposed assertion identity concepts, inputs, and normalization risks

The identity model is split into two distinct concepts (finalized in WO1; exact names non-binding until then):

**`AssertionIdentity`** — represents canonical engineering meaning.
- Includes: project; normalized subject; predicate; typed canonical value; canonical unit where applicable; modality; applicability/scope; semantic qualifiers; assertion semantic contract version.
- Excludes: source revision IDs, citation IDs, reviewer IDs, timestamps, and all other provenance data.
- Multiple authoritative evidence bindings may support one semantic assertion without producing different `AssertionIdentity` values; evidence is provenance, not meaning.

**`AssertionPromotionIdentity`** — represents the governed promotion operation and the successful replay key.
- Includes: `AssertionIdentity` plus source `KnowledgeDocumentRevision`, supporting citation/evidence identity, project, and promotion contract version.
- Successful promotion replay uses `AssertionPromotionIdentity` only.

Normalization risks (carried to WO1 spec examples):
- Predicate canonicalization (synonyms/aliases) vs. erasing meaning — under/over-normalization is a known risk (package Section 7).
- Value representation: units, case, trailing zeros, sign conventions; unit conversion that changes meaning is out of scope beyond approved normalization.
- Qualifier/modality: excluding a semantically relevant qualifier causes identity drift; including display-only text causes duplicate identity.
- Semantic/provenance separation: provenance data (revision, citation, reviewer, timestamp) must never enter `AssertionIdentity`; `AssertionPromotionIdentity` binds evidence without blurring semantic meaning.
- Text normalization: follow the repository convention (trim, `ToUpperInvariant`, exact `"|"` join, SHA-256 hex) plus explicit case/whitespace policy; unicode normalization must be pinned explicitly.

## 5. Lifecycle, supersession, replay, concurrency semantics

- **Lifecycle:** the minimum authoritative lifecycle is `CurrentAuthoritative`, `Superseded`, `Withdrawn`. There is no `Draft` `EngineeringAssertion`: a non-authoritative pre-promotion object is an assertion proposal/candidate, not a Draft assertion. `EngineeringAssertion` is created as authoritative only through the governed human promotion workflow. `Retracted` is added only if WO1 defines a distinct invariant and audit meaning from `Withdrawn`. Corrections create superseding assertions; they do not mutate canonical assertion meaning. `EDR-KE-011` and `EDR-KE-008` (Deferred) are the placeholders resolved in WO1.
- **Supersession:** requires higher authority or explicit governed human action; prior assertion history preserved. Knowledge `SupersedeCurrentRevision` (single-UoW) is the template; equal/higher-authority silent supersession rejected (mirror `HigherAuthorityExists` check in `PromoteFragmentCandidateUseCase.cs:224`).
- **Replay:** successful replay is `AssertionPromotionIdentity`-based only, checked before mutable eligibility, idempotent (no new assertion/evidence/conflict/audit rows). Every non-replay execution gets a distinct immutable attempt/outcome; failures never reuse prior diagnostics by candidate/evidence ID. Promotion replay/attempt records stay conceptually separate from immutable assertion provenance.
- **Concurrency:** optimistic concurrency via `ConcurrencyToken`; true stale-context tests; on commit failure rollback + `DiscardChanges` and clear staged state (lessons from `FRAGMENT-TO-KNOWLEDGE-PROMOTION-0.1` WO6/WO8).
- **Atomicity:** all success artifacts (assertion, evidence, provenance, conflicts, attempts, audits) staged for one atomic `SqliteUnitOfWork` commit.

## 6. Authority, evidence, and promotion-record requirements

- Assertion authority is derived by policy and cannot be caller-escalated; knowledge authority is an input, never automatic inheritance (package constraint 4).
- Every successful promotion records: reviewer identity, review timestamp, authority basis, authority-policy version, source `KnowledgeDocumentRevision`, supporting citation/evidence, canonical `AssertionIdentity` (within the recorded `AssertionPromotionIdentity`), promotion/attempt history (package Section 5).
- The reviewer is presented with the canonical assertion and its supporting authoritative evidence before promotion is recorded.
- AI, parsing, normalization, and extraction cannot bypass assertion review.

## 7. Design alternatives and recommendation

| Alternative | Decision | Rationale |
|---|---|---|
| Model assertions as `KnowledgeRelationship` | **Rejected** | No semantic content, no identity, no lifecycle/comparison; contradiction/supersession would need a parallel classification layer (Section 2). |
| Model assertions as revision metadata/hash extensions | **Rejected** | Corrupts knowledge-preservation invariant; revision is source-of-record. |
| Reuse `PromotionRecord`/`PromotionIdentity` as-is for assertions | **Rejected** | Fragment-specific fields and identity formula (includes `FragmentIdentityKey`); assertion identity has different inputs. Reuse the *shape* (new `AssertionIdentity` + `AssertionPromotionIdentity`, each with its own `ContractVersion`). |
| Generalize `IAuthorityPolicy.Classify` to a candidate-agnostic input | **Rejected** | The existing signature is fragment-typed; generalizing it risks weakening the fragment contract. Do not generalize unless WO1's Architecture Gate proves both workflows share one actual invariant. |
| Introduce assertion-specific authority contract `IAssertionAuthorityPolicy` | **Accepted (preferred direction)** | New assertion-specific classification contract, tentatively `IAssertionAuthorityPolicy`. Reuses existing authority result concepts (effective authority, authority basis, policy version). Policy input supports canonical assertion semantics, source knowledge authority, evidence, reviewer authorization, and project policy — no fragment-specific fields. WO1 finalizes the exact contract and decision record. |
| One unified `EngineeringAssertion` aggregate + promotion record/attempt/evidence/conflict value objects | **Accepted (recommended)** | Matches established aggregate/port/persistence conventions; single replay path; minimal surface. |
| Separate `IEngineeringAssertionRepository` vs. fold into knowledge repositories | **Accepted (recommended)** | Assertion repo separate (its own identity/query bounds); a separate conflict repository is deferred behind WO4's Architecture Gate unless conflict lifecycle/query ownership proves it necessary (package Section 6). |
| Persist promotion identity/record/attempt/provenance in-memory | **Rejected** | Existing `IPromotionRecordRepository` is `InMemoryPromotionRecordRepository` ("temporary, will be replaced by EF Core"); assertion slice must persist identity/record/attempt/provenance through SQLite from WO3. |
| Rule Engine (Rules project) evaluation of assertions in this slice | **Deferred** | Rules project stays empty; WO4 comparison is a closed deterministic matrix, not rule evaluation. |
| AI assertion extraction/proposal in this slice | **Deferred** | Package explicit deferral; WO1 documents the future non-authoritative proposal path only. |

## 8. Minimum persisted entity set (vertical slice)

Recommendation (finalized by WO1/WO3): `EngineeringAssertion` (aggregate, authoritative only through promotion), `AssertionIdentity` + `AssertionPromotionIdentity`/record (identity-unique replay key), `AssertionPromotionAttempt` (immutable attempts), `AssertionEvidence` (revision + citation binding; multiple bindings may support one semantic assertion), plus `AuditableEntity` audit events through the existing audit pipeline. `AssertionConflict` representation is a WO4 concern and is deferred behind WO4's Architecture Gate. Provenance follows the released knowledge promotion provenance chain (imported-source provenance is traced through `PromotionProvenance` rather than duplicated as independently trusted source fields); promotion replay/attempt records remain conceptually separate from immutable assertion provenance. A separate conflict repository is deferred unless WO4 proves conflict lifecycle/query ownership requires it.

## 9. Public surface (recommendation)

- Domain: `EngineeringAssertionId`, `EngineeringAssertion`, `AssertionIdentity`, `AssertionPromotionIdentity`, value objects (subject, predicate, typed canonical value, canonical unit, modality, applicability/scope, semantic qualifiers), `AssertionEvidence`, `AssertionLifecycle` (`CurrentAuthoritative`, `Superseded`, `Withdrawn`), `AssertionConflictType`/conflict record (WO4).
- Application: `IEngineeringAssertionRepository`; `PromoteEngineeringAssertion` command/use case/result; `LoadEngineeringAssertionSnapshot` bounded query; assertion-specific authority contract (tentatively `IAssertionAuthorityPolicy`), reusing effective authority, authority basis, and policy version concepts (Section 7).
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

- Freeze the split identity model: `AssertionIdentity` (canonical meaning) and `AssertionPromotionIdentity` (promotion operation + replay key) formulas and normalization rules. Spec examples: equivalent normalized input → same identity; distinct qualifier/unit/modality → different identity; multiple evidence bindings → same `AssertionIdentity`.
- Define the minimum authoritative lifecycle transition table (`CurrentAuthoritative`, `Superseded`, `Withdrawn`; invalid transitions) and supersession vs. mutation. Decide whether `Retracted` has a distinct invariant and audit meaning from `Withdrawn`.
- Define the authority matrix and promotion-record completeness; finalize the assertion-specific authority contract (tentatively `IAssertionAuthorityPolicy`) with a decision record. Do not generalize the fragment-typed `IAuthorityPolicy` unless WO1's Architecture Gate proves both workflows share one actual invariant.
- Define the closed deterministic comparison contract (predicates, value types, canonical units, equality/tolerance, modality, applicability/scope, temporal, abstention) — WO4 implements only this.
- Define evidence requirements (revision + ≥1 citation/evidence; multiple bindings per semantic assertion), provenance chain through the released knowledge promotion provenance chain, and bounded/sanitized query contracts.
- Recommendation: **WO1 requires an EDR update.** Resolve `EDR-KE-011` (Engineering Assertion Promotion) from Deferred to an adopted decision; create/adopt an assertion-identity normalization decision record (analogous to `EDR-DE-006` for fragment identity contract-version choice) and a closed-comparison-contract decision record; update `EDR-KE-008` linkage as supersession semantics are finalized.

## 12. Unresolved questions for architectural review

1. Should `Retracted` be included, or does `Withdrawn` already carry the required invariant and audit meaning? WO1 decides only if a distinct `Retracted` semantics is justified.
2. For `AssertionPromotionIdentity`, should the supporting citation/evidence reference or the source `KnowledgeDocumentRevision` be the canonical binding, and how are multiple evidence bindings represented without multiplying promotion identity?
3. Where exactly does the assertion-specific authority contract (`IAssertionAuthorityPolicy`) live (Domain interface vs. Application policy) while preserving the released `IAuthorityPolicy` untouched?
4. Should provenance be a separate record (mirroring `PromotionProvenance`) or folded into the promotion record given the field overlap, while tracing imported-source provenance through the released knowledge promotion provenance chain?
5. Is a separate conflict repository justified, or does a project-scoped query over assertion + conflict records suffice for WO4? Deferred behind WO4's Architecture Gate.

## 13. Repository-grounded implementation map (WO1→WO5)

| WO | Layer | Concrete anchors to extend/reuse |
|---|---|---|
| WO1 | specs/EDRs | `spec/knowledge/*.md` + `EDR-KE-011`; follow `spec/rules/rule-engine-boundary.md` ownership split |
| WO2 | Domain + Application | `AuditableEntity`/`AuditEvent`; `DomainGuards`; ID record-struct pattern (`KnowledgeIds.cs`, `PromotionEngine.cs`); `PromoteFragmentCandidateUseCase` orchestration + replay + staging template; `LoadPromotionAttemptsUseCase`/`LoadPromotionProvenanceUseCase` query/bounds pattern; `ServiceCollectionExtensions` DI (Domain/Application services only) |
| WO3 | Infrastructure | `SpinbusterModelConfiguration` (ConfigureXxx sections), `InfrastructureMapper`, `StronglyTypedIdValueConverters`, `SqliteUnitOfWork`, `KnowledgeDocumentDeferredReferenceHandler`, forward migration + integrity guard tests |
| WO4 | Domain/Application/Infrastructure | Closed comparison contract from WO1; conflict classification at promotion (atomic); conflict snapshot query; concurrency/rollback tests |
| WO5 | Desktop | `KnowledgePromotionWorkflowRunner` composition host pattern, bootstrapper/formatter/result using Application DTOs; repeated-run + provider-recreation proof; RC review |

## 14. Decision list summary

- Accepted: new `EngineeringAssertion` aggregate; distinct `AssertionIdentity` and `AssertionPromotionIdentity`; assertion-specific authority-policy direction (tentatively `IAssertionAuthorityPolicy`, reusing effective authority/authority basis/policy version); minimum authoritative lifecycle (`CurrentAuthoritative`/`Superseded`/`Withdrawn`, no `Draft` assertion); multiple evidence bindings per semantic assertion; one atomic UoW; SQLite persistence of the full assertion graph (incl. identity/attempts) from WO3; bounded sanitized queries; closed deterministic comparison with abstention; no new project; Rules deferred. Exact names remain non-binding until WO1.
- Rejected: `KnowledgeRelationship`-as-assertion; revision-metadata-as-assertion; reusing `PromotionIdentity` formula directly; generalizing the fragment-typed `IAuthorityPolicy`; `Draft` `EngineeringAssertion` state; in-memory persistence of identity/attempts; AI/parsing bypass of review; automatic contradiction resolution.
- Deferred: AI assertion proposals/extraction; unit-conversion engine; Rule Engine evaluation; contradiction auto-resolution; separate conflict repository (behind WO4's Architecture Gate); semantic retrieval; UI.

## 15. Completion report

- Files changed: one new document (`docs/02-engineering-design/ENGINEERING-ASSERTION-PROMOTION-0.1-RC-WO0-SLICE-DESIGN-BRIEF.md`). No production code, test code, schema, migration, project, DI, continuity, or release-state changes.
- Behavior implemented: none (read-only checkpoint per WO0).
- Design deviations: none; all package assumptions verified current.
- Tests added: none; baseline revalidated at 754/754 passing by project.
- Validation results: `dotnet restore` OK; `dotnet build` 0 errors; `dotnet test --no-build -m:1` 754/754; `git diff --check` clean. Note: `dotnet format --verify-no-changes` exits 2 on this Windows worktree because `* text=auto` normalizes committed files to LF while the worktree holds CRLF; the 13 flagged files are untouched production sources (no WO0 change). The repository's recorded validation command is in-place `dotnet format SPINbuster.sln --no-restore`.
- Migration status: 16 released migrations byte-stable, unchanged; no new migration.
- Known limitations/deferred: Rule Engine, AI extraction, unit-conversion engine, contradiction auto-resolution, UI (package Section 2 deferrals); `EDR-KE-011` resolution deferred to WO1.
- Confirmation: this checkpoint made no tag, no release, no next work order, and no code/test/migration/DI/continuity changes; the brief was committed and pushed to `origin/main` (`fc473b4`) for review at the requester's direction, and is now presented corrected for final WO0 approval.
