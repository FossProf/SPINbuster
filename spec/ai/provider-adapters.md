# Provider Adapters

Purpose: Describe provider abstraction boundaries and adapter expectations for AI backends.

## Supported Providers

- `tier0-deterministic`
  - local deterministic fixture provider
  - no network dependency
  - used for repeatable Tier 0 validation and tests
- Live providers are out of scope for `AI-DRAFT-PROPOSAL-SLICE-0.1-RC`.

## Adapter Contract

- Application depends on a provider-neutral `IAiGenerationProvider`.
- Adapters must expose:
  - provider identifier
  - model name
  - immutable model digest
  - supported capability set
  - structured generation operation with cancellation support
  - deterministic failure classification
  - token and latency metadata when available

## Portability Rules

- `SPINbuster.Application` must not reference provider SDK types.
- `SPINbuster.AI` must not reference `SPINbuster.Infrastructure`, `SPINbuster.Server`, or `SPINbuster.Desktop`.
- AI adapters may not call authoritative persistence repositories directly.
- AI provider output must flow back through Application validation and commit boundaries.
