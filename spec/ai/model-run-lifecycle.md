# Model Run Lifecycle

Purpose: Specify lifecycle states, transitions, and checkpoints for AI model execution.

## States

- `Requested`
- `ContextBuilding`
- `ContextValidated`
- `Queued`
- `Running`
- `OutputReceived`
- `SchemaValidating`
- `PolicyValidating`
- `ReadyForHumanReview`
- `ReviewCompleted`
- `Abstained`
- `Failed`
- `Closed`

## Transitions

- `Requested -> ContextBuilding`
- `ContextBuilding -> ContextValidated`
- `ContextValidated -> Queued`
- `Queued -> Running`
- `Running -> OutputReceived`
- `OutputReceived -> SchemaValidating`
- `SchemaValidating -> PolicyValidating`
- `PolicyValidating -> ReadyForHumanReview`
- `PolicyValidating -> Abstained`
- `ContextValidated -> Abstained`
- `ContextValidated | Queued | Running | OutputReceived | SchemaValidating | PolicyValidating -> Failed`
- `ReadyForHumanReview -> ReviewCompleted`
- `ReviewCompleted | Abstained | Failed -> Closed`

## Failure Handling

- Failed runs persist their trace metadata and attempts.
- Retry and fallback attempts must be distinct `ModelRunAttempt` records under the originating `ModelRun`.
- Invalid structured output may produce a non-reviewable proposal plus a failed model run.
- Failure telemetry must be committed through the same transaction boundary as other durable AI substrate records.
- Provider invocation must occur outside the database transaction that records the requested run state.
