# Spec: F-13 Minimal API Endpoints

## Summary

Wires real HTTP endpoints onto the Application-layer handlers built across E-03/E-04 (Assets and
Categories), which today are only exercised by tests. Covers Assets (Create, GetById, Search,
RequestMediaUpload, ConfirmMediaUpload, SubmitForModeration), Categories (Create, Update, List), and
AdminReview. `ApplyModerationVerdict` is explicitly excluded — it's already wired to the F-12 Kafka
consumer, not an HTTP-triggered action. This is the first Minimal API feature slice under
`Endpoints/`; only `Health` exists today. All new endpoints route under `/api/v1/` (existing
`MapVersionedApi` convention), are rate-limited (existing `RequireRateLimiting` on the versioned
group), and stay `AllowAnonymous` for now — real JWT-claims-derived authorization is explicitly
out of scope, deferred to the separate "Security Hardening" feature (ROADMAP M5), per user decision
this session. `OwnerId`/`IsAdmin` continue as caller-supplied request-body fields, matching every
handler's existing (pre-F-13) contract — no handler signatures change.

## Domain / Module

Api layer only (`02-src/01-Api/RentifyxAssetRegistry.Api/Endpoints/`). One new Application-layer
handler is required first (see Data Model Changes) since it doesn't exist yet; everything else in
Application is unchanged.

## Interface Contract

| # | Method | Route | Handler | Success status |
|---|---|---|---|---|
| 1 | POST | `/assets` | `CreateAssetHandler` | 201 Created |
| 2 | GET | `/assets/{id}` | `GetAssetByIdHandler` (**new**) | 200 OK |
| 3 | GET | `/assets` | `SearchAssetsHandler` (query params) | 200 OK |
| 4 | POST | `/assets/{id}/media/upload-request` | `RequestMediaUploadHandler` | 200 OK |
| 5 | POST | `/assets/{id}/media/confirm` | `ConfirmMediaUploadHandler` | 200 OK |
| 6 | POST | `/assets/{id}/submit-for-moderation` | `SubmitForModerationHandler` | 200 OK |
| 7 | POST | `/assets/{id}/admin-review` | `AdminReviewAssetHandler` | 200 OK |
| 8 | POST | `/categories` | `CreateCategoryHandler` | 201 Created |
| 9 | PATCH | `/categories/{id}` | `UpdateCategoryHandler` | 200 OK |
| 10 | GET | `/categories` | `ListCategoriesHandler` | 200 OK |

All under `/api/v1` (prefix applied automatically by `MapVersionedApi`). Auth: none required
(`AllowAnonymous`) for this feature — see Summary. `{id}` route params bind to each request's
`AssetId`/`CategoryId` field; everything else in each request comes from the JSON body (or query
string for #3), per row-by-row notes below.

**Row 1 (`POST /assets`)**: full `CreateAssetRequest` in body — includes `OwnerId`. Route: none
beyond `/assets`.

**Row 2 (`GET /assets/{id}`)**: `id` from route. **Requires a new `GetAssetByIdHandler`** (see Data
Model Changes) — no existing handler wraps `IAssetRepository.GetByIdAsync`.

**Row 3 (`GET /assets`)**: `SearchAssetsRequest`'s 6 fields (`PageSize`, `NextPageToken`,
`CategoryId`, `MinPrice`, `MaxPrice`, `Keyword`) all bind from query string — Minimal API's
`[AsParameters]`-style binding or explicit query param arguments (implementation detail, not a
contract change).

**Row 4/5 (media upload)**: `id` from route maps to `AssetId`; `OwnerId` + the rest of each
request's fields come from the body. Note both requests already contain `AssetId` as a field — the
endpoint constructs the request record from route `id` + body, not by trusting a body-supplied
`AssetId` that could mismatch the route (reject if they don't match, or simply don't accept
`AssetId` in the body schema at all — see Validation Rules).

**Row 6 (`SubmitForModeration`)**: `id` from route → `AssetId`; `OwnerId` from body.

**Row 7 (`AdminReview`)**: `id` from route → `AssetId`; `Approve`, `IsAdmin`, `Reason` from body.

**Row 8 (`CreateCategory`)**: full `CreateCategoryRequest` in body (`Name`, `ParentCategoryId`,
`IsAdmin`).

**Row 9 (`UpdateCategory`)**: `id` from route → `CategoryId`; `IsAdmin`, `NewName`,
`NewParentCategoryId` from body.

**Row 10 (`ListCategories`)**: no input — `ListCategoriesRequest` is a parameterless marker record.

## Request / Input

Every request DTO already exists and is unchanged by this feature (see spec's Domain/Module note),
**except** `GetAssetByIdRequest` (new — see Data Model Changes). No new fields are added to any
existing Request record; endpoints only decide which fields come from route vs. body vs. query.

## Response / Output

Every Response DTO already exists and is unchanged, except `GetAssetByIdResponse` (new). All
successful responses return the handler's existing `TResponse` type as-is via
`ToResult`/`ToCreatedResult` (see Handler/Service Logic) — no envelope wrapping.

## Data Model Changes

**New Application-layer handler: `GetAssetByIdHandler`** (net-new, not previously flagged in
ROADMAP/STATE as a gap — found during this feature's research pass, not assumed away). Follows the
exact same pattern as every other Assets handler (`Application/Features/Assets/Handlers/GetById/`):
- `Request/GetAssetByIdRequest.cs`: `record GetAssetByIdRequest(Guid AssetId)`
- No separate validator needed — `AssetId` being a route-bound `Guid` can't be empty/malformed the
  way a body string field could (ASP.NET's route-model binding already rejects a non-Guid segment
  with a 400 before the handler is ever called); a `FluentValidation` validator would only duplicate
  that. Deviates from every other handler in this repo having a validator — flagged here rather than
  silently added out of habit, since CLAUDE.md doesn't mandate a validator for every request, only
  where there's a real rule to enforce.
- `{Feature}Response.cs` addition: `GetAssetByIdResponse.cs`, a new
  `record GetAssetByIdResponse(Guid Id, string Title, string Description, decimal Price, Guid CategoryId, Guid OwnerId, AssetStatus Status, DateTime CreatedAt)`
  — the fullest asset shape returned anywhere so far (existing `AssetSummaryResponse` used by search
  is deliberately thinner). New `AssetMapper.ToGetByIdResponse(AssetEntity)` static method alongside
  the mapper's existing `ToNewAsset`/`ToCreateAssetResponse`/`ToModerationResponse`.
- Handler body: `repository.GetByIdAsync(request.AssetId, ct)` → `null` → `Error.NotFound` (reuse
  existing `AssetErrorCodes.NotFound`) → found → `AssetMapper.ToGetByIdResponse(asset)`. No
  ownership/authorization check — matches this feature's `AllowAnonymous` scope; whether GetById
  should be owner-or-admin-gated is a Security Hardening concern, not this one (an asset's own
  `Status` may leak pre-moderation details to an anonymous caller today — flagged as an Open
  Question, not silently decided either way).

**Bug fix required, found during research, not part of any existing Known Gap**:
`ErrorOrExtensions.ToProblem`'s `ErrorType` switch has no `ErrorType.Forbidden` case, falling through
to the `_ => 500` default. `CreateAssetHandler` already returns `Error.Forbidden` for a
suspended/deleted owner (F-05, unchanged by F-12) — until this endpoint exists, that code path was
untestable via HTTP and the gap went unnoticed. Must add
`ErrorType.Forbidden => StatusCodes.Status403Forbidden` before `POST /assets` can correctly surface
that case. `AdminReviewAssetHandler`/category handlers don't currently return `Forbidden` (they
return `Error.Validation`/`AssetErrorCodes.NotAdmin` per STATE.md's caller-supplied-`IsAdmin` note),
so this fix is scoped to unblocking `CreateAsset`'s existing behavior, not inventing new 403 cases
elsewhere.

## Validation Rules

| Field | Rule | Source / Constant |
|---|---|---|
| All existing request fields | Unchanged — each handler's own `FluentValidation` validator (already built, already tested) runs exactly as before | existing `Validator/` classes per feature |
| Route `{id}` vs. body's own `AssetId`/`CategoryId` (rows 4-7, 9) | Endpoint constructs the request from route `id`, does not accept a duplicate `AssetId`/`CategoryId` field in the body schema at all — eliminates the mismatch case entirely rather than validating it | new, endpoint-level (request DTO reshaping, not a validator) |
| `GetAssetByIdRequest.AssetId` | Route-bound `Guid`, ASP.NET's default model binding rejects malformed segments with 400 before reaching the handler | framework default, no new validator |

## Handler / Service Logic

Every endpoint follows the exact `IEndpoint` pattern from CLAUDE.md's "Endpoint pattern" section
verbatim:
1. `internal sealed class {Action} : IEndpoint`, `MapEndpoint` registers one route with
   `.WithName()`/`.WithDescription()`/`.WithTags(Tags.{Group})`.
2. Static `HandleAsync` resolves `IHandler<TRequest, TResponse>` via DI parameter injection, calls
   `HandleAsync(request, cancellationToken)`.
3. `result.Match(r => Results.Ok(r), e => e.ToProblem(httpContext))` for 200 responses; `POST
   /assets` and `POST /categories` use `ToCreatedResult` instead (201, `Location` header pointing at
   the new resource's GetById-shaped URI — `/api/v1/assets/{id}` and `/api/v1/categories/{id}`
   respectively, even though category GetById isn't itself a route in this feature — `Results.Created`
   only needs a URI string, not a resolvable route).
4. Route-bound `{id}` + body fields are combined into the existing request record by the endpoint's
   `HandleAsync` before calling the handler — no handler changes.

## Error Cases

| Scenario | Error type | HTTP status |
|---|---|---|
| Validation failure (any endpoint) | `ErrorType.Validation` | 422 (existing) |
| Asset/Category not found | `ErrorType.NotFound` | 404 (existing) |
| Owner suspended/deleted (`CreateAsset`) | `ErrorType.Forbidden` | **403 (fix required, see Data Model Changes)** |
| Category cycle/depth violation, idempotent-replay edge cases | `ErrorType.Validation` or `ErrorType.Conflict` per existing handler behavior | 422/409 (existing, unchanged) |
| Malformed route Guid | n/a (framework) | 400 |
| Unexpected failure | default | 500 (existing) |

## Testing Strategy

**Integration tests** (`03-tests/05-Integration/`, `CustomWebApplicationFactory` +
`Microsoft.AspNetCore.Mvc.Testing` — this is the first real use of that test project; confirm its
current state isn't just scaffold before assuming a pattern exists to extend):
- One happy-path test per endpoint (10 total) — status code + body shape.
- `POST /assets` with a suspended owner → 403 (covers the `ErrorType.Forbidden` fix directly).
- `GET /assets/{id}` for a non-existent id → 404.
- `POST /assets` with an invalid body (e.g. blank title) → 422 with `ValidationProblem` shape.
- Route `{id}` used consistently across upload-request/confirm/submit-for-moderation/admin-review
  (no body-supplied `AssetId` accepted — verify the body schema genuinely omits it, e.g. extra
  unknown JSON properties are ignored/rejected per whatever System.Text.Json default this repo uses).

**Unit tests**: `GetAssetByIdHandler` gets the same Moq-based handler test as every other Assets
handler (`03-tests/03-Handlers/`) — not-found and found-and-mapped cases.

**No new validator tests** — `GetAssetByIdHandler` has no validator (see Data Model Changes).

## Open Questions

- `[non-blocking]` `GET /assets/{id}` is unauthenticated and unfiltered by status in this feature —
  an anonymous caller can fetch a `Draft`/`PendingModeration` asset's full details (owner-only fields
  like `OwnerId`, real `Status`) the same as an `Active` one. `SearchAssetsHandler` deliberately
  hardcodes `Status = Active` for exactly this reason (F-08). Whether GetById needs the same
  restriction, or is meant to be owner/admin-only once auth lands, is a Security Hardening decision —
  not solved here, flagged so it isn't silently forgotten once endpoints go live.
- `[non-blocking]` Category `GetById` isn't in ROADMAP's F-13 list (only Create/Update/List) even
  though `Results.Created`'s `Location` header for `POST /categories` points at a URI
  (`/api/v1/categories/{id}`) that won't actually resolve to anything yet. Acceptable per REST
  convention (`Location` doesn't have to be immediately fetchable if the resource isn't independently
  gettable), but worth a Known Gap note once this ships if a future consumer expects to follow it.
- ~~`[non-blocking]` `03-tests/05-Integration`'s current state~~ — **RESOLVED**: confirmed
  `CustomWebApplicationFactory.cs` already exists (scaffold only, zero test files reference it yet).
  F-13 will be the first feature to actually write tests against it.
