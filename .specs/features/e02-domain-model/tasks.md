# E-02 · Domain Model & Core Asset Logic Tasks

**Design**: `.specs/features/e02-domain-model/design.md`
**Status**: Done — all 22 tasks complete, 60/60 Tests.Domain green, zero framework deps in Domain layer

**Mid-flight change (not in original plan):** repository interfaces (T16/T17) were revised after task completion to compose from new generic building blocks (`ISaveRepository<T>`, `ISearchRepository<T,TFilter>`, `ISoftDeleteRepository`, `IGetAllRepository<T>`) added to `Domain/Interfaces/Common/`, instead of the flat custom interfaces originally written. Rationale and convention documented in `CLAUDE.md`. See commit `87fb9bf`.

---

## Test Coverage Matrix (no `.specs/codebase/TESTING.md` yet — derived from CLAUDE.md + confirmed sibling-repo precedent)

Confirmed against `rentifyx-communications-api` (newer sibling, already has this exact gap solved): domain unit tests live in a dedicated `03-tests/00-Domain/{Solution}.Tests.Domain/` project — xUnit + FluentAssertions only, **no mocks, no I/O**. Subfolders mirror Domain layer folders: `Entities/`, `ValueObjects/`, `Constants/`. This repo (`rentifyx-identity-api`) predates that convention and has no `00-Domain` — we're adopting the newer pattern since it directly fits this feature.

| Code Layer | Test Type | Parallel-Safe |
|---|---|---|
| Domain value object (`record`, factory + guards) | unit | Yes |
| Domain entity/aggregate (`AssetEntity`, `CategoryEntity`) | unit | Yes |
| Domain event (`record : IDomainEvent`) | unit | Yes |
| Domain interface (no implementation) | none (compile-check only) | Yes |
| ADR markdown | none | Yes |

**Gate Check Commands:**

- **quick**: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- **full**: `dotnet build RentifyxAssetRegistry.slnx --configuration Release && dotnet test RentifyxAssetRegistry.slnx`

---

## Execution Plan

### Phase 0: Foundation (Sequential)

```
T01 → T02 → T03 → T04
```

### Phase 1: Value Objects (Parallel OK, after T01+T02)

```
T02 ──┬→ T05 [P]
      ├→ T06 [P]
      ├→ T07 [P]
      └→ T08 [P]
```

### Phase 2: Domain Events (Parallel OK, after T03)

```
T03 ──┬→ T09 [P]
      ├→ T10 [P]
      ├→ T11 [P]
      └→ T12 [P]
```

### Phase 3: Aggregates (Parallel OK, after Phase 1 + Phase 2 + T04)

```
T04, T05, T06, T07, T08, T09, T10, T11, T12 ──┬→ T13 (AssetEntity)
                                                └→ T14 [P] (CategoryEntity)
```

### Phase 4: Contracts (Parallel OK, after Phase 3)

```
T13 ──┬→ T15 ──→ T16 [P]
T13 ──────────────────────┐
T14 ──────────────────→ T17 [P]
T08 ──────────────────→ T18 [P]
(none) ────────────────→ T19 [P]
```

### Phase 5: Docs & Final Review (Sequential)

```
T13 → T20
T14 → T21
T20, T21, T16, T17, T18, T19 → T22
```

---

## Task Breakdown

### T01: Create `Tests.Domain` project scaffold

**What**: New xUnit test project `RentifyxAssetRegistry.Tests.Domain` at `03-tests/00-Domain/`, registered in `.slnx`, referencing `RentifyxAssetRegistry.Domain`
**Where**: `03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`, `RentifyxAssetRegistry.slnx`
**Depends on**: None
**Reuses**: `03-tests/00-Domain/RentifyxCommunications.Tests.Domain/*.csproj` from `rentifyx-communications-api` as the exact template (package set: xunit, xunit.runner.visualstudio, FluentAssertions, Microsoft.NET.Test.Sdk, coverlet.collector)
**Requirement**: DOM-01..06 (enabling infra for all)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `.csproj` created with `IsPackable=false`, `ProjectReference` to `Domain.csproj`, packages match sibling repo
- [ ] Project added to `RentifyxAssetRegistry.slnx`
- [ ] `dotnet build RentifyxAssetRegistry.slnx` succeeds with the new empty project
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none (scaffold only)
**Gate**: build

---

### T02: Add `AggregateRoot` + `IDomainEvent`

**What**: `IDomainEvent` marker interface + `AggregateRoot` base class with `RaiseDomainEvent`/`DomainEvents`/`ClearDomainEvents`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Common/IDomainEvent.cs`, `.../Common/AggregateRoot.cs`
**Depends on**: T01
**Reuses**: `Domain/Common/PagedResult.cs` folder as precedent for where shared Domain primitives live
**Requirement**: DOM-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `IDomainEvent` exposes `DateTime OccurredAt`
- [ ] `AggregateRoot` exposes `IReadOnlyCollection<IDomainEvent> DomainEvents`, `protected RaiseDomainEvent(IDomainEvent)`, `ClearDomainEvents()`
- [ ] Unit test: raising 2 events results in `DomainEvents.Count == 2`; `ClearDomainEvents()` empties it
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 2+ tests pass

**Tests**: unit
**Gate**: quick

---

### T03: Create `AssetStatus` + `MediaUploadStatus` enums

**What**: Two enums — `AssetStatus` (`Draft, PendingModeration, Active, Suspended, Archived`) and `MediaUploadStatus` (`Pending, Uploaded, Failed`)
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Enums/AssetStatus.cs`, `.../Enums/MediaUploadStatus.cs`
**Depends on**: T01
**Reuses**: Nothing existing (new `Enums/` folder)
**Requirement**: DOM-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Both enums compile with exact member names above, no explicit numeric values assigned (default underlying is fine since nothing persists yet — E-04 handles string serialization)
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none (no logic to test — pure enum declarations)
**Gate**: build

---

### T04: Add `ValidationConstants` entries for Asset/Category/Media

**What**: Add `AssetRules` (TitleMinLength=3, TitleMaxLength=100, DescriptionMinLength=10, DescriptionMaxLength=2000), `CategoryRules` (MaxDepth=3), `MediaRules` (AllowedMimeTypes set) to the existing `ValidationConstants.cs`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Constants/ValidationConstants.cs` (modify — read current content first, append alongside existing `ExampleRules`)
**Depends on**: T01
**Reuses**: Existing `ValidationConstants.cs` file/class structure
**Requirement**: DOM-01, DOM-03

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `AssetRules.TitleMinLength/TitleMaxLength/DescriptionMinLength/DescriptionMaxLength` defined as `const int`
- [ ] `CategoryRules.MaxDepth = 3` defined as `const int`
- [ ] `MediaRules.AllowedMimeTypes` defined as a static readonly set (implementer picks concrete allowed types — flag as assumption if plan doesn't enumerate them; e.g. `image/jpeg, image/png, image/webp, video/mp4`)
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none (constants only, exercised by T05-T08 tests)
**Gate**: build

---

### T05: `AssetTitle` value object [P]

**What**: `sealed record AssetTitle` with `private` constructor + `static Create(string value)` factory enforcing 3–100 chars
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/ValueObjects/AssetTitle.cs`
**Depends on**: T02, T04
**Reuses**: `ExampleEntity.Create` ctor-guard pattern (style); `AssetRules` constants (T04)
**Requirement**: DOM-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Throws `ArgumentException` on null/whitespace, <3 chars, >100 chars
- [ ] Succeeds on 3-char and 100-char boundary values
- [ ] Unit tests: valid, too-short, too-long, null/whitespace cases
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 4+ tests pass

**Tests**: unit
**Gate**: quick

---

### T06: `AssetDescription` value object [P]

**What**: Same shape as T05, enforcing 10–2000 chars
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/ValueObjects/AssetDescription.cs`
**Depends on**: T02, T04
**Reuses**: Same pattern as T05
**Requirement**: DOM-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Throws on null/whitespace, <10 chars, >2000 chars; succeeds at boundaries
- [ ] Unit tests: valid, too-short, too-long, null/whitespace cases
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 4+ tests pass

**Tests**: unit
**Gate**: quick

---

### T07: `Money` value object [P]

**What**: `sealed record Money` with `private` constructor + `static Create(decimal amount)` factory; `Currency` fixed to `"BRL"`; rejects negative amounts, allows `0`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/ValueObjects/Money.cs`
**Depends on**: T02
**Reuses**: Same ctor-guard pattern
**Requirement**: DOM-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Throws `ArgumentException` on negative amount
- [ ] Succeeds on `0` and positive amounts
- [ ] `Currency` property always returns `"BRL"`, no constructor parameter for it
- [ ] Unit tests: negative (throws), zero (succeeds), positive (succeeds), currency fixed value assertion
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 4+ tests pass

**Tests**: unit
**Gate**: quick

---

### T08: `Media` value object [P]

**What**: `sealed record Media` (multi-line per convention) with S3Key, MimeType, SizeBytes, Status; guards empty S3Key, invalid/unnormalized MIME type, non-positive size
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/ValueObjects/Media.cs`
**Depends on**: T02, T03, T04
**Reuses**: Same ctor-guard pattern; `MediaRules.AllowedMimeTypes` (T04); `MediaUploadStatus` (T03)
**Requirement**: DOM-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Throws on empty/whitespace S3Key, non-positive SizeBytes, MIME type not in `AllowedMimeTypes`
- [ ] MIME type normalized to lowercase before validation (`Image/JPEG` accepted same as `image/jpeg`)
- [ ] Unit tests: valid, empty key, bad size, disallowed MIME, mixed-case MIME normalization
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 5+ tests pass

**Tests**: unit
**Gate**: quick

---

### T09: `AssetCreated` domain event [P]

**What**: `sealed record AssetCreated(Guid AssetId, Guid OwnerId, Guid CategoryId, DateTime OccurredAt) : IDomainEvent`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Events/Asset/AssetCreated.cs`
**Depends on**: T02
**Reuses**: `IDomainEvent` (T02)
**Requirement**: DOM-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Record compiles, implements `IDomainEvent`, multi-line parameter list per CLAUDE.md
- [ ] Unit test: constructs with values, asserts properties round-trip
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 1+ test passes

**Tests**: unit
**Gate**: quick

---

### T10: `AssetMediaUploaded` domain event [P]

**What**: `sealed record AssetMediaUploaded(Guid AssetId, string S3Key, DateTime OccurredAt) : IDomainEvent`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Events/Asset/AssetMediaUploaded.cs`
**Depends on**: T02
**Reuses**: `IDomainEvent` (T02)
**Requirement**: DOM-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Record compiles, implements `IDomainEvent`
- [ ] Unit test: constructs with values, asserts properties round-trip
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 1+ test passes

**Tests**: unit
**Gate**: quick

---

### T11: `AssetPublished` domain event [P]

**What**: `sealed record AssetPublished(Guid AssetId, DateTime OccurredAt) : IDomainEvent`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Events/Asset/AssetPublished.cs`
**Depends on**: T02
**Reuses**: `IDomainEvent` (T02)
**Requirement**: DOM-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Record compiles, implements `IDomainEvent`
- [ ] Unit test: constructs with values, asserts properties round-trip
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 1+ test passes

**Tests**: unit
**Gate**: quick

---

### T12: `AssetSuspended` domain event [P]

**What**: `sealed record AssetSuspended(Guid AssetId, string Reason, Guid SuspendedBy, DateTime OccurredAt) : IDomainEvent`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Events/Asset/AssetSuspended.cs`
**Depends on**: T02
**Reuses**: `IDomainEvent` (T02)
**Requirement**: DOM-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Record compiles, implements `IDomainEvent`
- [ ] Unit test: constructs with values, asserts properties round-trip
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 1+ test passes

**Tests**: unit
**Gate**: quick

---

### T13: `AssetEntity` aggregate

**What**: Aggregate root with `Create`, `AttachMedia`, `SubmitForModeration`, `Publish`, `Suspend`, `Reinstate`, `Archive` — full state machine from design.md
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Entities/AssetEntity.cs`
**Depends on**: T03, T04, T05, T06, T07, T08, T09, T10, T11, T12
**Reuses**: `AggregateRoot` (T02), all VOs (T05-T08), all events (T09-T12), `AssetStatus` (T03)
**Requirement**: DOM-01, DOM-02, DOM-06

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `Create(...)` returns entity in `Draft`, raises `AssetCreated`, no public constructor
- [ ] `AttachMedia` raises `AssetMediaUploaded`
- [ ] `SubmitForModeration` only valid from `Draft`; `Publish` only valid from `PendingModeration`, raises `AssetPublished`; throws `InvalidOperationException` otherwise (e.g. `Draft` → `Active` directly)
- [ ] `Suspend` only valid from `Active`, raises `AssetSuspended`; `Reinstate` only valid from `Suspended`
- [ ] `Archive` valid from any non-`Archived` state; throws if already `Archived`
- [ ] Unit tests: happy-path full lifecycle (Draft→PendingModeration→Active→Suspended→Active→Archived), each invalid transition throws, each event has correct payload
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 10+ tests pass

**Tests**: unit
**Gate**: quick

**Commit**: `feat(domain): add AssetEntity aggregate with status lifecycle`

---

### T14: `CategoryEntity` [P]

**What**: `CreateRoot(name)` and `CreateChild(name, parent)` with depth-capped-at-3 enforcement
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Entities/CategoryEntity.cs`
**Depends on**: T02, T04
**Reuses**: `AggregateRoot` (T02), `CategoryRules.MaxDepth` (T04)
**Requirement**: DOM-03

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `CreateRoot` sets `Depth = 1`, `ParentCategoryId = null`
- [ ] `CreateChild(name, parent)` sets `Depth = parent.Depth + 1`; throws `ArgumentException` when `parent.Depth >= 3` (would produce depth 4)
- [ ] Unit tests: depth 1/2/3 succeed, depth 4 throws
- [ ] Gate check passes: `dotnet test 03-tests/00-Domain/RentifyxAssetRegistry.Tests.Domain/RentifyxAssetRegistry.Tests.Domain.csproj`
- [ ] Test count: 4+ tests pass

**Tests**: unit
**Gate**: quick

**Commit**: `feat(domain): add CategoryEntity with depth-capped nesting`

---

### T15: `AssetSearchFilter`

**What**: Filter record for `IAssetRepository.SearchAsync` — category, price range, keyword, page/pageSize
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Filters/Assets/AssetSearchFilter.cs`
**Depends on**: T13
**Reuses**: `Domain/Filters/Examples/ExampleFilter.cs` as the folder/naming pattern
**Requirement**: DOM-05

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Record mirrors `ExampleFilter.cs` shape/conventions, fields: `CategoryId?`, `MinPrice?`, `MaxPrice?`, `Keyword?`, `Page`, `PageSize`
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none (plain data record, no logic)
**Gate**: build

---

### T16: `IAssetRepository` [P]

**What**: Repository contract — `GetByIdAsync`, `GetByOwnerAsync`, `SaveAsync`, `SoftDeleteAsync`, `SearchAsync`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Interfaces/Asset/IAssetRepository.cs`
**Depends on**: T13, T15
**Reuses**: `PagedResult<T>` (existing `Common/PagedResult.cs`), `AssetSearchFilter` (T15)
**Requirement**: DOM-05

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Interface in `Domain/Interfaces/Asset/` subfolder (per CLAUDE.md — never loose under `Interfaces/`)
- [ ] All 5 methods present with `Async` suffix and `CancellationToken` parameter
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none (interface only, no implementation until E-04)
**Gate**: build

---

### T17: `ICategoryRepository` [P]

**What**: Repository contract — `GetByIdAsync`, `GetAllAsync`, `SaveAsync`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Interfaces/Category/ICategoryRepository.cs`
**Depends on**: T14
**Reuses**: Same interface-folder convention
**Requirement**: DOM-05

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Interface in `Domain/Interfaces/Category/`
- [ ] All 3 methods present with `Async` suffix and `CancellationToken` parameter
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none
**Gate**: build

---

### T18: `IMediaStorageService` [P]

**What**: Service contract — `GeneratePresignedUploadUrlAsync`, `ValidateUploadAsync`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Interfaces/Media/IMediaStorageService.cs`
**Depends on**: T08
**Reuses**: Same interface-folder convention, `Media` VO (T08)
**Requirement**: DOM-05

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Interface in `Domain/Interfaces/Media/`
- [ ] Both methods present with `Async` suffix and `CancellationToken` parameter
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none
**Gate**: build

---

### T19: `IOwnerStatusValidator` [P]

**What**: Service contract — `IsOwnerActiveAsync(Guid ownerId, CancellationToken ct)`
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/Interfaces/Asset/IOwnerStatusValidator.cs`
**Depends on**: None
**Reuses**: Same interface-folder convention (`Asset/` — validates asset ownership, not a standalone concept)
**Requirement**: DOM-05

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Interface in `Domain/Interfaces/Asset/`
- [ ] Method present with `Async` suffix and `CancellationToken` parameter
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release`

**Tests**: none

**Note**: fail-open/fail-closed behavior NOT implemented here — contract shape only, per STATE.md open question (needs user confirmation before E-04 implements it)

**Gate**: build

---

### T20: ADR-AR-002 — Asset status lifecycle rationale

**What**: ADR documenting why `PendingModeration` is distinct from `Draft`
**Where**: `docs/decisions/002-asset-status-lifecycle.md`
**Depends on**: T13
**Reuses**: `docs/decisions/000-adr-template.md`
**Requirement**: DOM-06

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Follows `000-adr-template.md` structure
- [ ] Covers: why 5 states, why `PendingModeration` ≠ `Draft`, valid transition graph, terminal states

**Tests**: none
**Gate**: none

**Commit**: `docs(adr): add ADR-AR-002 asset status lifecycle`

---

### T21: ADR-AR-003 — Category as entity vs. enum

**What**: ADR documenting why Category is a first-class entity with nested taxonomy, not an enum
**Where**: `docs/decisions/003-category-entity.md`
**Depends on**: T14
**Reuses**: `docs/decisions/000-adr-template.md`
**Requirement**: DOM-06

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Follows ADR template structure
- [ ] Covers: entity vs. enum tradeoff, depth-3 cap rationale, cycle-prevention-by-construction design

**Tests**: none
**Gate**: none

**Commit**: `docs(adr): add ADR-AR-003 category as entity`

---

### T22: Final review — zero framework deps + full gate

**What**: Verify `RentifyxAssetRegistry.Domain.csproj` has no `Microsoft.EntityFrameworkCore`/`AWSSDK.*`/other framework package references; run full solution build+test
**Where**: `02-src/03-Domain/RentifyxAssetRegistry.Domain/RentifyxAssetRegistry.Domain.csproj` (verify only, no edit expected)
**Depends on**: T16, T17, T18, T19, T20, T21
**Reuses**: N/A
**Requirement**: DOM-01..06 (closes out all)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `grep`/inspect `Domain.csproj` — zero non-BCL package references
- [ ] Gate check passes: `dotnet build RentifyxAssetRegistry.slnx --configuration Release && dotnet test RentifyxAssetRegistry.slnx`
- [ ] All `Tests.Domain` tests pass (expected total ≈ 40+ across T02, T05-T14), `Example*` tests still skip as before (unrelated, untouched)

**Tests**: none (verification task)
**Gate**: full

---

## Parallel Execution Map

```
Phase 0 (Sequential):
  T01 → T02 → T03 → T04

Phase 1+2 (Parallel, after Phase 0):
    ├── T05 [P]
    ├── T06 [P]
    ├── T07 [P]
    ├── T08 [P]
    ├── T09 [P]
    ├── T10 [P]
    ├── T11 [P]
    └── T12 [P]

Phase 3 (Parallel, after Phase 1+2):
    ├── T13 (AssetEntity)
    └── T14 [P] (CategoryEntity)

Phase 4 (Parallel, after Phase 3):
    ├── T15 → T16 [P]
    ├── T17 [P]
    ├── T18 [P]
    └── T19 [P]

Phase 5 (Sequential-ish, after Phase 4):
    T20, T21 → T22
```

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T01 | 1 project scaffold | ✅ Granular |
| T02 | 2 tiny cohesive types (interface + base class, same concern) | ✅ Granular (cohesive) |
| T03 | 2 enums, zero logic | ✅ Granular (cohesive) |
| T04 | 1 file modified (constants) | ✅ Granular |
| T05-T12 | 1 VO/event each | ✅ Granular |
| T13 | 1 aggregate (7 methods, one state machine) | ✅ Granular (cohesive, single concept) |
| T14 | 1 entity | ✅ Granular |
| T15 | 1 filter record | ✅ Granular |
| T16-T19 | 1 interface each | ✅ Granular |
| T20-T21 | 1 ADR each | ✅ Granular |
| T22 | 1 verification pass | ✅ Granular |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
|---|---|---|---|
| T01 | None | None | ✅ Match |
| T02 | T01 | T01→T02 | ✅ Match |
| T03 | T01 | T01→T02→T03 | ✅ Match (chained) |
| T04 | T01 | T01→T02→T03→T04 | ✅ Match (chained) |
| T05 | T02, T04 | Phase0→T05 | ✅ Match |
| T06 | T02, T04 | Phase0→T06 | ✅ Match |
| T07 | T02 | Phase0→T07 | ✅ Match |
| T08 | T02, T03, T04 | Phase0→T08 | ✅ Match |
| T09-T12 | T02 | Phase0→T09-T12 | ✅ Match |
| T13 | T03,T04,T05,T06,T07,T08,T09,T10,T11,T12 | Phase1+2→T13 | ✅ Match |
| T14 | T02, T04 | Phase1+2→T14 (shown as Phase3, after Phase0/1/2 which include T02/T04) | ✅ Match |
| T15 | T13 | T13→T15 | ✅ Match |
| T16 | T13, T15 | T15→T16, T13 already satisfied upstream | ✅ Match |
| T17 | T14 | T14→T17 | ✅ Match |
| T18 | T08 | T08→T18 (via Phase4 fan-out) | ✅ Match |
| T19 | None | Phase4→T19 | ✅ Match |
| T20 | T13 | T13→T20 | ✅ Match |
| T21 | T14 | T14→T21 | ✅ Match |
| T22 | T16,T17,T18,T19,T20,T21 | Phase5→T22 | ✅ Match |

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T01 | test project scaffold | none | none | ✅ OK |
| T02 | Domain common (AggregateRoot) | unit | unit | ✅ OK |
| T03 | Domain enums | none (no logic) | none | ✅ OK |
| T04 | Domain constants | none (exercised by later tests) | none | ✅ OK |
| T05-T08 | Domain VOs | unit | unit | ✅ OK |
| T09-T12 | Domain events | unit | unit | ✅ OK |
| T13 | Domain entity (AssetEntity) | unit | unit | ✅ OK |
| T14 | Domain entity (CategoryEntity) | unit | unit | ✅ OK |
| T15 | Domain filter (data-only record) | none | none | ✅ OK |
| T16-T19 | Domain interfaces (no impl) | none | none | ✅ OK |
| T20-T21 | ADR docs | none | none | ✅ OK |
| T22 | verification only | none | none | ✅ OK |

No violations — all code layers with logic (VOs, events, entities, AggregateRoot) have unit tests co-located in the same task; pure-declaration layers (enums, interfaces, filters, ADRs) correctly have `none`.

---

## Tools Confirmation Needed

Per skill process, before execution starts: no project MCPs configured beyond the standard set. Planned tools per task are `NONE` (plain filesystem edits + `dotnet` CLI) across all 22 tasks — no Context7/web search needed since this is pure C#/xUnit work matching an already-confirmed sibling-repo pattern. Flag if you want a different tool assignment before I start Phase 0.
