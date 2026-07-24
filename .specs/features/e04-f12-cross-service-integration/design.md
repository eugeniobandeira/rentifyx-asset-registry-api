# Design: F-12 Cross-Service Integration

PROJECT CONTEXT
- Language / framework: .NET 10, Minimal APIs, C# latest
- Architectural pattern: Clean Architecture (Domain → Infrastructure → Application → IoC → API → Tests)
- Modules: Domain (`03-Domain`), Application (`02-Application`), Infrastructure (`05-Infrastructure`), IoC (`04-IoC`), Api (`01-Api`)
- DI pattern: `InfrastructureDependencyInjection.Register`/`ApplicationDependencyInjection.Register` (flat extension methods, no assembly scanning except `IEndpoint`); background services wired directly in `Program.cs`
- Error pattern: `ErrorOr<T>`, `Error.Forbidden`/`Error.NotFound`/`Error.Validation` factories
- Test framework: xUnit + FluentAssertions + Moq (unit), Testcontainers (integration) — `03-tests/04-Repositories` already has a `KafkaFixture` (`Testcontainers.Kafka`, `apache/kafka:3.7.0`) used by `OutboxPublisherTests`, reusable as-is for this feature's consumer integration tests

## Architecture Overview

Two new `BackgroundService` classes join the existing `OutboxPublisher` under `Api/Messaging/`. Both use
`Confluent.Kafka`'s `IConsumer<string,string>` (net-new in this repo — only a producer existed before),
each with its own consumer group so their partition assignment doesn't interfere. Both create a DI scope
per message (matching how `ApplyModerationVerdictHandler` and the new `IOwnerStatusValidator`
implementation are scoped, while the hosted services themselves are singletons — same lifetime mismatch
`OutboxPublisher` doesn't have to deal with because it talks to `IAmazonDynamoDB`, a singleton; these two
consumers are the first to need `IServiceScopeFactory`).

```
Kafka: user-lifecycle-events ──▶ OwnerStatusConsumer ──▶ DynamoDbOwnerStatusValidator.UpsertAsync
                                                              (writes OwnerStatusItem, PK=OWNERSTATUS#{ownerId})

Kafka: asset-media-moderated ──▶ ModerationVerdictConsumer ──▶ IHandler<ApplyModerationVerdictRequest,_>
                                                                    (existing F-09 handler, unchanged)

CreateAssetHandler ──▶ IOwnerStatusValidator.IsOwnerActiveAsync ──▶ DynamoDbOwnerStatusValidator.IsOwnerActiveAsync
                                                                        (reads OwnerStatusItem; not found = false, fail-closed)
```

`DynamoDbOwnerStatusValidator` is the single class serving both the pre-existing `IOwnerStatusValidator`
(Domain-facing read, consumed by `CreateAssetHandler`) and a new Infrastructure-internal
`IOwnerStatusCacheWriter` (consumed only by `OwnerStatusConsumer`) — one DynamoDB item type, one class,
avoids a second client wrapper.

## Components

### 1. `KafkaTopics` (Domain, extend existing file)

Add two constants for inbound topics, using the **literal producer-side strings** (verified against real
`identity-api`/`rentifyx-ai-services` code in spec.md, not this repo's own naming convention):
`UserLifecycleEvents = "user-lifecycle-events"`, `AssetMediaModerated = "asset-media-moderated"`.

### 2. `DynamoDbKeys` (Infrastructure, extend existing file)

Add `OwnerStatusType = "OwnerStatus"`, `OwnerStatusPrefix = "OWNERSTATUS#"` (deliberately distinct from the
existing `OwnerPrefix = "OWNER#"`, which is `AssetItem`'s GSI1 owner-index key — reusing it would collide
PKs across item types in the single table), `MetadataSortKey = "METADATA"` (single-item-per-owner, no
range needed, same shape as `CategoryItem` conceptually but even simpler — no GSI), and
`OwnerStatusKey(Guid ownerId) => $"{OwnerStatusPrefix}{ownerId}"`.

### 3. `OwnerStatusItem` (new, Infrastructure `Persistence/Items/`)

POCO following `CategoryItem`'s exact shape/attribute style (`[DynamoDBTable("AssetRegistry")]`,
`[DynamoDBHashKey]`/`[DynamoDBRangeKey]` on `Pk`/`Sk`, plain `[DynamoDBProperty]` fields). No GSI — this
item is only ever read by exact PK/SK (`GetItemAsync`), never queried/listed. Fields: `Pk`, `Sk`, `Type`,
`OwnerId` (string, Guid.ToString()), `IsActive` (bool), `Reason` (string), `UpdatedAt` (string, ISO 8601 via
`DateTimeOffset.ToString("o")` — matches `AssetSortKey`'s `{createdAt:o}` precedent for datetime-as-string
in this table).

### 4. `OwnerStatusDynamoDbMapper` (new, Infrastructure `Persistence/Mappers/`)

Static class, same shape as `OutboxDynamoDbMapper`/`AssetDynamoDbMapper`: `ToItem(ownerId, isActive, reason,
updatedAt) -> OwnerStatusItem`, `ToAttributeMap(OwnerStatusItem) -> Dictionary<string,AttributeValue>`,
`FromAttributeMap(Dictionary<string,AttributeValue>) -> OwnerStatusItem`. Explicit attribute-name constants
at the top of the class, no reflection auto-mapping — matches every other mapper in this repo.

### 5. `IOwnerStatusCacheWriter` (new, Infrastructure-internal interface, `Persistence/` or alongside the validator class — not `Domain/Interfaces/`)

```
internal interface IOwnerStatusCacheWriter
{
    Task UpsertAsync(Guid ownerId, bool isActive, string reason, DateTimeOffset updatedAt, CancellationToken ct = default);
}
```

Infrastructure-only because only `OwnerStatusConsumer` (an Api-layer class, but calling through DI, not
directly depending on Domain contracts the way handlers do) needs the write path — it's not a Domain
concept the way `IOwnerStatusValidator`'s read path is.

### 6. `DynamoDbOwnerStatusValidator` (new, Infrastructure `Persistence/`)

Implements both `IOwnerStatusValidator` and `IOwnerStatusCacheWriter`. Primary constructor
`(IAmazonDynamoDB client, DynamoDbOptions options)` — same constructor shape as `DynamoDbAssetRepository`/
`DynamoDbCategoryRepository`. `IsOwnerActiveAsync`: `GetItemAsync` by `PK=OwnerStatusKey(ownerId),
SK=MetadataSortKey`; item missing → `return false` (**fail-closed, confirmed decision**); item found →
`return item.IsActive`. `UpsertAsync`: `PutItemAsync` with the mapper's `ToAttributeMap`.

### 7. `ConfigurationKeys` (Infrastructure, extend existing file)

Add `KafkaOwnerStatusConsumerGroupId = "Kafka:OwnerStatusConsumer:GroupId"`,
`KafkaModerationVerdictConsumerGroupId = "Kafka:ModerationVerdictConsumer:GroupId"`. Reuses the existing
`KafkaBootstrapServers` key — no new bootstrap-servers config needed.

### 8. Event contract DTOs (new, Api `Messaging/Contracts/`)

Plain records, `System.Text.Json`-deserializable, mirroring the verified real shapes from spec.md:
- `UserLifecycleEventEnvelope(string EventType, Guid AggregateId, DateTimeOffset OccurredAt, JsonElement Data)`
  — `Data` typed as `JsonElement` (not `object`) so the consumer can re-deserialize it into the correct
  inner type only after switching on `EventType`, without a custom `JsonConverter`.
- `UserSuspendedPayload(Guid UserId, string Reason, DateTimeOffset OccurredAt)`
- `UserAccountDeletedPayload(Guid UserId, DateTimeOffset OccurredAt)`
- `AssetMediaModeratedEvent(Guid AssetId, ModerationVerdict Verdict, IReadOnlyList<ModerationLabelDto> Labels, float TopConfidence, DateTimeOffset Timestamp, string Bucket, string Key, int SchemaVersion)`
  — reuses the existing Domain `ModerationVerdict` enum directly as the field type (same 3 names as
  ai-services' `Verdict` enum, confirmed in spec.md) so `System.Text.Json`'s default enum-as-string handling
  round-trips it without a custom converter, provided `JsonStringEnumConverter` is registered on the
  deserialization options (ai-services serializes with it; this side must deserialize with the matching
  converter registered, or plain string values won't parse into the enum).
- `ModerationLabelDto(string Name, float Confidence)` — deserialized for completeness, never read.

### 9. `OwnerStatusConsumer : BackgroundService` (new, Api `Messaging/`)

Constructor: `(IConsumer<string,string> consumer, IServiceScopeFactory scopeFactory, ILogger<OwnerStatusConsumer> logger)`.
`ExecuteAsync`: `consumer.Subscribe(KafkaTopics.UserLifecycleEvents)`, then a `while
(!stoppingToken.IsCancellationRequested)` loop calling `consumer.Consume(stoppingToken)` (blocking pull —
distinct shape from `OutboxPublisher`'s `PeriodicTimer` push loop). Per message: deserialize envelope,
switch on `EventType` (`"UserSuspended"` → deserialize `Data` as `UserSuspendedPayload`, reason = payload's
own `Reason`; `"UserAccountDeleted"` → deserialize `Data` as `UserAccountDeletedPayload`, reason =
`"Deleted"` constant; anything else → log + skip), create a scope, resolve `IOwnerStatusCacheWriter`, call
`UpsertAsync(userId, isActive: false, reason, occurredAt)`, then `consumer.Commit(result)`. Malformed
JSON/unknown `EventType` → log + `consumer.Commit(result)` anyway (poison pill, must not block the
partition). DynamoDB write exception → log, do **not** commit (let redelivery retry). Wrap the per-message
body in try/catch distinguishing "deserialize/map failure" (always commit) from "write failure" (never
commit) exactly as spec.md's Handler/Service Logic section describes. `StopAsync` override: `consumer.Close()`.

### 10. `ModerationVerdictConsumer : BackgroundService` (new, Api `Messaging/`)

Same loop shape, subscribed to `KafkaTopics.AssetMediaModerated`. Per message: deserialize
`AssetMediaModeratedEvent`; `SchemaVersion != 2` → log + skip + commit; otherwise create a scope, resolve
`IHandler<ApplyModerationVerdictRequest, AssetModerationResponse>`, call `HandleAsync(new
ApplyModerationVerdictRequest(evt.AssetId, evt.Verdict), stoppingToken)`; `result.Match`: success → commit;
`Error.NotFound` → log + commit (poison pill, asset will never exist on retry); any other error → log, no
commit. `StopAsync`: `consumer.Close()`.

### 11. `CrossServiceConsumingExtensions` (new, Api `Extensions/`, mirrors `OutboxServiceCollectionExtensions`)

`AddCrossServiceConsuming(this IServiceCollection services, IConfiguration configuration)`: registers two
named/keyed `IConsumer<string,string>` singletons — since both consumers need distinct `ConsumerConfig`
(different `GroupId`), a single unkeyed `IConsumer<string,string>` registration (like `OutboxPublisher`'s
single unkeyed `IProducer<string,string>`) won't work for two consumers. Use .NET's keyed DI
(`AddKeyedSingleton<IConsumer<string,string>>("owner-status", ...)` /
`AddKeyedSingleton<IConsumer<string,string>>("moderation-verdict", ...)`), and inject via
`[FromKeyedServices("owner-status")]` in each consumer's constructor. Each `ConsumerConfig`: `BootstrapServers`
from `ConfigurationKeys.KafkaBootstrapServers` (same key, same `"localhost:9092"` fallback as
`OutboxServiceCollectionExtensions`), `GroupId` from the new per-consumer config keys, `AutoOffsetReset =
Earliest`, `EnableAutoCommit = false` (required for the commit-only-after-success semantics above).

### 12. `Program.cs` (extend)

```
builder.Services.AddCrossServiceConsuming(builder.Configuration);
builder.Services.AddHostedService<OwnerStatusConsumer>();
builder.Services.AddHostedService<ModerationVerdictConsumer>();
```
Added alongside the existing `AddOutboxPublishing`/`AddHostedService<OutboxPublisher>()` lines.

### 13. `InfrastructureDependencyInjection.AddDynamoDb` (extend)

Add `services.AddScoped<IOwnerStatusValidator, DynamoDbOwnerStatusValidator>();` and
`services.AddScoped<IOwnerStatusCacheWriter, DynamoDbOwnerStatusValidator>();` (registering the same
concrete type against both interfaces — .NET DI supports this; each resolves independently, both scoped so
they share the DynamoDB client's scoped lifetime consistently with `IAssetRepository`/`ICategoryRepository`).

## Data Model Changes

Covered in full in spec.md's Data Model Changes section — `OwnerStatusItem`, single-table, new PK prefix
`OWNERSTATUS#{ownerId}`, no migration mechanism applicable (DynamoDB).

## Testing Strategy

- **Unit tests** — new folder `03-tests/03-Handlers/.../Messaging/` (or a new sibling if handler-only scope
  feels wrong — judgment call, `OwnerStatusConsumerTests`/`ModerationVerdictConsumerTests` mock
  `IOwnerStatusCacheWriter`/`IHandler<...>` respectively, feed hand-built `ConsumeResult<string,string>`-like
  messages through the private message-handling method (extract a testable `internal` method rather than
  testing `ExecuteAsync` directly, matching how `OutboxPublisher`'s `PublishPendingBatchAsync` is a private
  method exercised only via its public `ExecuteAsync` in integration tests — but this feature's malformed-
  payload/skip-vs-retry branching is dense enough to warrant direct unit coverage of the mapping logic,
  which means extracting an `internal` (not `private`) method + `[InternalsVisibleTo]`, or restructuring the
  parse/map step into a small internal static class the consumer calls — a design-time call, not dictated
  further here).
- **Integration tests** — `03-tests/04-Repositories/`, reuse the existing `KafkaFixture` +
  `LocalStackFixture` (`OutboxFixtureGroup`'s pattern, new `CrossServiceFixtureGroup` or reuse the same
  group) — publish a real message via a test `IProducer`, run the consumer, assert DynamoDB/asset state.
- **DynamoDbOwnerStatusValidator** unit tests — mock `IAmazonDynamoDB`, same pattern as any other repository
  unit test in this repo if one exists, or integration-only via `LocalStackFixture` if repositories in this
  repo are integration-tested exclusively (confirm via existing `DynamoDbAssetRepository` test file before
  deciding — not re-verified in this design pass).

## Risks & Unknowns

- `[non-blocking]` Keyed DI (`AddKeyedSingleton`/`[FromKeyedServices]`) is net-new to this repo — no existing
  precedent. If it turns out any currently-targeted .NET version/package combination has friction with it,
  fall back to two named wrapper types (`IOwnerStatusKafkaConsumer`/`IModerationVerdictKafkaConsumer`, each a
  thin single-purpose wrapper around a plain `IConsumer<string,string>`) instead of keyed services. Flag
  during Execute if keyed DI doesn't work cleanly.
- `[non-blocking]` Whether the internal per-message parsing logic needs its own extracted testable unit
  (see Testing Strategy) is left as an Execute-time call rather than dictated here — avoid over-designing
  before seeing the actual `Consume()` message shape in code.
- Everything else already flagged as open in spec.md carries forward unchanged (topic contract stability on
  identity-api's side, no DLQ, cache backfill strategy).
