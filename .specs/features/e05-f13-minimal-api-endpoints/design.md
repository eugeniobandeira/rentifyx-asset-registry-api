# Design: F-13 Minimal API Endpoints

PROJECT CONTEXT
- Language / framework: .NET 10, Minimal APIs, C# latest
- Architectural pattern: Clean Architecture — endpoints are the thinnest layer, calling
  `IHandler<TRequest,TResponse>` only, never a repository directly
- Endpoint discovery: reflection-based (`EndpointExtensions.AddEndpoints`/`MapEndpoints`) — every
  `IEndpoint` implementation is auto-registered and auto-mapped onto the versioned+rate-limited
  `/api/v1` route group; no manual wiring needed per endpoint class
- Error pattern: `ErrorOr<T>` → `result.ToResult(httpContext)` / `ToCreatedResult(...)` →
  `ErrorOrExtensions.ToProblem` maps `ErrorType` to HTTP status
- Test framework: xUnit + FluentAssertions; `03-tests/05-Integration` has a scaffolded
  `CustomWebApplicationFactory` with zero tests referencing it yet — F-13 is the first real user

## Architecture Overview

No new architectural pattern — this feature is pure "connect the wires" work following the exact
`IEndpoint` shape `HealthCheck.cs` already demonstrates, replicated 10 times across two new
`Endpoints/` subfolders (`Assets/`, `Categories/`), plus one new Application-layer handler
(`GetAssetByIdHandler`) and one bug fix (`ErrorOrExtensions.ToProblem`'s missing `Forbidden` case).

```
HTTP request
  → IEndpoint.MapEndpoint (route + tags + AllowAnonymous, registered on the v1 group)
  → static HandleAsync(request-shape, IHandler<TReq,TResp>, HttpContext, CancellationToken)
      - route {id} + body fields combined into the existing TRequest record
  → handler.HandleAsync(request, ct)  [unchanged Application-layer code]
  → ErrorOr<TResponse>.ToResult(httpContext) / ToCreatedResult(uri, httpContext)
      → success: Results.Ok / Results.Created
      → failure: ErrorOrExtensions.ToProblem (ErrorType → status code)
```

## Components

### 1. Bug fix: `ErrorOrExtensions.ToProblem` — add `Forbidden` case

`02-src/01-Api/RentifyxAssetRegistry.Api/Extensions/ErrorOrExtensions.cs`: add
`ErrorType.Forbidden => StatusCodes.Status403Forbidden` to the existing switch, alongside
`Validation`/`NotFound`/`Conflict`/`Unauthorized`. One-line change, no other logic touched.

### 2. New Application handler: `GetAssetByIdHandler`

`02-src/02-Application/.../Features/Assets/Handlers/GetById/` (new folder, mirrors `Create/`'s
shape exactly minus the validator):
- `Request/GetAssetByIdRequest.cs`: `public sealed record GetAssetByIdRequest(Guid AssetId);`
- No `Validator/` subfolder — route-bound `Guid` can't be malformed by the time the handler runs
  (see spec.md's Data Model Changes for the reasoning).
- `GetAssetByIdHandler.cs`: `IHandler<GetAssetByIdRequest, GetAssetByIdResponse>`, constructor takes
  `IAssetRepository repository, ILogger<GetAssetByIdHandler> logger` (no validator dependency,
  unlike every other handler in this repo — the DI registration in step 3 reflects this). Body:
  `repository.GetByIdAsync(request.AssetId, ct)` → null → `Error.NotFound(AssetErrorCodes.NotFound, ...)`
  → found → `AssetMapper.ToGetByIdResponse(asset)`.
- `AssetModerationResponse.cs`-sibling new file `GetAssetByIdResponse.cs` at the `Assets/` feature
  level: `public sealed record GetAssetByIdResponse(Guid Id, string Title, string Description, decimal Price, Guid CategoryId, Guid OwnerId, AssetStatus Status, DateTime CreatedAt);`
- `AssetMapper.cs`: add `public static GetAssetByIdResponse ToGetByIdResponse(AssetEntity entity) => new(entity.Id, entity.Title.Value, entity.Description.Value, entity.Price.Amount, entity.CategoryId, entity.OwnerId, entity.Status, entity.CreatedAt);`

### 3. IoC registration

`02-src/04-IoC/RentifyxAssetRegistry.IoC/ApplicationDependencyInjection.cs`: add
`services.AddScoped<IHandler<GetAssetByIdRequest, GetAssetByIdResponse>, GetAssetByIdHandler>();`
— **no** matching `IValidator<...>` line, since there is no validator (deviates from every other
handler pair in this file — the omission is the point, not an oversight).

### 4. `Tags.cs`

Add `public const string ASSETS = "Assets";` and `public const string CATEGORIES = "Categories";`
alongside the existing `HEALTH`.

### 5. Asset endpoints (`Endpoints/Assets/`, 7 new `IEndpoint` classes)

Each file follows `HealthCheck.cs`'s exact shape: `internal sealed class {Name} : IEndpoint`, one
`MapEndpoint` method, one `private static async Task<IResult> HandleAsync(...)`. `.AllowAnonymous()`
on every route (this feature's confirmed scope — see spec.md Summary).

| File | Route | Request construction |
|---|---|---|
| `CreateAsset.cs` | `POST /assets` | Body binds directly to `CreateAssetRequest` |
| `GetAssetById.cs` | `GET /assets/{id}` | `new GetAssetByIdRequest(id)` |
| `SearchAssets.cs` | `GET /assets` | `[AsParameters]` struct or explicit query params → `new SearchAssetsRequest(...)` |
| `RequestMediaUpload.cs` | `POST /assets/{id}/media/upload-request` | Body DTO (without `AssetId` field) + route `id` → `new RequestMediaUploadRequest(id, body.OwnerId, body.MimeType, body.SizeBytes)` |
| `ConfirmMediaUpload.cs` | `POST /assets/{id}/media/confirm` | Same pattern, body DTO without `AssetId` |
| `SubmitForModeration.cs` | `POST /assets/{id}/submit-for-moderation` | Body DTO (`OwnerId` only) + route `id` |
| `AdminReviewAsset.cs` | `POST /assets/{id}/admin-review` | Body DTO (`Approve`, `IsAdmin`, `Reason`) + route `id` |

Since `RequestMediaUploadRequest`/`ConfirmMediaUploadRequest`/`SubmitForModerationRequest`/
`AdminReviewAssetRequest` all already have `AssetId` as their first positional field, and the spec
requires the route `{id}` to be the sole source of truth (no duplicate body field), each of these
4 endpoint files declares a small **local body-only record** (e.g.
`private sealed record RequestMediaUploadBody(Guid OwnerId, string MimeType, long SizeBytes);`)
scoped to that endpoint file, used only to bind the JSON body minus `AssetId`, then combined with
route `id` into the real `TRequest` before calling the handler. This is the only new "shape" this
feature introduces — everything else reuses existing Request/Response types verbatim.

`CreateAsset.cs` and `SearchAssets.cs`/`GetAssetById.cs` need no such wrapper — `CreateAssetRequest`
has no route param at all (pure body), and `GetAssetByIdRequest`/`SearchAssetsRequest` either take
only the route id or only query params.

`CreateAsset.cs` uses `ToCreatedResult(result, $"/api/v1/assets/{result.Value?.AssetId}", httpContext)`
guarded by `result.IsError` (or `ErrorOr`'s built-in `Match` with a success lambda building the URI
from the typed value, matching `ErrorOrExtensions.ToCreatedResult`'s existing signature — that
extension takes a plain `string uri` computed from the `ErrorOr<T>`'s value in the success branch,
so the endpoint's `HandleAsync` must call `.Match` directly rather than the extension if the URI
depends on the response body; confirm the exact call shape against `ToCreatedResult`'s real generic
constraint at Execute time, this is a judgment call not fully pinned by the existing extension's
current signature which takes a pre-computed `uri` string, implying the endpoint must know the new
id before calling it — trivial since `CreateAssetResponse.AssetId` is known only after `HandleAsync`
returns, so the actual call is `result.Match(r => Results.Created($"/api/v1/assets/{r.AssetId}", r), e => e.ToProblem(httpContext))`, bypassing `ToCreatedResult` for this one endpoint).

### 6. Category endpoints (`Endpoints/Categories/`, 3 new `IEndpoint` classes)

| File | Route | Request construction |
|---|---|---|
| `CreateCategory.cs` | `POST /categories` | Body binds directly to `CreateCategoryRequest` |
| `UpdateCategory.cs` | `PATCH /categories/{id}` | Body DTO (`IsAdmin`, `NewName`, `NewParentCategoryId`) + route `id` → `UpdateCategoryRequest` |
| `ListCategories.cs` | `GET /categories` | No input — `new ListCategoriesRequest()` |

`CreateCategory.cs` mirrors `CreateAsset.cs`'s `Results.Created` pattern
(`/api/v1/categories/{r.Id}`).

## Testing Strategy

`03-tests/05-Integration/RentifyxAssetRegistry.Tests.Integration/` — first real tests against
`CustomWebApplicationFactory`. Two new test files, `AssetEndpointsTests.cs` and
`CategoryEndpointsTests.cs`, one `[Fact]` per happy path (10 total) plus the specific error-case
tests called out in spec.md's Testing Strategy (403 on suspended owner, 404 on missing asset, 422 on
invalid body). `GetAssetByIdHandler` also gets a standard Moq unit test in `03-tests/03-Handlers/`.

## Risks & Unknowns

- `[non-blocking]` The exact Minimal API query-parameter binding style for `SearchAssets` (6
  optional/nullable fields) — whether `[AsParameters]` with a small parameter-binding record type or
  individual method parameters reads cleaner — is an Execute-time call, not dictated here; either
  compiles to the same route.
- `[non-blocking]` `CreateAsset`'s exact `Results.Created` call shape (see Component 5's note) needs
  confirming against `ErrorOr`'s actual `Match` overload signature at Execute time — the intent
  (id-bearing Location header) is fixed, the exact one-liner isn't.
- Everything else already flagged in spec.md's Open Questions carries forward unchanged.
