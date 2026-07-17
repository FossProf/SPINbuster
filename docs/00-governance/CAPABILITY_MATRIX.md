# Capability Matrix

Purpose: Define single-engine ownership for major platform capabilities.

Rule:
Every capability belongs to exactly one engine.
Collaboration is expected, but ownership overlap is not.

| Engine | Owns | Never Owns |
| --- | --- | --- |
| Storage Engine | Immutable object storage, storage object identity, durability metadata, corruption detection, storage availability state | Knowledge authority, report authority, parsing semantics, AI proposal lifecycle |
| Document Engine | Import sessions, imported document sources, duplicate detection, processing-attempt lifecycle, non-authoritative document candidate lifecycle | Knowledge revisions, rule evaluations, authoritative reports, provider execution policy |
| Parsing Engine | Parser orchestration, parse outputs, parser-attempt outcomes, normalized structural parse results | Authoritative fragments, knowledge promotion, report approval, raw storage durability |
| Fragment Engine | Fragment candidates, fragment locators, reviewed fragment metadata, fragment provenance to source material | Knowledge authority, final rule results, report issuance, provider transport |
| Knowledge Engine | Knowledge documents, knowledge revisions, knowledge relationships, contradictions, citations, supersession rules | Raw byte storage, parser execution, AI provider execution, presentation behavior |
| Rule Engine | Rule definitions, rule evaluations, deterministic findings, evidence-backed rule results | Knowledge creation, AI-generated conclusions, presentation state, sync transport |
| Retrieval Engine | Deterministic retrieval, semantic retrieval supplementation, result ranking, bounded traversal, retrieval result shaping | Authority promotion, final report ownership, provider orchestration, storage durability |
| Context Engine | Context manifests, source inclusion policy, manifest hashes, replayable context scope, truncation policy | Provider execution, knowledge promotion, report issuance, raw retrieval indexes |
| AI Proposal Layer | AI proposals, proposal provenance, proposal review state, proposal linkage to context and sources | Model transport execution, authoritative reports, authoritative knowledge, provider SDK isolation |
| AI Execution Engine | Model runs, model-run attempts, execution outcomes, retry and failure handling, structured output execution routing | Engineering authority, report approval, fragment promotion, presentation logic |
| Provider Adapters | Provider-specific translation, SDK isolation, capability normalization, transport boundary implementations | Business orchestration, authority decisions, report revisions, project knowledge ownership |
| Reporting Engine | Reports, report revisions, report sections, report provenance, issuance and export boundaries | Raw storage, parser execution, AI authority, sync conflict policy |
| Synchronization Engine | Sync operations, cursors, conflict records, reconciliation state, exchange protocol boundaries | Local business truth ownership, rule evaluation, UI workflows, knowledge authority definitions |
| Presentation | User interaction, workflow presentation, composition roots, platform-specific UX behavior | Business truth, persistence ownership, engineering authority, provider execution contracts |

## Notes

- In the layered architecture, concrete implementations of these engines live in outer adapters such as Infrastructure, Documents, AI, Reporting, and future synchronization adapters.
- Provider Adapters describe an adapter role within capability architecture, not a separate dependency layer that overlaps Infrastructure ownership.
- Capability ownership does not imply a direct project reference.
- Engines may collaborate across Application workflows while still maintaining single-engine ownership.
- Semantic retrieval belongs to the Retrieval Engine as a supplement to deterministic retrieval, not as a replacement for it.
- Human review remains the authority boundary even when Fragment, Knowledge, Reporting, or AI-related engines collaborate, but review still operates through governed validation, provenance, scope, and lifecycle rules.
