# Tasks: F-13 Minimal API Endpoints

| # | Layer | What to create / change | Reference file | Depends on |
|---|---|---|---|---|
| 1 | Api | Fix `ErrorOrExtensions.ToProblem` — add `ErrorType.Forbidden` → 403 | existing file | — |
| 2 | Application | New `GetAssetByIdHandler` + `Request`/`Response` + `AssetMapper.ToGetByIdResponse` | `CreateAssetHandler.cs`/`AssetMapper.cs` | — |
| 3 | IoC | Register `GetAssetByIdHandler` (no validator line) | `ApplicationDependencyInjection.cs` | 2 |
| 4 | Api | `Tags.cs`: add `ASSETS`/`CATEGORIES` | existing file | — |
| 5 | Api | `CreateAsset.cs` endpoint | `HealthCheck.cs` | 1, 4 |
| 6 | Api | `GetAssetById.cs` endpoint | `HealthCheck.cs` | 2, 3, 4 |
| 7 | Api | `SearchAssets.cs` endpoint | `HealthCheck.cs` | 4 |
| 8 | Api | `RequestMediaUpload.cs` endpoint | `HealthCheck.cs` | 4 |
| 9 | Api | `ConfirmMediaUpload.cs` endpoint | `HealthCheck.cs` | 4 |
| 10 | Api | `SubmitForModeration.cs` endpoint | `HealthCheck.cs` | 4 |
| 11 | Api | `AdminReviewAsset.cs` endpoint | `HealthCheck.cs` | 1, 4 |
| 12 | Api | `CreateCategory.cs` endpoint | `HealthCheck.cs` | 4 |
| 13 | Api | `UpdateCategory.cs` endpoint | `HealthCheck.cs` | 4 |
| 14 | Api | `ListCategories.cs` endpoint | `HealthCheck.cs` | 4 |
| 15 | Test | `GetAssetByIdHandler` unit test | `03-Handlers/.../CreateAssetHandlerTests.cs` | 2 |
| 16 | Test | `AssetEndpointsTests.cs` (7 happy paths + 403/404/422 cases) | `CustomWebApplicationFactory.cs` | 5-11 |
| 17 | Test | `CategoryEndpointsTests.cs` (3 happy paths) | `CustomWebApplicationFactory.cs` | 12-14 |
| 18 | Docs | STATE.md/ROADMAP.md update, close M5's "Minimal API Endpoints" row | — | 1-17 |

---

---
status: pending
title: Fix ErrorOrExtensions.ToProblem missing Forbidden case
type: backend
complexity: low
dependencies: []
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Extensions/ErrorOrExtensions.cs`
**Reference:** existing file, same switch expression
**What:** Add `ErrorType.Forbidden => StatusCodes.Status403Forbidden,` to the `statusCode` switch.
**Done when:** `dotnet build` succeeds; covered by task 16's suspended-owner 403 test.
**Commit:** `fix(api): map ErrorType.Forbidden to 403 in ToProblem`

---
status: pending
title: Implement GetAssetByIdHandler
type: backend
complexity: medium
dependencies: []
---

**Layer:** Application
**File:** `02-src/02-Application/RentifyxAssetRegistry.Application/Features/Assets/Handlers/GetById/GetAssetByIdHandler.cs` (new), `Request/GetAssetByIdRequest.cs` (new), `GetAssetByIdResponse.cs` (new, at `Features/Assets/` level), `AssetMapper.cs` (extend)
**Reference:** `Handlers/Create/CreateAssetHandler.cs` (shape, minus validator dependency)
**What:** `GetAssetByIdRequest(Guid AssetId)`, no validator. Handler: `repository.GetByIdAsync` → null → `Error.NotFound(AssetErrorCodes.NotFound, ...)` → found → `AssetMapper.ToGetByIdResponse(asset)`. New response record with the fuller asset shape (Title, Description, Price, CategoryId, OwnerId, Status, CreatedAt).
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(application): add GetAssetByIdHandler`

---
status: pending
title: Register GetAssetByIdHandler in DI
type: backend
complexity: low
dependencies: [2]
---

**Layer:** IoC
**File:** `02-src/04-IoC/RentifyxAssetRegistry.IoC/ApplicationDependencyInjection.cs`
**Reference:** existing handler registration lines
**What:** `services.AddScoped<IHandler<GetAssetByIdRequest, GetAssetByIdResponse>, GetAssetByIdHandler>();` — no validator registration.
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(ioc): register GetAssetByIdHandler`

---
status: pending
title: Add ASSETS/CATEGORIES route tags
type: backend
complexity: low
dependencies: []
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Tags.cs`
**Reference:** existing file
**What:** Add `ASSETS`/`CATEGORIES` constants alongside `HEALTH`.
**Done when:** `dotnet build` succeeds.
**Commit:** `feat(api): add Assets/Categories route tags`

---
status: pending
title: Implement CreateAsset endpoint
type: backend
complexity: medium
dependencies: [1, 4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Assets/CreateAsset.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `POST /assets`, body binds `CreateAssetRequest` directly, `AllowAnonymous`. `result.Match(r => Results.Created($"/api/v1/assets/{r.AssetId}", r), e => e.ToProblem(httpContext))`.
**Done when:** `dotnet build` succeeds; covered by task 16.
**Commit:** `feat(api): add POST /assets endpoint`

---
status: pending
title: Implement GetAssetById endpoint
type: backend
complexity: low
dependencies: [2, 3, 4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Assets/GetAssetById.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `GET /assets/{id}`, `new GetAssetByIdRequest(id)`, `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 16 (404 case).
**Commit:** `feat(api): add GET /assets/{id} endpoint`

---
status: pending
title: Implement SearchAssets endpoint
type: backend
complexity: medium
dependencies: [4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Assets/SearchAssets.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`; `SearchAssetsRequest.cs` for the 6 query fields
**What:** `GET /assets`, binds `PageSize`/`NextPageToken`/`CategoryId`/`MinPrice`/`MaxPrice`/`Keyword` from query string into `SearchAssetsRequest`, `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 16.
**Commit:** `feat(api): add GET /assets search endpoint`

---
status: pending
title: Implement RequestMediaUpload endpoint
type: backend
complexity: medium
dependencies: [4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Assets/RequestMediaUpload.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `POST /assets/{id}/media/upload-request`. Local body record `(Guid OwnerId, string MimeType, long SizeBytes)`, combined with route `id` into `RequestMediaUploadRequest`. `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 16.
**Commit:** `feat(api): add media upload-request endpoint`

---
status: pending
title: Implement ConfirmMediaUpload endpoint
type: backend
complexity: medium
dependencies: [4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Assets/ConfirmMediaUpload.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `POST /assets/{id}/media/confirm`. Local body record `(Guid OwnerId, string S3Key, string MimeType, long SizeBytes)`, combined with route `id`. `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 16.
**Commit:** `feat(api): add media confirm endpoint`

---
status: pending
title: Implement SubmitForModeration endpoint
type: backend
complexity: low
dependencies: [4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Assets/SubmitForModeration.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `POST /assets/{id}/submit-for-moderation`. Local body record `(Guid OwnerId)`, combined with route `id`. `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 16.
**Commit:** `feat(api): add submit-for-moderation endpoint`

---
status: pending
title: Implement AdminReviewAsset endpoint
type: backend
complexity: low
dependencies: [1, 4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Assets/AdminReviewAsset.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `POST /assets/{id}/admin-review`. Local body record `(bool Approve, bool IsAdmin, string? Reason)`, combined with route `id`. `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 16.
**Commit:** `feat(api): add admin-review endpoint`

---
status: pending
title: Implement CreateCategory endpoint
type: backend
complexity: low
dependencies: [4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Categories/CreateCategory.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`, task 5's `Results.Created` pattern
**What:** `POST /categories`, body binds `CreateCategoryRequest` directly, `AllowAnonymous`, `Results.Created($"/api/v1/categories/{r.Id}", r)` on success.
**Done when:** `dotnet build` succeeds; covered by task 17.
**Commit:** `feat(api): add POST /categories endpoint`

---
status: pending
title: Implement UpdateCategory endpoint
type: backend
complexity: low
dependencies: [4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Categories/UpdateCategory.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `PATCH /categories/{id}`. Local body record `(bool IsAdmin, string? NewName, Guid? NewParentCategoryId)`, combined with route `id`. `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 17.
**Commit:** `feat(api): add PATCH /categories/{id} endpoint`

---
status: pending
title: Implement ListCategories endpoint
type: backend
complexity: low
dependencies: [4]
---

**Layer:** Api
**File:** `02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/Categories/ListCategories.cs` (new)
**Reference:** `Endpoints/Health/HealthCheck.cs`
**What:** `GET /categories`, no input, `new ListCategoriesRequest()`, `AllowAnonymous`, `ToResult`.
**Done when:** `dotnet build` succeeds; covered by task 17.
**Commit:** `feat(api): add GET /categories endpoint`

---
status: pending
title: Unit test GetAssetByIdHandler
type: test
complexity: low
dependencies: [2]
---

**Layer:** Test
**File:** `03-tests/03-Handlers/RentifyxAssetRegistry.Tests.Handlers/Features/Assets/GetAssetByIdHandlerTests.cs` (new)
**Reference:** any existing `03-Handlers/.../Assets/*Tests.cs` (Moq pattern)
**What:** Not-found → `Error.NotFound`; found → correctly mapped `GetAssetByIdResponse`.
**Done when:** `dotnet test` passes for this file.
**Commit:** `test(application): add GetAssetByIdHandler unit tests`

---
status: pending
title: Integration tests for Asset endpoints
type: test
complexity: high
dependencies: [5, 6, 7, 8, 9, 10, 11]
---

**Layer:** Test
**File:** `03-tests/05-Integration/RentifyxAssetRegistry.Tests.Integration/AssetEndpointsTests.cs` (new)
**Reference:** `CustomWebApplicationFactory.cs`
**What:** One happy-path `[Fact]` per Asset endpoint (7). Plus: `POST /assets` with a suspended owner → 403; `GET /assets/{id}` for a missing id → 404; `POST /assets` with an invalid body (blank title) → 422 `ValidationProblem` shape.
**Done when:** `dotnet test` passes for this file.
**Commit:** `test(api): add Asset endpoint integration tests`

---
status: pending
title: Integration tests for Category endpoints
type: test
complexity: medium
dependencies: [12, 13, 14]
---

**Layer:** Test
**File:** `03-tests/05-Integration/RentifyxAssetRegistry.Tests.Integration/CategoryEndpointsTests.cs` (new)
**Reference:** `CustomWebApplicationFactory.cs`
**What:** One happy-path `[Fact]` per Category endpoint (3).
**Done when:** `dotnet test` passes for this file.
**Commit:** `test(api): add Category endpoint integration tests`

---
status: pending
title: STATE.md/ROADMAP.md update, close F-13
type: docs
complexity: low
dependencies: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17]
---

**Layer:** Docs
**File:** `.specs/project/STATE.md`, `.specs/project/ROADMAP.md`
**Reference:** prior feature-completion entries
**What:** Feature Completion Log entry, mark M5's "Minimal API Endpoints" DONE, note Security Hardening/API Docs still PLANNED.
**Done when:** Both docs reflect F-13 complete; full `dotnet test` suite result cited.
**Commit:** `docs: close F-13, update STATE.md/ROADMAP.md`
