# DynamoDB Repository & Outbox (F-10 / US-016, US-017) Specification

## Problem Statement

`IAssetRepository`/`ICategoryRepository` (Domain, E-02) have no concrete implementation —
every Application handler built in E-03 depends on them but nothing is registered in DI. This
feature implements the DynamoDB-backed repositories plus the Outbox delivery mechanism that
turns `AssetEntity`'s raised domain events (`AssetCreated`, `AssetMediaUploaded`, `AssetPublished`,
`AssetSuspended`) into Kafka messages.

**Cross-repo research done before writing this spec (not guessed):**
- `rentifyx-identity-api`'s ADR-005: single-table DynamoDB design, `IDynamoDBContext` (high-level
  SDK) + custom POCO mapper classes (`XDynamoDbMapper.ToItem/ToEntity`), GSI-only queries (no
  Scan), `IAmazonDynamoDB.TransactWriteItemsAsync` for atomic multi-item writes.
- `rentifyx-identity-api`'s DEF-005: **rejected DynamoDB Streams** for Outbox delivery ("would
  need a Lambda or separate compute process this repo doesn't have") in favor of a `PeriodicTimer`
  poll-loop `IHostedService` (`OutboxPublisher`), living in `Api/Messaging/` not Infrastructure, no
  separate Outbox table (item type in the same single table). This directly contradicts this
  repo's plan text ("ADR-AR-010: DynamoDB Streams... consistent with identity-api's DEF-005") —
  the plan was wrong; corrected here after verifying against the real repo.
- `rentifyx-communications-api` diverges from identity-api: low-level `IAmazonDynamoDB` instead of
  `IDynamoDBContext`, no Outbox precedent (it's a Kafka consumer/reactor, not an event producer).
  Since this repo's plan explicitly says "ADR-AR-009 mirrors identity-api ADR-005", identity-api's
  pattern is authoritative here, not communications-api's.
- User decision (2026-07-22): Outbox = poll-loop `IHostedService`, matching identity-api. Owner-status
  cache (F-12, separate feature) will be DynamoDB-backed for cross-pod consistency (EKS multi-replica),
  sharing the `IAmazonDynamoDB` client registration with this feature.

## Goals

- [ ] `DynamoDbAssetRepository`/`DynamoDbCategoryRepository` implement all existing Domain
      interface members (`IGetByIdRepository`, `ISaveRepository`, `ISoftDeleteRepository`,
      `ISearchRepository<AssetEntity, AssetSearchFilter>` for assets; `IGetByIdRepository`,
      `IGetAllRepository`, `ISaveRepository` for categories) against a real single-table design
- [ ] `AssetSearchFilter`'s cursor pagination (`NextPageToken`) maps to DynamoDB's real
      `LastEvaluatedKey` pagination token — no offset emulation
- [ ] Domain events raised on `AssetEntity` during a handler's `SaveAsync` call are reliably
      delivered to Kafka via the Outbox poll-loop, even across process restarts
- [ ] All repository + Outbox behavior integration-tested against `Testcontainers.LocalStack`
      (matches both sibling repos' convention — no dedicated DynamoDB Testcontainers package used
      platform-wide)

## Out of Scope

| Feature | Reason |
|---|---|
| S3 Media Storage (`S3MediaStorageService`) | Separate feature, F-11 — independent AWS service, no code overlap |
| Kafka consumers (`UserSuspended`/`UserDeleted`, `AssetMediaModerated`) | Separate feature, F-12 — consumption is a different concern from this feature's production side |
| DynamoDB Streams | Explicitly rejected — see Problem Statement; Streams infrastructure (event source mapping, Lambda) is not built |
| A dedicated Outbox DynamoDB table | Matches identity-api: Outbox entries are items in the same single table (`OUTBOX#{id}` partition), not a second table |
| GSI-backed full `contains` keyword search at scale | ADR-AR-007 watch item, past ~10k assets — this feature makes `Keyword` work via GSI2 (status) + client -side/expression filter on `Title`, acceptable at current expected volume, not optimized further |

---

## User Stories

### P1: Persist and retrieve assets via DynamoDB ⭐ MVP

**User Story**: As the system, I want `CreateAsset`/`SubmitForModeration`/`ApplyModerationVerdict`/
`AdminReviewAsset`/`ConfirmMediaUpload` to actually persist to a real database so the Application
layer built in E-03 stops being untestable end-to-end.

**Why P1**: Every existing handler is blocked without this — it's the single biggest gap left
before any endpoint (E-05) can go live.

**Acceptance Criteria**:

1. WHEN `SaveAsync` is called with a new `AssetEntity` THEN system SHALL write it as a single
   `PutItem` under `PK=ASSET#{id}`, `SK=ASSET#{id}` (single-table, no composite sort key needed
   for the primary item)
2. WHEN `SaveAsync` is called with an existing `AssetEntity` (same `Id`) THEN system SHALL
   overwrite the item (upsert semantics, matches `ISaveRepository`'s single-verb contract)
3. WHEN `GetByIdAsync` is called for an existing asset THEN system SHALL return the fully
   deserialized `AssetEntity` (all VOs reconstructed: `AssetTitle`, `AssetDescription`, `Money`,
   `Media`)
4. WHEN `GetByOwnerAsync` is called THEN system SHALL query GSI1 (`PK=OWNER#{ownerId}`), not Scan
5. WHEN `GetByIdempotencyKeyAsync` is called THEN system SHALL query GSI3 (`PK=IDEMPOTENCY#{key}`), not Scan
6. WHEN `SoftDeleteAsync` is called THEN system SHALL flip the item's status attribute via
   `UpdateItem`, not delete the DynamoDB item itself

**Independent Test**: Against LocalStack, save an asset, read it back by ID/owner/idempotency key, assert full round-trip equality.

---

### P1: Search assets with cursor pagination ⭐ MVP

**User Story**: As the system, I want `SearchAssetsHandler`'s `AssetSearchFilter` to actually query
DynamoDB so F-08's search feature stops being handler-only.

**Why P1**: F-08 shipped with the Application contract but zero working implementation — this
closes that gap.

**Acceptance Criteria**:

1. WHEN `SearchAsync` is called with `CategoryId` set THEN system SHALL query GSI2
   (`PK=CATEGORY#{categoryId}`, filtered to `Status=Active` server-side via `FilterExpression`)
2. WHEN `SearchAsync` is called with no `CategoryId` THEN system SHALL query GSI4
   (`PK=STATUS#Active`) — never Scan the whole table
3. WHEN `MinPrice`/`MaxPrice`/`Keyword` are set THEN system SHALL apply them as a DynamoDB
   `FilterExpression` on top of the GSI query (post-query filtering, not part of the key schema —
   consistent with the Out of Scope note on `contains` search at scale)
4. WHEN more results exist than `PageSize` THEN system SHALL return a `NextPageToken` that is the
   Base64-encoded `LastEvaluatedKey`, and WHEN that token is supplied on a subsequent call THEN
   system SHALL resume via `ExclusiveStartKey`
5. WHEN `NextPageToken` is malformed/tampered (fails to decode into a valid key) THEN system SHALL
   return a validation error, not throw an unhandled deserialization exception

**Independent Test**: Seed 40 assets across 2 categories, search with `PageSize=10`, walk all pages via `NextPageToken`, assert every asset seen exactly once.

---

### P1: Reliable Outbox delivery to Kafka ⭐ MVP

**User Story**: As the system, I want domain events raised during a save (`AssetCreated`,
`AssetMediaUploaded`, `AssetPublished`, `AssetSuspended`) to reach Kafka reliably, even if the
process crashes between the DB write and the publish.

**Why P1**: Without this, `AssetCreated` (which `rentifyx-ai-services` depends on to trigger
moderation) can silently never fire — breaks the entire moderation pipeline (F-09).

**Acceptance Criteria**:

1. WHEN `SaveAsync` persists an `AssetEntity` with pending domain events THEN system SHALL write
   the entity item AND one Outbox item per domain event in a single `TransactWriteItems` call
   (atomic — matches identity-api's `WriteTransactionallyAsync` pattern)
2. WHEN the `OutboxPublisher` `IHostedService` polls THEN system SHALL query pending entries via a
   GSI (`PK=OUTBOX_STATUS#Pending`), publish each to the correct Kafka topic
   (`AssetCreated`/`AssetMediaUploaded`/`AssetPublished`/`AssetSuspended`, per CLAUDE.md's topic
   list), then mark it `Published`
3. WHEN a publish attempt fails THEN system SHALL increment a retry counter and retry up to 3
   times, then mark the entry `Failed` and log at `Critical` (matches identity-api — no DLQ topic,
   a `Failed` status flag)
4. WHEN the host shuts down THEN system SHALL best-effort drain the current batch before stopping
   (matches identity-api's `StopAsync` drain + producer flush)

**Independent Test**: Save an asset (triggers `AssetCreated`), assert an `OUTBOX#` item exists with `Status=Pending`, run one `OutboxPublisher` poll cycle, assert the item flips to `Published` and a Kafka consumer on the test topic receives the message.

---

## Edge Cases

- WHEN `TransactWriteItems` exceeds DynamoDB's 100-item-per-transaction limit (many domain events
  raised in one save) THEN system SHALL fail loudly (`InvalidOperationException`, a genuine
  programmer-error guard) rather than silently truncate — not expected in practice (`AssetEntity`
  never raises more than one event per method call today), but must not corrupt data if it ever did
- WHEN two concurrent `SaveAsync` calls race on the same `AssetEntity.Id` THEN system SHALL let
  last-write-win (no optimistic locking in this pass — `ISaveRepository`'s single-upsert contract
  doesn't model version conflicts; flagged as a deferred idea, not solved here)
- WHEN `GetByIdAsync` is called for a non-existent ID THEN system SHALL return `null`, not throw
- WHEN the Outbox poll finds zero pending entries THEN system SHALL sleep for the configured
  interval without hitting Kafka at all

## Known Gap (documented, not solved here)

No optimistic concurrency control (version attribute / conditional writes) on `SaveAsync` —
last-write-wins. Acceptable for now given current traffic assumptions; revisit if concurrent
edits to the same asset become a real scenario (e.g. simultaneous admin review + owner edit).

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| DYN-01 | P1: PutItem write, single-table PK/SK | Infrastructure | Pending |
| DYN-02 | P1: upsert semantics | Infrastructure | Pending |
| DYN-03 | P1: GetById round-trip | Infrastructure | Pending |
| DYN-04 | P1: GetByOwner via GSI, no Scan | Infrastructure | Pending |
| DYN-05 | P1: GetByIdempotencyKey via GSI, no Scan | Infrastructure | Pending |
| DYN-06 | P1: SoftDelete via UpdateItem, not delete | Infrastructure | Pending |
| DYN-07 | P1: Search by category via GSI | Infrastructure | Pending |
| DYN-08 | P1: Search unfiltered via status GSI | Infrastructure | Pending |
| DYN-09 | P1: price/keyword as FilterExpression | Infrastructure | Pending |
| DYN-10 | P1: cursor pagination via real LastEvaluatedKey | Infrastructure | Pending |
| DYN-11 | P1: malformed token → validation error | Infrastructure | Pending |
| DYN-12 | P1: atomic entity+outbox TransactWriteItems | Infrastructure | Pending |
| DYN-13 | P1: OutboxPublisher polls and publishes | Infrastructure | Pending |
| DYN-14 | P1: retry-then-Failed after 3 attempts | Infrastructure | Pending |
| DYN-15 | P1: graceful shutdown drain | Infrastructure | Pending |
| DYN-16 | Edge: >100 events per transaction fails loudly | Infrastructure | Pending |
| DYN-17 | Edge: GetById returns null for missing asset | Infrastructure | Pending |
| DYN-18 | Edge: idle poll doesn't touch Kafka | Infrastructure | Pending |

**Coverage:** 18 total, 18 mapped to a task breakdown (Tasks phase — this feature is Complex/multi-component, tasks.md follows), 0 unmapped

---

## Success Criteria

- [ ] All E-03 handlers work end-to-end against a real (LocalStack) DynamoDB table — no more `NotImplementedException` gap in DI
- [ ] `AssetCreated` reliably reaches a Kafka topic within one poll interval of a save, verified by an integration test that doesn't mock Kafka
- [ ] Zero table Scans anywhere in the repository implementation (verified by code review — GSI queries only, matches both sibling repos' discipline)
