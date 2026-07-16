# EDR-DE-003: Import Session Atomicity

Status: Accepted

Decision:
Document import sessions are additive and per-source, not all-or-nothing batch transactions.

Consequence:

- one failed source does not erase prior successful imports in the same session
- import-session counts are the durable batch summary
- later executable workflows may layer stronger operator controls without changing the stored import model
