# AI Specification

`spec/ai/` defines how AI works inside SPINbuster.

## Governing Rule

`.ai/` tells an AI agent how to work on SPINbuster.
`spec/ai/` defines how AI works inside SPINbuster.

## Purpose

This directory contains the authoritative engineering design for AI behavior, boundaries, contracts, evaluation, and provider integration.

## Governing Principle

SPINbuster shall remain operationally correct without AI. AI output is advisory until explicitly accepted through deterministic Application workflows.

## Scope

This layer defines:

- model responsibilities
- prohibited actions
- schemas and structured contracts
- provider abstractions
- lifecycle states
- security boundaries
- evaluation thresholds
- confidence handling
- audit behavior

## Usage

Agents should read only the documents relevant to the task they are performing.

## Current Implemented Baseline

`AI-DRAFT-PROPOSAL-SLICE-0.1` is the latest released baseline and `AI-PROPOSAL-EXECUTABLE-SLICE-0.1-RC` currently extends it with:

- provider-neutral generation contracts in `SPINbuster.Application`
- deterministic Tier 0 provider and prompt-package registry in `SPINbuster.AI`
- governed report-draft context manifests
- durable model-run and proposal records in SQLite
- structured proposal validation against the report-draft proposal schema
- non-authoritative AI proposal persistence, review loading, and rejection workflow
- deterministic Desktop-host execution of proposal request, replay, review action, and failure-display workflows

Not implemented in this slice:

- Ollama or any live model provider
- AI-authored authoritative report revisions
- approval, issuance, export, or synchronization behavior
