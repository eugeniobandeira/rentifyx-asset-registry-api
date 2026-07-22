# E-03 · F-05 Asset Creation & Idempotency Specification

**Scope note:** first feature slice of E-03 (Application Layer). Covers US-010 (T-048–T-054) in full. US-011 (Draft TTL auto-expiry, T-055–T-057) is deferred — it's primarily a DynamoDB TTL configuration (E-04/Infrastructure), and the only Domain-layer touchpoint (a `DraftExpiresAt`-style field) can be added later without reworking this slice.

## Problem Statement

An asset owner needs to create a listing. The handler must be safe to retry (idempotent), must reject creation from a suspended/deleted owner, and must produce a persisted `AssetEntity` in `Draft` status with `AssetCreated` already raised (for the DynamoDB Streams outbox, E-04, to pick up later — this handler does not publish to Kafka directly).

## Goals

- [ ] `CreateAssetHandler` returns the same result on a retried request with the same idempotency key (no duplicate asset created)
- [ ] Creation is rejected when `IOwnerStatusValidator` reports the owner is not active
- [ ] All FluentValidation rules enforce the same limits as the Domain VOs (title/description length, valid category reference format) so invalid requests fail fast before touching the repository

## Out of Scope

| Item | Reason |
|---|---|
| Draft TTL auto-expiry (US-011) | DynamoDB TTL config is an E-04 concern; deferred |
| Actual DynamoDB conditional-write atomicity | `IAssetRepository`/`DynamoDbAssetRepository` is E-04; this slice implements the idempotency *check* against the repository contract (`GetByIdempotencyKeyAsync`), which is correct but not yet race-proof under truly concurrent identical requests — E-04 hardens this with a conditional `PutItem` |
| Publishing `AssetCreated` to Kafka | Per ADR-AR-010 (DynamoDB Streams as outbox), this handler never publishes directly — it just ensures the event exists on the entity before `SaveAsync`; the outbox bridge is E-04 |
| Category existence validation beyond format | Whether `CategoryId` refers to a real `CategoryEntity` requires `ICategoryRepository.GetByIdAsync` — folding this in now vs. treating it as a follow-up is a design question, not assumed here |

## User Stories

### P1: Create Asset with Idempotency ⭐ MVP

**User Story**: As an owner, I want to create an asset listing so I can offer it for rent, and safely retry the request without creating duplicates.

**Acceptance Criteria**:

1. WHEN `CreateAssetRequest` (OwnerId, Title, Description, CategoryId, IdempotencyKey) passes validation THEN system SHALL check `IOwnerStatusValidator.IsOwnerActiveAsync(OwnerId)` before creating anything
2. WHEN the owner is not active THEN system SHALL return an `ErrorOr` failure (no asset created, no repository write)
3. WHEN `IAssetRepository.GetByIdempotencyKeyAsync(IdempotencyKey)` finds an existing asset THEN system SHALL return that asset's response (idempotent replay), SHALL NOT create a second one
4. WHEN no existing asset is found for the idempotency key AND the owner is active THEN system SHALL call `AssetEntity.Create(...)`, attach the idempotency key, and call `IAssetRepository.SaveAsync`
5. WHEN save succeeds THEN system SHALL return a `CreateAssetResponse` (AssetId, Status, CreatedAt)

**Independent Test**: Mock `IAssetRepository`/`IOwnerStatusValidator` with Moq; assert each branch (suspended owner, duplicate idempotency key, happy path) independently.

---

### P1: Validation Rules ⭐ MVP

**User Story**: As a dev, I want `CreateAssetValidator` to reject malformed requests before they reach the handler.

**Acceptance Criteria**:

1. WHEN `Title` is null/too short/too long THEN validator SHALL fail (mirrors `AssetTitle` bounds: 3–100)
2. WHEN `Description` is null/too short/too long THEN validator SHALL fail (mirrors `AssetDescription` bounds: 10–2000)
3. WHEN `CategoryId` is `Guid.Empty` THEN validator SHALL fail
4. WHEN `IdempotencyKey` is null/whitespace THEN validator SHALL fail
5. WHEN `OwnerId` is `Guid.Empty` THEN validator SHALL fail

**Independent Test**: `CreateAssetValidatorTests` — no mocks, one test per rule per CLAUDE.md's Validators test convention.

---

## Domain Changes Required (prerequisite, not a separate epic)

- `AssetEntity` gains an `IdempotencyKey` property (string, required, set at `Create(...)` time — factory signature grows a parameter)
- `IAssetRepository` gains `Task<AssetEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)`
- New unit tests for both, following E-02's established `Tests.Domain` patterns

## Requirement Traceability

| ID | Requirement | Status |
|---|---|---|
| CA-01 | Idempotency check via `GetByIdempotencyKeyAsync` before create | Pending |
| CA-02 | Owner-status check before create | Pending |
| CA-03 | `CreateAssetValidator` mirrors Domain VO bounds | Pending |
| CA-04 | `AssetEntity.IdempotencyKey` + repository contract extension | Pending |

## Success Criteria

- [ ] `dotnet test` — new Handler tests (Moq-based) and Validator tests all pass
- [ ] `dotnet build` — zero warnings, `TreatWarningsAsErrors` respected
- [ ] Handler touches only `IAssetRepository`/`IOwnerStatusValidator` (Domain contracts) — no Infrastructure/AWS reference in Application layer
