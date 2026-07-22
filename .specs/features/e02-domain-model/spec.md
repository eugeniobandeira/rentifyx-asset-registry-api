# E-02 · Domain Model & Core Asset Logic Specification

## Problem Statement

The repo currently has only the `dotnet new clean-arch` scaffold (`Example*` feature against EF Core/Postgres). There is no real domain model for RentifyX Asset Registry. Before any use case (E-03), repository (E-04), or endpoint (E-05) work can start, we need a pure, framework-free Domain layer: the `AssetEntity` aggregate, its value objects, `CategoryEntity`, domain events, and repository/service contracts that Infrastructure will later implement.

## Goals

- [ ] `AssetEntity` aggregate enforces all invariants at construction/transition time — impossible to build an invalid asset
- [ ] Zero framework/AWS references in `02-src/03-Domain` (verified by dependency check)
- [ ] 100% unit-tested Domain layer, no I/O in any test
- [ ] Repository/service contracts defined so Infrastructure (E-04) and Application (E-03) can be built against stable interfaces

## Out of Scope

| Feature | Reason |
|---|---|
| Application handlers (CreateAsset, RequestMediaUpload, etc.) | E-03 |
| DynamoDB/S3 implementations of the contracts | E-04 |
| API endpoints | E-05 |
| Moderation verdict processing logic | E-03 (F-08) — only the domain event/status shape lands here |
| Multi-currency support | Explicit decision: BRL-only for now, no FX |
| `IOwnerStatusValidator` fail-open/fail-closed behavior | Open question in STATE.md — contract defined here, behavior decided in E-04 |

---

## User Stories

### P1: Asset Aggregate & Value Objects ⭐ MVP

**User Story**: As a domain expert, I want a rich `AssetEntity` aggregate that enforces business rules so invalid assets can never exist in memory or storage.

**Why P1**: Every later layer (Application, Infrastructure, API) depends on this shape existing and being trustworthy.

**Acceptance Criteria**:

1. WHEN `AssetEntity.Create(...)` is called with valid `OwnerId`, `Title`, `Description`, `CategoryId` THEN system SHALL return a new entity with `Status = Draft`, `CreatedAt`/`UpdatedAt` set, and no public constructor available
2. WHEN `AssetTitle` is constructed with a value outside 3–100 characters THEN system SHALL throw `ArgumentException` (guard clause, not `ErrorOr` — construction-time invariant)
3. WHEN `AssetDescription` is constructed with a value outside 10–2000 characters THEN system SHALL throw `ArgumentException`
4. WHEN `Money` is constructed with a negative amount THEN system SHALL throw `ArgumentException`
5. WHEN `Money` is constructed THEN system SHALL fix `Currency = "BRL"` (no currency parameter accepted yet)
6. WHEN `Media` is constructed with an empty S3 key, invalid MIME type, or non-positive size THEN system SHALL throw `ArgumentException`
7. WHEN `AssetStatus` is used THEN system SHALL expose exactly the 5 values: `Draft`, `PendingModeration`, `Active`, `Suspended`, `Archived`

**Independent Test**: Unit tests construct each VO/entity directly with valid and invalid inputs, assert success or exception — no mocks, no I/O.

---

### P1: Category Entity ⭐ MVP

**User Story**: As a domain expert, I want `CategoryEntity` to support admin-managed nested taxonomy so assets can be organized without a flat/unstructured category list.

**Why P1**: `AssetEntity.CategoryId` is a required field from day one; Category must exist before Asset creation use cases (E-03) can be built.

**Acceptance Criteria**:

1. WHEN `CategoryEntity.Create(...)` is called with a `Name` and no `ParentCategoryId` THEN system SHALL create a root-level category (depth 1)
2. WHEN a category chain (via `ParentCategoryId`) would exceed depth 3 THEN system SHALL reject creation (domain service/guard, exact mechanism decided at implementation — see Edge Cases)
3. WHEN `ParentCategoryId` would introduce a cycle (a category becomes its own ancestor) THEN system SHALL reject creation

**Independent Test**: Unit tests build category chains of depth 1, 2, 3 (pass) and 4 (fail); attempt a cycle (fail).

---

### P1: Domain Events ⭐ MVP

**User Story**: As a dev, I want domain events raised on state changes so Application/Infrastructure (E-03/E-04) can publish them to Kafka without the Domain layer knowing about messaging.

**Why P1**: `AssetCreated`/`AssetMediaUploaded`/`AssetPublished`/`AssetSuspended` are named explicitly in CLAUDE.md's Messaging section as the outbound contract — this is the shape everything downstream builds on.

**Acceptance Criteria**:

1. WHEN `AssetEntity.Create(...)` succeeds THEN system SHALL raise `AssetCreated` (AssetId, OwnerId, CategoryId, OccurredAt)
2. WHEN media is attached to an asset THEN system SHALL raise `AssetMediaUploaded`
3. WHEN an asset transitions Draft/PendingModeration → Active THEN system SHALL raise `AssetPublished`
4. WHEN an asset transitions → Suspended THEN system SHALL raise `AssetSuspended` (reason, suspendedBy)
5. WHEN any domain event is raised THEN it SHALL be collected via `AggregateRoot.RaiseDomainEvent()`, not published directly (Domain has no messaging dependency)

**Independent Test**: Unit tests call state-transition methods on `AssetEntity`, assert the correct event type/payload appears in `DomainEvents` collection.

---

### P1: Repository & Service Contracts ⭐ MVP

**User Story**: As a dev, I want Domain-layer interfaces so Infrastructure (E-04) can be swapped freely and Application (E-03) can be built/tested against contracts today.

**Why P1**: E-03 (Day 8–13) is blocked without these interfaces existing first.

**Acceptance Criteria**:

1. WHEN `IAssetRepository` is defined THEN it SHALL expose `GetByIdAsync`, `GetByOwnerAsync`, `SaveAsync`, `SoftDeleteAsync`, `SearchAsync`
2. WHEN `ICategoryRepository` is defined THEN it SHALL expose `GetByIdAsync`, `GetAllAsync`, `SaveAsync`
3. WHEN `IMediaStorageService` is defined THEN it SHALL expose `GeneratePresignedUploadUrlAsync`, `ValidateUploadAsync`
4. WHEN `IOwnerStatusValidator` is defined THEN it SHALL expose `IsOwnerActiveAsync(ownerId)`
5. WHEN any of the above interfaces are added THEN they SHALL live under `Domain/Interfaces/{Concept}/` per CLAUDE.md convention (e.g. `Domain/Interfaces/Asset/IAssetRepository.cs`), never loose under `Interfaces/`

**Independent Test**: Compile-time check — interfaces exist, are referenced by no concrete implementation yet (Infrastructure lands in E-04).

---

### P2: State Lifecycle Transition Guards

**User Story**: As a domain expert, I want illegal status transitions rejected so an asset can never skip moderation or resurrect from Archived.

**Why P2**: Important for correctness but the exact transition graph is exercised more thoroughly once E-03's `SubmitForModeration`/`AdminReviewAsset` handlers exist; Domain layer needs the guard now, full workflow later.

**Acceptance Criteria**:

1. WHEN transitioning Draft → Active directly (skipping PendingModeration) THEN system SHALL throw (invalid transition)
2. WHEN transitioning Archived → any other status THEN system SHALL throw (terminal state)
3. WHEN transitioning Suspended → Active THEN system SHALL succeed (reinstatement path)

**Independent Test**: Unit tests assert `AssetEntity` exposes only valid transition methods and each rejects out-of-order calls.

---

## Edge Cases

- WHEN `AssetTitle`/`AssetDescription` is null, empty, or whitespace-only THEN system SHALL throw via `ArgumentException.ThrowIfNullOrWhiteSpace` before length validation
- WHEN `Category` depth-limit check needs cross-entity knowledge (parent's own depth) THEN this may require a domain service rather than a pure constructor guard — implementer decides based on whether `CategoryEntity.Create` can be given the parent chain or only a `ParentCategoryId`; document the choice in ADR-AR-003
- WHEN `Media` MIME type is provided in a case-inconsistent form (`Image/JPEG` vs `image/jpeg`) THEN system SHALL normalize before validating against the allowed set
- WHEN `Money.Amount` is exactly `0` THEN system SHALL allow it (free/inquire-for-price listings are valid; only negative is rejected)

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| DOM-01 | P1: Asset Aggregate & VOs | Design | Pending |
| DOM-02 | P1: Asset Aggregate & VOs | Design | Pending |
| DOM-03 | P1: Category Entity | Design | Pending |
| DOM-04 | P1: Domain Events | Design | Pending |
| DOM-05 | P1: Repository & Service Contracts | Design | Pending |
| DOM-06 | P2: State Lifecycle Transition Guards | Design | Pending |

**ID format:** `DOM-[NUMBER]`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 6 total, 0 mapped to tasks, 6 unmapped ⚠️ (mapped once tasks.md is created)

---

## Success Criteria

- [ ] `dotnet build` on `RentifyxAssetRegistry.Domain` project has zero references to `Microsoft.EntityFrameworkCore`, `AWSSDK.*`, or any other framework/cloud package
- [ ] `dotnet test` — all Domain unit tests pass, 100% of new VOs/entities/events covered
- [ ] ADR-AR-002 (status lifecycle) and ADR-AR-003 (Category as entity) written and committed to `docs/decisions/`
- [ ] `IAssetRepository`, `ICategoryRepository`, `IMediaStorageService`, `IOwnerStatusValidator` compile with no implementation, ready for E-03/E-04 to consume
