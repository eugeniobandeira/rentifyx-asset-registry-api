# Tasks — DynamoDB Repository & Outbox (F-10)

Sequential (each depends on prior layers being in place); no `[P]` parallel tasks — this is a
single cohesive Infrastructure slice, splitting it across parallel sub-agents would create more
merge friction than it saves given how much the pieces share (mapper conventions, key schema).

| # | Task | Files | Reqs | Gate |
|---|---|---|---|---|
| 1 | Add packages (`AWSSDK.DynamoDBv2`, `Confluent.Kafka`, `Aspire.Hosting.Kafka`, `Testcontainers.LocalStack`, `Testcontainers.Kafka`) to `Directory.Packages.props` + reference in `Infrastructure.csproj`/`Api.csproj`/`AppHost.csproj`/`Tests.Repositories.csproj` | `Directory.Packages.props`, 4 `.csproj` | — | `dotnet restore` clean |
| 2 | `Infrastructure/Configuration/DynamoDbOptions.cs`, `Infrastructure/Constants/ConfigurationKeys.cs` additions, `Infrastructure/Persistence/DynamoDbKeys.cs` (PK/SK/GSI attribute name + prefix constants) | 3 files | — | builds |
| 3 | POCO items: `AssetItem`, `CategoryItem`, `OutboxItem` in `Infrastructure/Persistence/Items/` | 3 files | DYN-01 | builds |
| 4 | Static mappers: `AssetDynamoDbMapper`, `CategoryDynamoDbMapper`, `OutboxDynamoDbMapper` | 3 files | DYN-01,03 | unit-testable in isolation (no I/O) |
| 5 | `PageTokenCodec` + `InvalidPageTokenException` | 2 files | DYN-10,11 | builds |
| 6 | `DynamoDbAssetRepository`: `GetByIdAsync`, `SaveAsync` (upsert + transactional outbox write + >100-event guard), `SoftDeleteAsync` | 1 file | DYN-01,02,03,06,12,16,17 | builds |
| 7 | `DynamoDbAssetRepository`: `GetByOwnerAsync`, `GetByIdempotencyKeyAsync`, `SearchAsync` (GSI2/GSI4 + FilterExpression + pagination) | same file | DYN-04,05,07,08,09,10,11 | builds |
| 8 | `DynamoDbCategoryRepository`: `GetByIdAsync`, `GetAllAsync` (GSI1 `CATEGORY_LIST`), `SaveAsync` | 1 file | DYN-01,02,03 (category analog) | builds |
| 9 | `KafkaTopics` constants (`Domain/Constants/`) + `OutboxPublisher` (`Api/Messaging/`) with retry/failed/drain logic | 2 files | DYN-13,14,15,18 | builds |
| 10 | IoC wiring: `InfrastructureDependencyInjection` registers `IAmazonDynamoDB`/`IDynamoDBContext`/both repositories; new `Api/Extensions/OutboxServiceCollectionExtensions.cs` registers Kafka producer + `IOptions<OutboxOptions>`; `Program.cs` calls it + `AddHostedService<OutboxPublisher>()` | 3 files | — | `dotnet build` full solution clean |
| 11 | `AppHost.cs`: add `Aspire.Hosting.Kafka` resource, wire to API | 1 file | — | AppHost builds |
| 12 | Repository integration tests: `LocalStackFixture`, `DynamoDbAssetRepositoryTests`, `DynamoDbCategoryRepositoryTests` against real LocalStack DynamoDB | 3+ files | DYN-01..11,16,17 | `dotnet test` (Repositories project) green |
| 13 | `OutboxPublisherTests` against LocalStack DynamoDB + `Testcontainers.Kafka` | 1-2 files | DYN-12..15,18 | `dotnet test` green |
| 14 | Full-solution build + full test suite run | — | all | `dotnet build` Release + `dotnet test` all green |
| 15 | Update `.specs/project/STATE.md` + `ROADMAP.md`, write ADR-AR-009 (DynamoDB single-table) + ADR-AR-010 (Outbox poll-loop, corrected per spec.md's Problem Statement) | 4 files | — | reviewed |
| 16 | Commit per logical step (already interleaved above), push, open PR | — | — | `gh pr create` |

Requirement coverage: all 18 DYN-xx IDs land in tasks 3–9 + 12–13. 0 unmapped.
