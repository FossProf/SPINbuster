# EDR-APP-002

Title: Draft-generation ownership

Status: Accepted for APPLICATION-0.1

Decision:

`GenerateReportDraftRequest` assembles source material only.
It does not create, persist, approve, or otherwise authoritatively change a `Report`.

Current rule:

- `GenerateReportDraftRequest` remains a query.
- Future authoritative `Report` creation will be implemented as a command.

Context:

The current Application boundary is intended to supply drafting context to a later
drafting service without implying that a durable report record already exists.

Consequences:

- Query handlers stay side-effect free for this drafting boundary.
- Future report creation can evolve independently as a command with explicit persistence.
