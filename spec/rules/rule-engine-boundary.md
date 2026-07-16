# Rule Engine Boundary

Status: Review Candidate
Baseline: `ENGINEERING-KNOWLEDGE-MODEL-0.1-RC`

## Purpose

The Rule Engine owns deterministic evaluation logic derived from authoritative requirements or approved organizational policy.

It does not own engineering truth. It operationalizes truth that is already governed elsewhere.

## Boundary rules

- requirements are knowledge
- rules are executable deterministic logic
- rules must carry source citations
- applicability must be explicit
- rules are versioned
- evaluation results are durable
- indeterminate states remain first-class
- human override requires justification
- audit is mandatory
- AI recommendations do not replace deterministic rule evaluation

## Conceptual model

A deterministic rule conceptually supports:

- rule ID
- rule version
- authoritative source citations
- applicability
- required inputs
- severity
- evaluation semantics
- pass, fail, or indeterminate outcomes

A rule evaluation result conceptually supports:

- result ID
- rule ID and version
- evaluated subject
- inputs used
- output state
- supporting citations
- override state
- override justification
- audit metadata

## Ownership split

`SPINbuster.Rules` owns:

- rule definitions
- rule evaluators
- result shaping

`SPINbuster.Application` owns:

- orchestration
- transaction boundaries
- promotion and review workflows

`SPINbuster.Domain` owns:

- authoritative requirement and observation semantics

`SPINbuster.AI` may:

- propose rule candidates
- explain rule results

It may not replace deterministic evaluation.
