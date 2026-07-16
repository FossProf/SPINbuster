# EDR-DE-002: Project-Scoped Import Records And Global Storage Dedup

Status: Accepted

Decision:
Imported document sources remain project-scoped records.

Storage objects may be deduplicated globally by exact content hash, algorithm, and version.

Consequence:

- cross-project metadata remains isolated
- exact duplicate storage reuse is allowed
- cross-project duplicate checks must not reveal another project's descriptive metadata
