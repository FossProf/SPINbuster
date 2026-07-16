# Prompt Contracts

Purpose: Define the required prompt inputs, outputs, and invariants for AI interactions.

## Inputs

- prompt package ID
- prompt package semantic version
- assigned model role
- governed prompt context text
- context-manifest hash
- output schema ID and version
- optional generation parameters such as temperature

## Outputs

- structured JSON matching `report-draft-proposal.schema.json`
- provider and model identity metadata
- token counts and latency when available
- failure classification when generation does not succeed

## Invariants

- prompt packages are resolved by ID and semantic version.
- only `Approved` prompt packages may be used for proposal generation.
- prompt packages declare required output schema and context-policy versions.
- prompt packages define allowed provider capabilities.
- prompt text must not grant authority to approve, issue, or persist reports directly.
