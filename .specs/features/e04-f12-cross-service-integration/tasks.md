# Tasks: F-12 Cross-Service Integration

Convention note (confirmed against `DynamoDbAssetRepositoryTests.cs`): repository/Infrastructure persistence
code in this repo is integration-tested only (Testcontainers/LocalStack), no mocked unit tests — the same
applies to `DynamoDbOwnerStatusValidator` below. Only Application-layer / message-parsing logic gets Moq
unit tests, matching `03-Handlers`' existing convention.

| # | Layer | What to create / change | Reference file | Depends on |
|---|---|---|---|---|
| 1 | Domain | Add `UserLifecycleEvents`/`AssetMediaModerated` to `KafkaTopics` | `KafkaTopics.cs` | — |
| 2 | Infrastructure | Add `OwnerStatusType`/`OwnerStatusPrefix`/`MetadataSortKey`/`OwnerStatusKey` to `DynamoDbKeys` | `DynamoDbKeys.cs` | — |
| 3 | Infrastructure | New `OwnerStatusItem` POCO | `CategoryItem.cs` | 2 |
| 4 | Infrastructure | New `OwnerStatusDynamoDbMapper` | `OutboxDynamoDbMapper.cs` (via AssetDynamoDbMapper style) | 3 |
| 5 | Infrastructure | New `IOwnerStatusCacheWriter` interface | — (new, Infrastructure-internal) | — |
| 6 | Infrastructure | New `DynamoDbOwnerStatusValidator` (implements `IOwnerStatusValidator` + `IOwnerStatusCacheWriter`) | `DynamoDbCategoryRepository.cs` | 3, 4, 5 |
| 7 | Infrastructure | Add 2 `GroupId` keys to `ConfigurationKeys` | `ConfigurationKeys.cs` | — |
| 8 | IoC | Register `IOwnerStatusValidator`/`IOwnerStatusCacheWriter` in `AddDynamoDb` | `InfrastructureDependencyInjection.cs` | 6 |
| 9 | Api | New event DTOs (`UserLifecycleEventEnvelope`, `UserSuspendedPayload`, `UserAccountDeletedPayload`, `AssetMediaModeratedEvent`, `ModerationLabelDto`) | — (new, `Messaging/Contracts/`) | — |
| 10 | Api | New `CrossServiceConsumingExtensions.AddCrossServiceConsuming` | `OutboxServiceCollectionExtensions.cs` | 7 |
| 11 | Api | New `OwnerStatusConsumer : BackgroundService` | `OutboxPublisher.cs` | 1, 5, 9, 10 |
| 12 | Api | New `ModerationVerdictConsumer : BackgroundService` | `OutboxPublisher.cs` | 1, 9, 10 |
| 13 | Api | Wire both in `Program.cs` | `Program.cs` | 10, 11, 12 |
| 14 | Test | Integration tests for `OwnerStatusConsumer` (Kafka + LocalStack) | `OutboxPublisherTests.cs` + `KafkaFixture.cs` | 11 |
| 15 | Test | Integration tests for `ModerationVerdictConsumer` (Kafka + LocalStack, seeded `PendingModeration` asset) | `OutboxPublisherTests.cs` + `KafkaFixture.cs` | 12 |
| 16 | Test | Integration tests for `DynamoDbOwnerStatusValidator` (`IsOwnerActiveAsync` not-found→false, found+false, found+true; `UpsertAsync` round-trip) | `DynamoDbAssetRepositoryTests.cs` | 6 |
| 17 | Docs | ADR-AR-011 (fail-closed owner-status cache decision) + STATE.md/ROADMAP.md update | `docs/decisions/008-moderation-event-only-boundary.md` (style precedent) | 1–16 |

---

---
status: pending
title: Add inbound Kafka topic constants
type: backend
complexity: low
dependencies: []
---

**Layer:** Domain
**File:** `02-src/03-Domain/RentifyxAssetRegistry.Domain/Constants/KafkaTopics.cs`
**Reference:** existing file (same class, adding 2 constants)
**What:** Add `UserLifecycleEvents = "user-lifecycle-events"` and `AssetMediaModerated = "asset-media-moderated"` — literal strings verified against real `identity-api`/`rentifyx-ai-services` producer code (see spec.md), not this repo's own dotted naming convention.
**Done when:** `dotnet build` succeeds; both constants exist and are referenced by nothing yet (referenced starting task 11/12).
**Commit:** `feat(domain): add inbound Kafka topic constants for F-12 consumers`

---
status: pending
title: Add OwnerStatus DynamoDB key constants
type: backend
complexity: low
dependencies: []
---

**Layer:** Infrastructure
**File:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/DynamoDbKeys.cs`
**Reference:** existing file (same class, adding constants + one key-builder method)
**What:** Add `OwnerStatusType = "OwnerStatus"`, `OwnerStatusPrefix = "OWNERSTATUS#"` (distinct from `OwnerPrefix`), `MetadataSortKey = "METADATA"`, `OwnerStatusKey(Guid ownerId) => $"{OwnerStatusPrefix}{ownerId}"`.
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(infra): add DynamoDB key constants for owner-status cache item`

---
status: pending
title: Add OwnerStatusItem POCO
type: backend
complexity: low
dependencies: [2]
---

**Layer:** Infrastructure
**File:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/Items/OwnerStatusItem.cs` (new)
**Reference:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/Items/CategoryItem.cs`
**What:** New sealed class, `[DynamoDBTable("AssetRegistry")]`, `Pk`/`Sk` hash/range keys, `Type` (defaults to `DynamoDbKeys.OwnerStatusType`), `OwnerId` (string), `IsActive` (bool), `Reason` (string), `UpdatedAt` (string, ISO 8601). No GSI attributes — this item is only ever read by exact PK/SK.
**Done when:** `dotnet build` succeeds; class compiles with all `[DynamoDBProperty]` attributes present.
**Commit:** `feat(infra): add OwnerStatusItem DynamoDB POCO`

---
status: pending
title: Add OwnerStatusDynamoDbMapper
type: backend
complexity: low
dependencies: [3]
---

**Layer:** Infrastructure
**File:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/Mappers/OwnerStatusDynamoDbMapper.cs` (new)
**Reference:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/Mappers/OutboxDynamoDbMapper.cs` (and `AssetDynamoDbMapper.cs` for the general style — explicit attribute-name constants, no reflection)
**What:** Static class: `ToItem(Guid ownerId, bool isActive, string reason, DateTimeOffset updatedAt) -> OwnerStatusItem` (builds `Pk`/`Sk` via `DynamoDbKeys.OwnerStatusKey`/`MetadataSortKey`), `ToAttributeMap(OwnerStatusItem) -> Dictionary<string,AttributeValue>`, `FromAttributeMap(Dictionary<string,AttributeValue>) -> OwnerStatusItem`.
**Done when:** `dotnet build` succeeds; round-trip `ToAttributeMap` → `FromAttributeMap` preserves all fields (verified in task 16's integration test, not a separate unit test here — no mocked-repository unit tests exist in this repo's convention).
**Commit:** `feat(infra): add OwnerStatusDynamoDbMapper`

---
status: pending
title: Add IOwnerStatusCacheWriter interface
type: backend
complexity: low
dependencies: []
---

**Layer:** Infrastructure
**File:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/IOwnerStatusCacheWriter.cs` (new)
**Reference:** none exact — new Infrastructure-internal contract, not a `Domain/Interfaces/` type (see design.md's rationale: this is a write-path only `OwnerStatusConsumer` needs, not a Domain-facing contract like `IOwnerStatusValidator`)
**What:** `internal interface IOwnerStatusCacheWriter { Task UpsertAsync(Guid ownerId, bool isActive, string reason, DateTimeOffset updatedAt, CancellationToken ct = default); }`
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(infra): add IOwnerStatusCacheWriter interface`

---
status: pending
title: Implement DynamoDbOwnerStatusValidator
type: backend
complexity: medium
dependencies: [3, 4, 5]
---

**Layer:** Infrastructure
**File:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/DynamoDbOwnerStatusValidator.cs` (new)
**Reference:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Persistence/DynamoDbCategoryRepository.cs` (primary-constructor shape, `IAmazonDynamoDB`/`DynamoDbOptions` injection, `GetItemAsync`/`PutItemAsync` usage)
**What:** Primary constructor `(IAmazonDynamoDB client, DynamoDbOptions options)`, implements both `IOwnerStatusValidator` (from `Domain/Interfaces/Asset/IOwnerStatusValidator.cs`) and `IOwnerStatusCacheWriter`. `IsOwnerActiveAsync`: `GetItemAsync` by PK/SK; item missing → `return false` (fail-closed, confirmed decision); found → `return item.IsActive`. `UpsertAsync`: `PutItemAsync` via the mapper.
**Done when:** `dotnet build` succeeds; behavior verified in task 16's integration tests (not-found→false, found+false→false, found+true→true, upsert round-trip).
**Commit:** `feat(infra): implement DynamoDbOwnerStatusValidator (fail-closed owner-status cache)`

---
status: pending
title: Add Kafka consumer group config keys
type: backend
complexity: low
dependencies: []
---

**Layer:** Infrastructure
**File:** `02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/Constants/ConfigurationKeys.cs`
**Reference:** existing file (same class, adding 2 constants; reuses existing `KafkaBootstrapServers`)
**What:** Add `KafkaOwnerStatusConsumerGroupId = "Kafka:OwnerStatusConsumer:GroupId"`, `KafkaModerationVerdictConsumerGroupId = "Kafka:ModerationVerdictConsumer:GroupId"`.
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(infra): add Kafka consumer group config keys`

---
status: pending
title: Register IOwnerStatusValidator/IOwnerStatusCacheWriter in DI
type: backend
complexity: low
dependencies: [6]
---

**Layer:** IoC
**File:** `02-src/04-IoC/RentifyxAssetRegistry.IoC/InfrastructureDependencyInjection.cs`
**Reference:** existing `AddDynamoDb` method (adding 2 lines alongside `IAssetRepository`/`ICategoryRepository` registrations)
**What:** `services.AddScoped<IOwnerStatusValidator, DynamoDbOwnerStatusValidator>();` and `services.AddScoped<IOwnerStatusCacheWriter, DynamoDbOwnerStatusValidator>();` — this closes the previously-unregistered `IOwnerStatusValidator` dependency `CreateAssetHandler` already has.
**Done when:** `dotnet build` succeeds; `dotnet test` full suite still green (this is the first real exercise of `CreateAssetHandler`'s DI graph being fully resolvable).
**Commit:** `feat(ioc): register DynamoDbOwnerStatusValidator for owner-status validation`

---
status: pending
title: Add inbound event contract DTOs
type: backend
complexity: low
dependencies: []
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/Contracts/` (new folder: `UserLifecycleEventEnvelope.cs`, `UserSuspendedPayload.cs`, `UserAccountDeletedPayload.cs`, `AssetMediaModeratedEvent.cs`, `ModerationLabelDto.cs`)
**Reference:** field shapes verified in spec.md's Request/Input section against real sibling-repo code; no existing local file to pattern-match against (net-new DTOs), follow this repo's general "plain sealed record" style used throughout Domain events/Application requests
**What:** 5 small sealed records exactly matching spec.md's verified field tables. `UserLifecycleEventEnvelope.Data` typed as `System.Text.Json.JsonElement`. `AssetMediaModeratedEvent.Verdict` typed as the existing Domain `ModerationVerdict` enum.
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(api): add inbound Kafka event contract DTOs for F-12`

---
status: pending
title: Add CrossServiceConsumingExtensions (keyed consumer DI)
type: backend
complexity: medium
dependencies: [7]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Extensions/CrossServiceConsumingExtensions.cs` (new)
**Reference:** `02-src/01-Api/RentifyxAssetRegistry.Api/Extensions/OutboxServiceCollectionExtensions.cs`
**What:** `AddCrossServiceConsuming(this IServiceCollection services, IConfiguration configuration)` registering two keyed `IConsumer<string,string>` singletons (`"owner-status"`, `"moderation-verdict"`) via `AddKeyedSingleton`, each with its own `ConsumerConfig { BootstrapServers, GroupId, AutoOffsetReset = Earliest, EnableAutoCommit = false }`. If keyed DI proves awkward with the installed package versions, fall back to two distinct wrapper interfaces instead (design.md Risk, flag if hit).
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(api): add keyed Kafka consumer DI registration`

---
status: pending
title: Implement OwnerStatusConsumer
type: backend
complexity: high
dependencies: [1, 5, 9, 10]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/OwnerStatusConsumer.cs` (new)
**Reference:** `02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/OutboxPublisher.cs` (BackgroundService shape, StopAsync override, try/catch-per-cycle-error pattern — but this is a consume loop, not a `PeriodicTimer` loop, so the inner loop shape differs)
**What:** `BackgroundService` subscribed to `KafkaTopics.UserLifecycleEvents`. Per message: deserialize `UserLifecycleEventEnvelope`; switch `EventType` → deserialize `Data` into the matching payload type (`"UserSuspended"`/`"UserAccountDeleted"`), unknown type → log + skip; create a DI scope via `IServiceScopeFactory`, resolve `IOwnerStatusCacheWriter`, call `UpsertAsync(userId, isActive: false, reason, occurredAt)`; commit offset on success or on any parse/map failure (poison pill); do **not** commit on a DynamoDB write exception (let redelivery retry). `StopAsync` override calls `consumer.Close()`.
**Done when:** `dotnet build` succeeds; behavior verified end-to-end in task 14's integration test.
**Commit:** `feat(api): implement OwnerStatusConsumer (identity-api user-lifecycle-events)`

---
status: pending
title: Implement ModerationVerdictConsumer
type: backend
complexity: high
dependencies: [1, 9, 10]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/ModerationVerdictConsumer.cs` (new)
**Reference:** `02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/OutboxPublisher.cs` (same shape notes as task 11); `02-src/02-Application/.../ApplyModerationVerdict/ApplyModerationVerdictHandler.cs` for the exact `Request`/response contract being called
**What:** `BackgroundService` subscribed to `KafkaTopics.AssetMediaModerated`. Per message: deserialize `AssetMediaModeratedEvent`; `SchemaVersion != 2` → log + skip + commit; else create a DI scope, resolve `IHandler<ApplyModerationVerdictRequest, AssetModerationResponse>`, call `HandleAsync(new ApplyModerationVerdictRequest(evt.AssetId, evt.Verdict), stoppingToken)`; `result.Match`: success → commit; `Error.NotFound` → log + commit (poison pill); any other error → log, no commit. `StopAsync`: `consumer.Close()`.
**Done when:** `dotnet build` succeeds; behavior verified end-to-end in task 15's integration test, including confirming F-09's existing idempotent-replay behavior (asset not `PendingModeration`) requires no extra handling here.
**Commit:** `feat(api): implement ModerationVerdictConsumer (ai-services asset-media-moderated)`

---
status: pending
title: Wire both consumers in Program.cs
type: backend
complexity: low
dependencies: [10, 11, 12]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Program.cs`
**Reference:** existing `AddOutboxPublishing`/`AddHostedService<OutboxPublisher>()` lines (add alongside)
**What:** `builder.Services.AddCrossServiceConsuming(builder.Configuration); builder.Services.AddHostedService<OwnerStatusConsumer>(); builder.Services.AddHostedService<ModerationVerdictConsumer>();`
**Done when:** `dotnet build` succeeds; `dotnet run` starts without a DI resolution exception (manual smoke check, or confirmed via task 14/15's integration tests exercising the full host).
**Commit:** `feat(api): wire cross-service Kafka consumers into host startup`

---
status: pending
title: Integration tests for OwnerStatusConsumer
type: test
complexity: medium
dependencies: [11]
---

**Layer:** Test
**File:** `03-tests/04-Repositories/RentifyxAssetRegistry.Tests.Repositories/OwnerStatusConsumerTests.cs` (new)
**Reference:** `03-tests/04-Repositories/RentifyxAssetRegistry.Tests.Repositories/OutboxPublisherTests.cs` + `Fixtures/KafkaFixture.cs` (reuse `KafkaFixture`/`LocalStackFixture` via a new or shared collection fixture group)
**What:** Publish a real `UserSuspended` envelope message to a Testcontainers Kafka broker, run `OwnerStatusConsumer` against it, assert the `OwnerStatusItem` lands in LocalStack DynamoDB with `IsActive=false, Reason="Suspended"`. Repeat for `UserAccountDeleted`. Add a malformed-message case asserting the offset still commits (no infinite retry).
**Done when:** `dotnet test` passes for this file.
**Commit:** `test(infra): add OwnerStatusConsumer integration tests`

---
status: pending
title: Integration tests for ModerationVerdictConsumer
type: test
complexity: medium
dependencies: [12]
---

**Layer:** Test
**File:** `03-tests/04-Repositories/RentifyxAssetRegistry.Tests.Repositories/ModerationVerdictConsumerTests.cs` (new)
**Reference:** same fixtures as task 14
**What:** Seed a `PendingModeration` asset in LocalStack DynamoDB, publish an `Approved`-verdict `AssetMediaModeratedEvent` (SchemaVersion 2) to Kafka, run `ModerationVerdictConsumer`, assert the asset transitions to `Active` end-to-end. Add a `SchemaVersion=1` (mismatch) case asserting no handler call occurs and the offset commits anyway.
**Done when:** `dotnet test` passes for this file.
**Commit:** `test(infra): add ModerationVerdictConsumer integration tests`

---
status: pending
title: Integration tests for DynamoDbOwnerStatusValidator
type: test
complexity: low
dependencies: [6]
---

**Layer:** Test
**File:** `03-tests/04-Repositories/RentifyxAssetRegistry.Tests.Repositories/DynamoDbOwnerStatusValidatorTests.cs` (new)
**Reference:** `03-tests/04-Repositories/RentifyxAssetRegistry.Tests.Repositories/DynamoDbAssetRepositoryTests.cs`
**What:** `IsOwnerActiveAsync` for a never-seen owner → `false`; for an owner with `IsActive=false` written via `UpsertAsync` → `false`; for an owner with `IsActive=true` written directly → `true` (mapper round-trip check, even though this consumer never writes `true` itself). `UpsertAsync` round-trip preserves `Reason`/`UpdatedAt`.
**Done when:** `dotnet test` passes for this file.
**Commit:** `test(infra): add DynamoDbOwnerStatusValidator integration tests`

---
status: pending
title: ADR-AR-011 + STATE.md/ROADMAP.md update
type: docs
complexity: low
dependencies: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]
---

**Layer:** Docs
**File:** `docs/decisions/011-owner-status-fail-closed.md` (new), `.specs/project/STATE.md`, `.specs/project/ROADMAP.md`
**Reference:** `docs/decisions/008-moderation-event-only-boundary.md` (ADR style/structure precedent)
**What:** Write ADR-AR-011 documenting the fail-closed decision (already confirmed by user this session) and its cold-start-rejection consequence. Update STATE.md (Feature Completion Log entry, close the "F-12 not started" Current Work note) and ROADMAP.md (mark Cross-Service Integration DONE, M4 CLOSED).
**Done when:** Both docs reflect F-12 complete; full `dotnet test` suite green cited in STATE.md's entry.
**Commit:** `docs: ADR-AR-011 fail-closed owner-status cache; close M4/F-12`
