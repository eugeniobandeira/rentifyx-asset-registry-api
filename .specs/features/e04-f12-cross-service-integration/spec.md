# Spec: F-12 Cross-Service Integration

## Summary

Two independent Kafka consumer `BackgroundService`s close out E-04/M4. **Consumer A** subscribes to
`identity-api`'s `UserSuspended`/`UserDeleted` events and maintains a DynamoDB-backed local owner-status
cache (single-table, same `AssetRegistry` table as everything else) so `IOwnerStatusValidator` — already
injected into `CreateAssetHandler` but never implemented — has a real, first-ever implementation.
**Consumer B** subscribes to `rentifyx-ai-services`' `AssetMediaModerated` event and calls the existing
`ApplyModerationVerdictHandler` (F-09, Application layer, already built and tested) per message. Both
consumers are net-new: no `Confluent.Kafka` *consumer* exists anywhere in this repo today, only the
`OutboxPublisher` *producer* (F-10). This closes M4 — no other E-04 work remains after this feature.

## Domain / Module

Both consumers are cross-cutting integration concerns, not Domain logic — they live in
`02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/`, alongside `OutboxPublisher`, per the existing
precedent (background Kafka services are an Api-layer concern in this repo, not Infrastructure/IoC).
Consumer A additionally needs one new Infrastructure piece: `DynamoDbOwnerStatusValidator`, implementing
the pre-existing Domain contract `IOwnerStatusValidator` (`Domain/Interfaces/Asset/IOwnerStatusValidator.cs`).
Consumer B needs zero new Domain/Application code — `ApplyModerationVerdictHandler`/`Request`/
`ModerationVerdict` already exist and are unchanged by this feature.

## Interface Contract

Not a REST API — both are background `IHostedService`s with no HTTP surface. No new `/api/v1/*` endpoint
is added by this feature (matches every other Application-layer feature so far — API endpoints for the
whole repo remain deferred to E-05, ROADMAP M5).

| Consumer | Inbound topic(s) | Triggers |
|---|---|---|
| A — `OwnerStatusConsumer` | `user-lifecycle-events` (single shared topic, both events) | Upsert into DynamoDB owner-status cache |
| B — `ModerationVerdictConsumer` | `asset-media-moderated` | `ApplyModerationVerdictHandler.HandleAsync` |

**Verified against the real sibling-repo code (not guessed)**, correcting this spec's own earlier draft:
- `identity-api` does **not** have a `UserDeleted` event — the real name is `UserAccountDeleted`
  (`RentifyxIdentity.Domain.Events.UserAccountDeleted`). Both it and `UserSuspended` are published to a
  single generic topic, `user-lifecycle-events` (not two topics as originally assumed), wrapped in a shared
  envelope (see Request/Input below).
- `identity-api`'s own docs flag this envelope/topic as **"no consumer exists yet... deliberately simple"**
  (`UserLifecycleEventEnvelope.cs`'s own doc comment, and `.specs/features/outbox-kafka-notifications/design.md`).
  This consumer will be the **first real consumer** of that topic — the contract should be treated as young
  and not contractually hardened on identity-api's side yet, worth a heads-up to that team rather than
  assumed stable.
- `identity-api`'s `OutboxPublisher` producing this topic could not be confirmed as fully built in that
  repo's `02-src` at time of research (only the envelope-building factory was found, covered by tests; the
  actual Kafka publish step was only found referenced in that repo's design doc, not as a completed file) —
  worth confirming this is live before relying on it end-to-end.
- `rentifyx-ai-services`' real topic is literally `asset-media-moderated` (env-var override
  `KAFKA_MODERATED_TOPIC`, default matches), not the `ai-services.asset-media-moderated` dotted form this
  repo's own `KafkaTopics` naming convention would suggest — **ai-services does not follow this repo's
  `{service}.{event}` topic-naming convention**, so the constant added to `KafkaTopics` here must hold the
  literal producer-side string, not a locally-invented name.

## Request / Input

### Consumer A — inbound event contract (envelope + nested payload, both verified against real `identity-api` code)

Outer envelope (`user-lifecycle-events` topic, both event types share this shape):

| Field | Type | Notes |
|---|---|---|
| `EventType` | string | `"UserSuspended"` or `"UserAccountDeleted"` (exact `GetType().Name` values) — this repo's consumer switches on this to pick the inner DTO shape |
| `AggregateId` | `Guid` | the user id — same value as the inner payload's `UserId`, redundant but present at envelope level too |
| `OccurredAt` | `DateTimeOffset` | event time |
| `Data` | nested JSON object | the actual event payload, shape depends on `EventType` (below) |

Inner payload when `EventType == "UserSuspended"`:

| Field | Type |
|---|---|
| `UserId` | `Guid` |
| `Reason` | string |
| `OccurredAt` | `DateTimeOffset` |

Inner payload when `EventType == "UserAccountDeleted"`:

| Field | Type |
|---|---|
| `UserId` | `Guid` |
| `OccurredAt` | `DateTimeOffset` |

No `SchemaVersion` field exists anywhere in this envelope — `identity-api` has no schema-version concept for
this topic (confirmed, not assumed). A version-mismatch guard (as Consumer B has) is therefore not
applicable to Consumer A; malformed/unrecognized `EventType` values are the only "unknown shape" guard
needed.

### Consumer B — inbound event contract (verified against real `rentifyx-ai-services` code, flat, no envelope)

Published as a bare top-level JSON object (`Verdict` serialized as its string name via `JsonStringEnumConverter`,
Kafka message key = `AssetId.ToString()`):

| Field | Type | Used by this consumer? |
|---|---|---|
| `AssetId` | `Guid` | Yes — maps to `ApplyModerationVerdictRequest.AssetId` |
| `Verdict` | string enum: `Approved`/`PendingReview`/`Rejected` | Yes — maps to `ApplyModerationVerdictRequest.Verdict` (existing `ModerationVerdict` enum, same 3 names, confirmed identical) |
| `Labels` | array of `{ Name: string, Confidence: float }` | No — not part of `ApplyModerationVerdictHandler`'s input surface. Deserialized (so the DTO is complete/valid) but not forwarded or persisted. |
| `TopConfidence` | float | No — deserialized but unused, same reasoning as `Labels`. |
| `Timestamp` | `DateTimeOffset` | No — used only for structured logging (message age / lag visibility), not passed to the handler. |
| `Bucket` | string | No — S3 bucket the moderated media lives in. Not previously known (missing from this repo's prior-session summary and from ai-services' own stale design doc) — deserialized but unused; this repo has no reason to touch S3 from this consumer. |
| `Key` | string | No — S3 object key, same reasoning as `Bucket`. |
| `SchemaVersion` | int, current real value **`2`** | Used for a version-mismatch guard (see Handler/Service Logic) — **not `1`** as this repo's earlier, unverified assumption and ai-services' own stale design doc both said; the shipped code is the source of truth. Guard should reject/log-and-skip on any value other than `2`. |

## Response / Output

Neither consumer produces an HTTP response or a return value consumed elsewhere — success is "message
processed, offset committed"; failure is "message not committed, will be redelivered on restart/rebalance"
(see Error Cases for the retry/DLQ posture).

## Data Model Changes

### New DynamoDB item: `OwnerStatusItem` (Consumer A only)

Single-table design, same `AssetRegistry` table (ADR-AR-009 precedent — no new table). New POCO in
`02-src/05-Infrastructure/.../Persistence/Items/OwnerStatusItem.cs`, new item-type discriminator
`DynamoDbKeys.OwnerStatusType = "OwnerStatus"`, new mapper `OwnerStatusDynamoDbMapper` (`ToItem`/`ToEntity`
static pair, following `AssetDynamoDbMapper`/`OutboxDynamoDbMapper` precedent exactly — explicit attribute
name constants, no auto-mapping).

| Field | Type | Notes |
|---|---|---|
| `Pk` | string | `OWNERSTATUS#{ownerId}` — **deliberately a new prefix, not the existing `OwnerPrefix = "OWNER#"`**, since that prefix is already in use as `AssetItem`'s GSI1 owner-index key for a different purpose (listing an owner's assets); reusing it for a distinct item type in the same table risks a PK/GSI1PK collision. Resolves the collision question the research flagged. |
| `Sk` | string | `METADATA` (single item per owner, no range needed — matches `CategoryItem`'s single-item-per-entity shape, not `AssetItem`'s multi-item-per-entity shape) |
| `OwnerId` | `Guid` | |
| `IsActive` | bool | `false` after `UserSuspended` or `UserDeleted`, `true` is never written by this consumer (no `UserReactivated` event exists in `identity-api` per the plan — once flipped false, stays false; this is intentional, not a gap) |
| `Reason` | string | `"Suspended"` or `"Deleted"` — which event caused the flip, for observability |
| `UpdatedAt` | `DateTimeOffset` | from the event's own `Timestamp`, not `DateTimeOffset.UtcNow` — preserves event ordering info even if consumer processes out of order after a restart |

No entity/aggregate is added to the Domain layer for this — `IOwnerStatusValidator.IsOwnerActiveAsync`
returns a plain `bool`, so `OwnerStatusItem` is Infrastructure-only, never crosses into Domain/Application.

### No data model changes for Consumer B

`ApplyModerationVerdictHandler` and everything it touches (`AssetEntity`, `ModerationVerdict`) already
exists, unchanged.

Migration: not applicable (DynamoDB, no schema migration mechanism — new item type just starts appearing
under its own PK prefix in the existing table).

## Validation Rules

| Field | Rule | Source / Constant |
|---|---|---|
| Consumer A: `UserId` | must deserialize as non-empty `Guid`; malformed message → log + skip (see Error Cases), never throw unhandled | new |
| Consumer B: `AssetId` | must deserialize as non-empty `Guid` | mirrors `ApplyModerationVerdictValidator`'s existing rule, applied again pre-handler-call since the Kafka payload is a new untrusted boundary distinct from the validator's existing HTTP/direct-call boundary |
| Consumer B: `Verdict` | must map to one of `ModerationVerdict`'s 3 known names; unknown string → log + skip, do not guess-default to a verdict | new |
| Consumer B: `SchemaVersion` | must match the single version this consumer understands; mismatch → log + skip, not a crash | new — exact expected value is an open question until `ai-services`' real contract is confirmed |

## Handler / Service Logic

### Consumer A — `OwnerStatusConsumer : BackgroundService`

1. On start: build `IConsumer<string,string>` via `ConsumerBuilder`, subscribe to both
   `identity-api.user-suspended` and `identity-api.user-deleted` topics (one consumer, two topics — simpler
   than two separate hosted services for two events that both just flip the same cache field).
2. Loop: `consumer.Consume(stoppingToken)` (blocking pull, standard Confluent.Kafka consumer loop — distinct
   shape from `OutboxPublisher`'s `PeriodicTimer` push loop, since this is consume-not-produce).
3. Deserialize message value by topic (which topic tells us which event/DTO shape to expect).
4. Malformed payload → log error with message key/offset, **commit the offset anyway** (poison-pill message,
   retrying it forever would block the whole partition — see Error Cases) — continue loop.
5. Valid payload → create a DI scope (`IServiceScopeFactory`, same pattern Consumer B needs, since
   `IAmazonDynamoDB`/DynamoDB context are scoped, not singleton, per existing `AddDynamoDb` registration),
   resolve a new scoped `IOwnerStatusCacheWriter` (or reuse `DynamoDbOwnerStatusValidator` itself if it
   exposes both read and write — see Repository Interfaces below), upsert `OwnerStatusItem` with
   `IsActive = false`.
6. On successful write: commit offset (`consumer.Commit()` or auto-commit disabled, explicit commit —
   matches "at-least-once, only advance after durable write" semantics, consistent with `OutboxPublisher`'s
   own "only mark published after Kafka ack" ordering principle applied in reverse).
7. On DynamoDB write failure: do **not** commit offset, log, let the loop retry same message next
   `Consume()` call (transient AWS errors should be retried, not skipped like a poison pill) — bounded by
   Kafka's own redelivery, no custom retry-count/backoff loop needed for a first pass.
8. `StopAsync` override: `consumer.Close()` (commits final offsets, leaves consumer group cleanly) — mirrors
   `OutboxPublisher.StopAsync`'s producer-flush precedent but for a consumer.

### Consumer B — `ModerationVerdictConsumer : BackgroundService`

1. Same consumer-loop shape as Consumer A, subscribed to `ai-services.asset-media-moderated` only.
2. Deserialize `AssetMediaModerated` DTO. `SchemaVersion` mismatch or malformed JSON → log + skip + commit
   (poison pill, same reasoning as Consumer A step 4).
3. Map `Verdict` string → `ModerationVerdict` enum. Unknown value → log + skip + commit.
4. Create DI scope, resolve `IHandler<ApplyModerationVerdictRequest, AssetModerationResponse>` (already
   registered — no Application DI changes needed), call
   `HandleAsync(new ApplyModerationVerdictRequest(AssetId, Verdict), stoppingToken)`.
5. Handler already handles the "not currently PendingModeration" case as an idempotent no-op internally (F-09)
   — so a redelivered/duplicate message after a crash-before-commit is safe without any consumer-side
   dedup logic. This is why fail-closed retry-without-commit (step 7 pattern from Consumer A) is safe to
   reuse here too.
6. `result.Match`: success → commit offset. `ErrorOr` failure → inspect error type:
   - `Error.NotFound` (asset doesn't exist) → this is a poison pill (retrying won't make the asset appear)
     → log + commit, do not retry forever.
   - Any other error type → do not commit, let Kafka redeliver (matches Consumer A's transient-failure
     posture).
7. `StopAsync`: `consumer.Close()`, same as Consumer A.

### Shared: `IOwnerStatusValidator` implementation (Consumer A's Infrastructure side)

`DynamoDbOwnerStatusValidator.IsOwnerActiveAsync(ownerId, ct)`:
1. `GetItemAsync` for `PK=OWNERSTATUS#{ownerId}, SK=METADATA`.
2. **Item not found → return `false` (fail-closed)** — this is the confirmed decision (open question in
   STATE.md, now resolved): an owner with no cache entry (never-suspended, or event hasn't arrived yet
   after a fresh deploy/backfill gap) is treated as *not verified active*, so `CreateAssetHandler` rejects
   asset creation via the existing `Error.Forbidden(AssetErrorCodes.OwnerNotActive, ...)` path. This is a
   real behavior change worth flagging loudly: **on first deploy of this feature, with an empty cache, every
   `CreateAsset` call will be rejected until `identity-api` republishes a full snapshot or every owner has
   triggered at least one no-op sync.** Backfill strategy is an explicit open question below, not solved by
   this consumer alone (it only handles the live event stream, not a historical backfill).
3. Item found → return `Item.IsActive`.

## Error Cases

| Scenario | Handling | Committed? |
|---|---|---|
| Malformed/undeserializable Kafka message (either consumer) | Log error (key, offset, topic, raw value truncated), skip | Yes — poison pill, must not block partition |
| Unknown `Verdict` string / unknown event-type discriminator | Log warning, skip | Yes — same poison-pill reasoning |
| `SchemaVersion` mismatch | Log warning, skip | Yes |
| DynamoDB write failure (Consumer A) | Log error | **No** — retry via redelivery |
| `ApplyModerationVerdictHandler` returns `Error.NotFound` (Consumer B) | Log warning | Yes — asset will never exist on retry |
| `ApplyModerationVerdictHandler` returns any other `ErrorOr` failure (Consumer B) | Log error | **No** — retry |
| Consumer crashes mid-message (process kill, OOM) | No commit happened yet → redelivered on restart. `ApplyModerationVerdictHandler`'s existing idempotency (F-09) and this consumer's upsert-only writes (Consumer A) make redelivery safe without extra dedup. | N/A |
| Kafka broker unavailable at startup | `BackgroundService.ExecuteAsync` should not crash the whole host — `Consume()` throwing `KafkaException` transient errors should be caught, logged, and retried with a short delay inside the loop (not exit the loop/service) | N/A |

**Open question `[non-blocking]`:** no dead-letter-queue topic is specified in the plan or either sibling
repo's precedent. Poison-pill messages are currently just logged-and-dropped, not preserved anywhere for
later inspection. Acceptable for a first pass (matches this repo's overall "no DLQ infra exists yet"
reality) but worth a Known Gap entry in STATE.md once this ships, not silently accepted.

## Testing Strategy

**Unit tests** (`03-tests/03-Handlers/` doesn't apply — no `IHandler` is new; instead new test project or
folder for consumer message-handling logic, isolated from the real `IConsumer<,>`):
- Consumer A: valid `UserSuspended` payload → cache writer called with `IsActive=false, Reason="Suspended"`
- Consumer A: valid `UserDeleted` payload → cache writer called with `IsActive=false, Reason="Deleted"`
- Consumer A: malformed payload → cache writer never called, offset still committed (assert via mock consumer's `Commit` call)
- Consumer B: `Approved` verdict → `IHandler.HandleAsync` called with correctly-mapped `ApplyModerationVerdictRequest`
- Consumer B: unknown verdict string → handler never called, message skipped
- Consumer B: handler returns `Error.NotFound` → offset committed despite failure
- Consumer B: handler returns `Error.Validation`/other → offset NOT committed
- `DynamoDbOwnerStatusValidator.IsOwnerActiveAsync`: item not found → `false`; item found with `IsActive=false` → `false`; item found with `IsActive=true` → `true` (this case only reachable if a future feature ever writes `true` — worth a unit test anyway since the mapper must round-trip it correctly even if this consumer never sets it)

**Integration tests** (`03-tests/04-Repositories/`, Testcontainers — `Testcontainers.Kafka` already a
`PackageVersion` in `Directory.Packages.props`, unused until now):
- `OwnerStatusConsumer` against a real Kafka + LocalStack DynamoDB: publish a `UserSuspended` message,
  assert the item lands in DynamoDB with correct PK/SK/attributes.
- `ModerationVerdictConsumer` against real Kafka: publish an `AssetMediaModerated` message for a
  `PendingModeration` asset seeded in DynamoDB first, assert the asset transitions to `Active` (Approved
  case) end-to-end through the real handler + real repository.

**End-to-end tests:** not applicable — no HTTP surface for this feature.

## Repository Interfaces (Clean Architecture supplement)

New Infrastructure-only interface, **not** exposed in `Domain/Interfaces/` since it's not a Domain contract
consumed by Application handlers (unlike `IOwnerStatusValidator`, which already exists and stays as-is):

```
internal interface IOwnerStatusCacheWriter
{
    Task UpsertAsync(Guid ownerId, bool isActive, string reason, DateTimeOffset updatedAt, CancellationToken ct = default);
}
```

`DynamoDbOwnerStatusValidator` may implement **both** `IOwnerStatusValidator` (Domain-facing read) and this
new writer interface (Infrastructure-facing write) on the same class — avoids a second DynamoDB client
wrapper for what is, physically, one item type. Open to a design-time judgment call during Execute rather
than a hard requirement of this spec.

## Mapper

`OwnerStatusDynamoDbMapper` — `ToItem(Guid ownerId, bool isActive, string reason, DateTimeOffset updatedAt) -> OwnerStatusItem`,
`ToAttributeMap(OwnerStatusItem) -> Dictionary<string,AttributeValue>`, `FromAttributeMap(...) -> OwnerStatusItem`,
`Item.IsActive` accessor for the validator's read path. Follows `AssetDynamoDbMapper`/`OutboxDynamoDbMapper`
precedent exactly (explicit attribute-name constants, no reflection-based auto-mapping).

## IoC / DI Registration

- `InfrastructureDependencyInjection.AddDynamoDb`: add
  `services.AddScoped<IOwnerStatusValidator, DynamoDbOwnerStatusValidator>()` (this single registration is
  what unblocks `CreateAssetHandler`'s currently-unregistered dependency — a real, if currently silent, gap
  on `main` today per research finding #9).
- New `02-src/01-Api/.../Extensions/CrossServiceConsumingExtensions.cs` (or two smaller extension methods),
  mirroring `OutboxServiceCollectionExtensions`: registers `IConsumer<string,string>` instances (one per
  consumer, since each needs its own `ConsumerConfig.GroupId` — Kafka consumer groups are per-logical-
  consumer, sharing a group across Consumer A/B would incorrectly load-balance unrelated topics) via
  `ConsumerConfig { BootstrapServers = configuration[ConfigurationKeys.KafkaBootstrapServers], GroupId = "..." , AutoOffsetReset = Earliest, EnableAutoCommit = false }` — explicit `EnableAutoCommit = false` is
  required for the "commit only after successful write" semantics in Handler/Service Logic above.
- New `ConfigurationKeys` entries for each consumer's `GroupId` (e.g. `Kafka:OwnerStatusConsumer:GroupId`,
  `Kafka:ModerationVerdictConsumer:GroupId`), reusing the existing `Kafka:BootstrapServers` key.
- `Program.cs`: `builder.Services.AddCrossServiceConsuming(builder.Configuration); builder.Services.AddHostedService<OwnerStatusConsumer>(); builder.Services.AddHostedService<ModerationVerdictConsumer>();`
- New topic constants in `KafkaTopics` (inbound side; existing 4 constants are all outbound):
  `UserLifecycleEvents = "user-lifecycle-events"`, `AssetMediaModerated = "asset-media-moderated"` —
  literal producer-side strings, verified against real code, not locally-invented names.
- New `DynamoDbKeys.OwnerStatusType`/`OwnerStatusPrefix`/`OwnerStatusKey(Guid)` following the existing
  key-builder pattern.

## Open Questions

- ~~`[blocking]` Exact Kafka topic name strings~~ — **RESOLVED this session**: `user-lifecycle-events`
  (identity-api, both events) and `asset-media-moderated` (ai-services), verified against real producer code.
- ~~`[blocking]` Exact `UserSuspended`/`UserDeleted` field/envelope shape~~ — **RESOLVED this session**: see
  Request/Input above (envelope + per-event inner payload, and the real name is `UserAccountDeleted`).
- ~~`[blocking]` Exact `AssetMediaModerated` `SchemaVersion` value~~ — **RESOLVED this session**: `2`, not `1`.
- `[non-blocking]` `identity-api`'s `user-lifecycle-events` topic is explicitly documented on that repo's side
  as having no consumer yet and a deliberately loose contract, and its `OutboxPublisher` producing step
  could not be fully confirmed as built/live in that repo at research time. Worth a quick check-in with
  whoever owns `identity-api` before this consumer ships to production, and worth re-verifying the topic is
  actually receiving messages during Consumer A's integration testing rather than assuming.
- `[non-blocking]` No DLQ topic/strategy for poison-pill messages — accepted as a first-pass gap, log Known
  Gap in STATE.md once shipped.
- `[non-blocking]` Backfill strategy for the owner-status cache on first deploy (empty cache = every
  `CreateAsset` rejected until organically synced) — needs a decision (request `identity-api` publish a full
  snapshot? seed cache as `IsActive=true` for all pre-existing owners as a one-time migration? accept the
  cold-start rejection window?) but does not block *building* the consumer itself, only its safe rollout.
- `[non-blocking]` Whether `DynamoDbOwnerStatusValidator` implements both read (`IOwnerStatusValidator`) and
  write (`IOwnerStatusCacheWriter`) on one class or two — judgment call during Execute.
