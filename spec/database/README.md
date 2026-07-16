# Database Specifications

## Purpose

`spec/database/` contains authoritative persistence specifications for durable storage owned by `SPINbuster.Infrastructure`.

These specifications define storage shape, migration expectations, and persistence boundaries. They do not authorize Application, AI, Documents, Reporting, Desktop, or Server code to bypass repository contracts.

## Current scope

- Local SQLite persistence is the active durable path.
- EF Core migrations are authoritative for schema evolution.
- Domain models remain persistence-free.
- Application contracts remain provider-neutral.

## Active specifications

- `spec/database/knowledge-engine-persistence.md`

## Current release boundary

The latest released executable baseline remains `AI-PROPOSAL-EXECUTABLE-SLICE-0.1`.

The active review candidate is `KNOWLEDGE-ENGINE-PERSISTENCE-0.1-RC`.
