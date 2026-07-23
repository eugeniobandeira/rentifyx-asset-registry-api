# Design — DynamoDB Repository & Outbox (F-10)

## Single-table key schema

One table (`AssetRegistry`, name configurable). Generic attribute names shared across item
types (standard single-table trick — matches identity-api's ADR-005): `PK`, `SK`, `Type`,
`GSI1PK`/`GSI1SK` .. `GSI4PK`/`GSI4SK`. Each item type uses a different subset.

| Item type | PK | SK | GSI1 (PK/SK) | GSI2 (PK/SK) | GSI3 (PK/SK) | GSI4 (PK/SK) |
|---|---|---|---|---|---|---|
| Asset | `ASSET#{id}` | `ASSET#{id}` | `OWNER#{ownerId}` / `ASSET#{createdAt:o}#{id}` | `CATEGORY#{categoryId}` / `ASSET#{createdAt:o}#{id}` | `IDEMPOTENCY#{key}` / `ASSET#{id}` | `STATUS#{status}` / `ASSET#{createdAt:o}#{id}` |
| Category | `CATEGORY#{id}` | `CATEGORY#{id}` | `CATEGORY_LIST` / `CATEGORY#{depth:D2}#{name}#{id}` | — | — | — |
| Outbox | `OUTBOX#{id}` | `OUTBOX#{id}` | `OUTBOX_STATUS#{status}` / `{createdAtUtc:o}#{id}` | — | — | — |

Rationale: this maps every acceptance criterion (DYN-01..11) to a named GSI 1:1 (GSI1=owner,
GSI2=category-search, GSI3=idempotency, GSI4=status-only search) and lets Category/Outbox reuse
GSI1's attribute *names* for unrelated partitions (`CATEGORY_LIST` / `OUTBOX_STATUS#...`) without
a 5th index — legal in DynamoDB (a GSI is just an index over whatever attributes happen to be
present on an item) and consistent with "GSI queries only, never Scan."

4 GSIs total, each projecting `ALL` (simplicity over storage cost at this item count).

## POCO items vs. Domain entities

Domain stays framework-free (no AWS SDK ref). Each aggregate gets a plain POCO `*Item` class
(`AssetItem`, `CategoryItem`, `OutboxItem`) living in `Infrastructure/Persistence/Items/`,
decorated with `[DynamoDBTable]`/`[DynamoDBHashKey]`/`[DynamoDBRangeKey]`/
`[DynamoDBGlobalSecondaryIndexHashKey]` etc. — this is the "custom POCO" the brief calls for;
attributes describe *storage shape* only, mapping to/from the Domain entity is 100% done by hand
in static mapper classes (`AssetDynamoDbMapper`, `CategoryDynamoDbMapper`, `OutboxDynamoDbMapper`),
never relying on attribute-driven property-name auto-mapping for the Domain→Item direction (VOs
like `Money`/`Media`/`AssetTitle` don't map 1:1 to scalar item properties, so the mapper explicitly
flattens/reconstructs them).

`IDynamoDBContext` (`context.LoadAsync<T>`, `context.SaveAsync<T>`) is used for the simple,
non-transactional single-item paths: `GetByIdAsync`, and `SaveAsync` when the entity raised no
domain events. `IAmazonDynamoDB` (low-level) is used for: GSI `QueryAsync` (all search/lookup
paths — context's high-level query needs a fully-typed key object per GSI, more ceremony than a
raw `QueryRequest` for 4 different index shapes), `UpdateItemAsync` (soft delete — flips `Status`
+ `GSI4PK` without a full item rewrite), and `TransactWriteItemsAsync` (entity + outbox atomic
write). Query results are converted back to POCOs via `context.FromDocument<T>(Document.FromAttributeMap(item))`.

## SaveAsync / Outbox flow

`DynamoDbAssetRepository.SaveAsync`:
1. Map entity → `AssetItem` → `Dictionary<string,AttributeValue>` (`AssetDynamoDbMapper.ToItem`).
2. If `entity.DomainEvents.Count == 0`: single `PutItemAsync`.
3. Else: build one `TransactWriteItem` per domain event (`OutboxDynamoDbMapper.ToTransactPut`,
   `PK/SK = OUTBOX#{Guid.NewGuid()}`, `EventType` = event's runtime type name, `Payload` =
   `JsonSerializer.Serialize(domainEvent, domainEvent.GetType())`, `Status = "Pending"`,
   `RetryCount = 0`) plus one `Put` for the asset item itself, all in a single
   `TransactWriteItemsAsync` call. If event count + 1 > 100, throw `InvalidOperationException`
   (DYN-16, programmer-error guard, not expected in practice per spec).
4. On success, `entity.ClearDomainEvents()` (repository's responsibility — handlers never call
   this themselves today, matches existing handler code that never touches `DomainEvents`).

`CategoryEntity` has no domain events today — `DynamoDbCategoryRepository.SaveAsync` is always a
plain `context.SaveAsync`, no transaction needed.

## Cursor pagination

`AssetSearchFilter.NextPageToken` (opaque string) ↔ DynamoDB `ExclusiveStartKey`
(`Dictionary<string,AttributeValue>`), via `PageTokenCodec`:
- Encode: `Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(lastEvaluatedKey))` where
  the dictionary is first converted to a `Dictionary<string,string>` (attribute values in a
  search-relevant key are always S-type — GSI + table keys here are all strings) to avoid
  hand-rolling `AttributeValue` JSON converters.
- Decode: reverse; on `FormatException`/`JsonException`/malformed shape, `SearchAsync` returns
  `null` as a sentinel the repository maps to... no — repository is Infrastructure and can't
  return `ErrorOr`. Decision: malformed token throws a dedicated `InvalidPageTokenException`
  (`Infrastructure/Persistence/Exceptions/`), and `SearchAssetsHandler` (Application, already
  exists) is given a small addition — catch it and return `Error.Validation` — this is the one
  place this feature reaches slightly outside Infrastructure, justified because DYN-11 is
  explicitly a *validation* outcome (expected user error: tampered pagination token), and
  `ErrorOr` conversion only exists at the Application boundary per CLAUDE.md's error-handling
  convention. Repository itself never returns `ErrorOr` — it just throws a typed exception the
  handler is taught to translate.

## Search query shape

- `CategoryId` set → GSI2 `KeyConditionExpression: GSI2PK = :pk` (`CATEGORY#{id}`),
  `FilterExpression: #status = :active` (server-side, still counts as "server-side" per spec
  even though it's a Filter not a key condition — DynamoDB applies FilterExpression before
  returning to the client but after reading, which is what AC1 asks for).
- No `CategoryId` → GSI4 `KeyConditionExpression: GSI4PK = :pk` (`STATUS#Active`) — no filter
  needed, key already encodes status.
- `MinPrice`/`MaxPrice`/`Keyword` → appended to `FilterExpression` (`PriceAmount BETWEEN :min AND
  :max`, `contains(Title, :kw)`) regardless of which GSI was used.
- `PageSize` → `Limit`. `NextPageToken` decoded → `ExclusiveStartKey`. Response's
  `LastEvaluatedKey` (if present) → encoded → `CursorPagedResult.NextPageToken`.

## Outbox publisher

`OutboxPublisher : BackgroundService` in `02-src/01-Api/RentifyxAssetRegistry.Api/Messaging/`.
`PeriodicTimer` loop (interval from config, default 5s). Each tick:
1. `QueryAsync` GSI1 `GSI1PK = OUTBOX_STATUS#Pending`, `Limit` batch size (e.g. 25).
2. For each entry: resolve Kafka topic from `EventType` (switch on the 4 known event type names —
   `AssetErrorCodes`-style constants in a new `Domain/Constants/KafkaTopics.cs`), publish
   `Payload` (already-serialized JSON) via `IProducer<string,string>.ProduceAsync` keyed by
   `AssetId` parsed out of the payload (ensures ordering per asset within a partition).
3. On publish success: `UpdateItemAsync` → `Status = Published`, `GSI1PK` removed/changed so it
   drops out of future polls (DynamoDB GSI item disappears from the index once the indexed
   attribute is removed — cheaper than a second query filtering `Status`).
4. On publish failure: increment `RetryCount`; if `< 3`, leave `Status = Pending` (retried next
   tick); if `>= 3`, `Status = Failed` (also removes `GSI1PK`) + `ILogger.LogCritical`.
5. Zero pending entries → skip Kafka entirely, loop back to `PeriodicTimer.WaitForNextTickAsync`
   (DYN-18).
6. `StopAsync` override: signal loop cancellation, await one last in-flight batch (bounded by the
   same `CancellationToken` passed to `ExecuteAsync`), then `producer.Flush(TimeSpan)` — matches
   identity-api's drain pattern.

## DI wiring

`InfrastructureDependencyInjection.Register`: bind `DynamoDbOptions` (table name, region — plain
record via `configuration.GetSection(...).Get<T>()`, one-shot startup read, no `IOptions<T>` per
CLAUDE.md's config-binding rule since nothing besides this extension method reads it directly);
register `IAmazonDynamoDB` (`AmazonDynamoDBClient`, LocalStack `ServiceURL` override read from
config when present) and `IDynamoDBContext` as singletons; register `DynamoDbAssetRepository`/
`DynamoDbCategoryRepository` as `IAssetRepository`/`ICategoryRepository`, scoped.

`OutboxPublisher` needs `IOptions<OutboxOptions>` (poll interval, batch size) — it's a hosted
service DI-constructs, so `IOptions<T>` is the right call here per CLAUDE.md's exception case.
Registered via `builder.Services.AddHostedService<OutboxPublisher>()` in `Program.cs` (Api layer
owns Messaging, so it self-registers there rather than through IoC's Infrastructure extension —
matches sibling repos: hosted services aren't part of the generic IoC composition root). Kafka
`IProducer<string,string>` registered as a singleton in the same Api-layer registration block
(`AddOutboxPublishing(this IServiceCollection, IConfiguration)` extension in
`Api/Extensions/OutboxServiceCollectionExtensions.cs`), config keys added to
`Infrastructure/Constants/ConfigurationKeys.cs` (`Kafka:BootstrapServers`, `AWS:DynamoDb:TableName`,
`AWS:DynamoDb:ServiceUrl` (LocalStack only), `Outbox:PollIntervalSeconds`, `Outbox:BatchSize`,
`Outbox:MaxRetries`).

## AppHost

Add `Aspire.Hosting.Kafka` resource (`builder.AddKafka("kafka")`) wired to the API project via
`WithReference`. No LocalStack Aspire resource — DynamoDB local dev story stays whatever E-01's
existing decision was (checked STATE.md — no LocalStack Aspire resource currently wired; this
feature doesn't add one either, Testcontainers.LocalStack is test-only, matching both sibling
repos where LocalStack only appears in the repository-integration-test project, not AppHost).

## Testing

`03-tests/04-Repositories/RentifyxAssetRegistry.Tests.Repositories/` (project already scaffolded,
empty): `LocalStackFixture` (`IAsyncLifetime`, spins up `LocalStackBuilder("localstack/localstack:latest")`
container once per test class via `ICollectionFixture`, creates the table + 4 GSIs on
`InitializeAsync`), `DynamoDbAssetRepositoryTests`, `DynamoDbCategoryRepositoryTests`,
`OutboxPublisherTests` (drives one manual poll cycle against a real embedded Kafka — Aspire/
Testcontainers doesn't ship a dedicated Kafka Testcontainers package pinned yet in this repo;
reuse `Confluent.Kafka`'s in-proc `Admin`/`Producer`/`Consumer` against a `Testcontainers.Kafka`—
**deviation flagged below**).

**SPEC_DEVIATION:** the brief didn't mention a Kafka Testcontainer explicitly, only
`Testcontainers.LocalStack` for DynamoDB. `OutboxPublisherTests`' "assert...a Kafka consumer on
the test topic receives the message" (spec.md's Independent Test) needs a real broker. Adding
`Testcontainers.Kafka` (same platform, no version pinned elsewhere yet — using the version that
matches `Testcontainers.LocalStack`'s major, `~4.13`) as a test-only package is the smallest
change consistent with "integration-tested... no mocks" in TESTING conventions; documented here
rather than silently added.

## Out-of-scope confirmation

No changes to `IAssetRepository`/`ICategoryRepository` (Domain) — all existing members are
implementable against this schema without modification. No changes to S3/media or Kafka
*consumer* code (F-11/F-12 untouched).
