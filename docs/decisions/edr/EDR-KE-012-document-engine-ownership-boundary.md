# EDR-KE-012: Document Engine Ownership Boundary

Status: Accepted

Applies to:
`ENGINEERING-KNOWLEDGE-MODEL-0.1-RC`

Decision:
The Document Engine owns binary-source handling and non-authoritative processing outputs only.

It may produce:

- immutable import records
- content hashes
- processing attempts
- fragment candidates
- citation candidates
- relationship candidates
- assertion candidates

It may not directly create authoritative Knowledge Engine facts without an Application workflow, deterministic validation, and explicit human review where required.

Consequence:

- parser adapters stay outside the authoritative knowledge core
- OCR or extraction success never implies authority
- authoritative promotion remains a governed Application workflow
