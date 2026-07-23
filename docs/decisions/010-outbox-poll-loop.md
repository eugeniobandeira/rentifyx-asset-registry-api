# ADR-AR-010: Outbox Delivery via Poll-Loop, Not DynamoDB Streams

- **Date:** 2026-07-23
- **Status:** Accepted (supersedes an earlier informal assumption in the plan doc)

## Context

The plan document originally described the Outbox mechanism as "DynamoDB Streams... consistent
with identity-api's DEF-005." That text was wrong: research into the actual `rentifyx-identity-api`
repo (done at the start of this E-04 session, not guessed) found that identity-api's real DEF-005
**rejected** DynamoDB Streams for Outbox delivery — its own environment has no Lambda/compute
available to attach a Streams consumer to, so it built a `PeriodicTimer` poll-loop `IHostedService`
(`OutboxPublisher`, living in `Api/Messaging/`, no separate Outbox table) instead. This repo's plan
text cited the wrong resolution of that decision. Since this repo's EKS/Fargate deployment has the
same shape (a long-running API process, no Lambda), the same constraint applies here.

## Options Considered

- **Option A — DynamoDB Streams + Lambda consumer.** What the plan doc originally said. Requires a
  Lambda function (or equivalent event-driven compute) subscribed to the table's stream — this repo
  has no Lambda in its deployment target (EKS/Fargate only), so this would mean introducing a new
  compute primitive solely for outbox delivery.
- **Option B — `PeriodicTimer` poll-loop `IHostedService`, same as identity-api.** No new compute
  primitive — runs inside the existing API pod. Outbox entries live as regular items in the same
  single table (`OUTBOX#{id}` partition, see ADR-AR-009), not a separate DynamoDB Streams-enabled
  table.

## Decision

**Option B.** `OutboxPublisher : BackgroundService` in `02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/`,
polling every `Outbox:PollIntervalSeconds` (default 5s, configurable via `OutboxOptions`/`IOptions<T>`
since it's a DI-constructed hosted service). Each tick:

1. `QueryAsync` GSI1 `GSI1PK = OUTBOX_STATUS#Pending`, batch-limited (`Outbox:BatchSize`).
2. Zero pending entries → skip Kafka entirely, loop back to the next tick (no wasted broker round-trip).
3. Per entry: resolve the Kafka topic from `EventType` (`Domain/Constants/KafkaTopics.cs`), publish
   the already-serialized JSON payload keyed by `AssetId` (ordering per asset within a partition).
4. On publish success: flip `Status` to `Published` and drop `GSI1PK` (item falls out of the index,
   cheaper than a second query filtering `Status` client-side).
5. On publish failure: increment `RetryCount`; below `Outbox:MaxRetries` (3), stays `Pending` and is
   retried next tick; at/above the limit, `Status = Failed` (also drops `GSI1PK`) + `LogCritical`.
6. `StopAsync` drains one last in-flight batch under the same cancellation token, then flushes the
   Kafka producer — matches identity-api's own drain pattern.

The entity save and its outbox writes are atomic: `DynamoDbAssetRepository.SaveAsync` builds one
`TransactWriteItem` per domain event plus the entity's own `Put`, in a single `TransactWriteItemsAsync`
call (DynamoDB transactions cap at 100 items — more than 100 raised events on one save throws, a
programmer-error guard, not an expected runtime case).

## Consequences

- No Lambda, no Streams-enabled table, no extra IAM surface for a stream consumer — the outbox is
  just more items in the same table this repo already reads/writes.
- Delivery latency is bounded by the poll interval (default 5s) rather than being near-instant like a
  Streams trigger would be — acceptable for this repo's event types (`AssetCreated`/
  `AssetMediaUploaded`/`AssetPublished`/`AssetSuspended`), none of which have a sub-5-second latency
  requirement documented anywhere in the plan.
- If the API pod restarts mid-batch, in-flight (not-yet-`Published`) entries stay `Pending` and are
  picked up by the next pod's next tick — at-least-once delivery, not exactly-once; downstream
  consumers of these Kafka topics must already tolerate duplicate events (this is the same guarantee
  identity-api's own poll-loop provides).
- The plan doc's original "DynamoDB Streams... consistent with identity-api's DEF-005" text was
  factually wrong about what identity-api actually did — corrected here and in this repo's
  `.specs/project/STATE.md`/`ROADMAP.md`, which had repeated the same wrong claim before this ADR
  was written.
