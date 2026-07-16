# EDR-DE-001: Storage Ownership And Orphan Cleanup

Status: Accepted

Decision:
Original bytes are preserved through provider-neutral `StorageObject` records plus immutable content-store adapters.

Database transactions do not wrap large binary writes.

Consequence:

- storage happens before the relational commit when a new object is required
- orphaned storage objects are possible if the relational commit fails after successful storage
- reconciliation and cleanup remain operational concerns and do not block the foundation baseline
