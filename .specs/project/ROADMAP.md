# Roadmap

**Current Milestone:** M4 DONE (F-10/F-11/F-12 merged, fully verified). M5 IN PROGRESS — F-13 Minimal API Endpoints, API Docs, and Security Hardening's headers/request-size-limit slice all built (2026-07-24); role-based authZ (Security Hardening's remaining piece) deliberately deferred; G-004 (OWASP suppression file) resolved 2026-07-24.
**Status:** F-13 wires 10 HTTP endpoints (Assets: Create/GetById/Search/RequestMediaUpload/ConfirmMediaUpload/SubmitForModeration/AdminReview; Categories: Create/Update/List) onto the existing Application handlers, all `AllowAnonymous` — JWT-claims-derived auth is explicitly Security Hardening's job, not this feature's. Built a missing `GetAssetByIdHandler` and fixed a real `ErrorType.Forbidden`→500 gap in `ErrorOrExtensions`. Security Hardening's headers/request-size-limit slice adds `SecurityHeadersMiddleware`, `app.UseHsts()`, and `RequestSizeLimitMiddleware`/Kestrel `MaxRequestBodySize`, both gated/configured per env — role-based admin authZ deliberately untouched, see STATE.md for detail. API Docs fixed stale OpenAPI metadata, added request/response examples to all 10 endpoints, and enabled XML doc generation. 164/164 non-container + 14/14 Integration tests green (as of this merge). **CI note:** the OWASP dependency-check gate's suppression file (`dependency-check-suppressions.xml`) has now been authored and wired into `ci.yml` (G-004 resolved, 2026-07-24) — a future PR/push carrying this change should turn the `build-and-test` check green again without an admin override; not yet confirmed via a real CI run.

---

## M1 — Project Foundation & DevSecOps Pipeline

**Goal:** Template gives Day 1–2 for free — focus on CI security gates, S3 bucket bootstrap, secrets.
**Target:** Day 1–3 — E-01

### Features

**Repo & Solution Structure** — IN PROGRESS

- `dotnet new clean-arch` scaffold generated: Api/Application/Domain/IoC/Infrastructure + Aspire AppHost/ServiceDefaults + Tests layers
- `Directory.Packages.props`, `Directory.Build.props` (Nullable, TreatWarningsAsErrors) present from template
- Serilog + CorrelationId middleware + GlobalExceptionHandler, OpenTelemetry, health checks, Scalar UI, ErrorOr present from template
- **Open:** swap generated EF Core/Npgsql `Infrastructure` (AppDbContext, migrations, generic `IAddRepository<T>`/`IUnitOfWork`) for DynamoDB — plan's stack has no relational DB
- **Open:** LocalStack container in AppHost (DynamoDB, S3, KMS) + init scripts

**CI/CD Pipeline & DevSecOps Baseline** — PARTIALLY DONE (F-02 CI gates, 2026-07-22; see `.specs/features/e01-foundation-devsecops/spec.md`)

- ~~Coverage gate ≥80%~~ — intentionally NOT added, D-002
- OWASP dependency-check (fail on CVSS ≥ 7) — DONE
- Trivy container scan (fail on CRITICAL/HIGH) — DONE
- Branch protection: `master` requires CI green + 1 review — DONE
- AWS Secrets Manager wiring (`SecretsManagerConfigurationProvider`/`AddSecretsManager()`, copied from identity-api's real pattern — no literal `ISecretsProvider` interface exists anywhere), RS256 JWT validation against identity-api's public key — DONE (2026-07-22)
- ADR-AR-001: RS256 JWT validation (corrected from the plan's original HS256 assumption) — DONE

---

## M2 — Domain Model & Core Asset Logic

**Goal:** Pure domain — Asset aggregate, Category, Media VOs, status lifecycle, zero framework deps.
**Target:** Day 4–7 — E-02
**Status:** DONE (2026-07-22) — see `.specs/features/e02-domain-model/` and STATE.md Feature Completion Log

### Features

**Asset Aggregate & Value Objects** — DONE

- Asset aggregate root, AssetTitle/AssetDescription/Money/Media value objects, AssetStatus enum, Category entity
- Domain events: `AssetCreated`, `AssetMediaUploaded`, `AssetPublished`, `AssetSuspended`

**Domain Services & Repository Contracts** — DONE

- `IAssetRepository`, `ICategoryRepository`, `IMediaStorageService`, `IOwnerStatusValidator`
- 100% unit-tested domain layer (60/60 in new `Tests.Domain` project), ADR-AR-002/003 (status lifecycle, Category as entity)

---

## M3 — Application Layer — Use Cases

**Goal:** All asset use cases implemented via `IHandler<TRequest,TResponse>`.
**Target:** Day 8–13 — E-03

### Features

**Asset Creation & Idempotency** — PARTIALLY DONE (US-010 done 2026-07-22, see `.specs/features/e03-f05-create-asset/spec.md`)

- `CreateAsset` with idempotency key check (`GetByIdempotencyKeyAsync`), owner-status validation — DONE
- `AssetCreated` Kafka outbox publish — N/A here by design: handler never publishes directly, DynamoDB Streams outbox (E-04, ADR-AR-010) picks up the event already raised on the entity
- Draft TTL (30 days, ADR-AR-004) — DEFERRED (US-011, DynamoDB TTL is an E-04 concern)

**Media Upload Pipeline** — DONE (US-012 done 2026-07-22, see `.specs/features/e03-f06-media-upload/spec.md`)

- `RequestMediaUpload` (pre-validate size/MIME before presigned URL — ADR-AR-005), `ConfirmMediaUpload` — DONE

**Categorization** — DONE (US-013 done 2026-07-22, see `.specs/features/e03-f07-categorization/spec.md`)

- Admin-only `CreateCategory`/`UpdateCategory`, public `ListCategories` (caching deferred, no infra yet), re-parenting cycle prevention (ADR-AR-006) — DONE, leaf-only re-parent for now

**Search & Discovery** — DONE (US-014 done 2026-07-22, see `.specs/features/e03-f08-search-assets/spec.md`)

- `SearchAssets` (category/price/keyword, cursor-based pagination) — DONE, always restricted to `Active` assets
- Contract correction: `AssetSearchFilter`/`ISearchRepository` reworked from offset (`Page`/`Total`) to cursor (`NextPageToken`) pagination — DynamoDB has no native "skip N" or cheap total count
- GSI design, DynamoDB `contains` filter implementation — still PLANNED (E-04, ADR-AR-007 watch item past ~10k assets)

**Moderation Workflow** — DONE (US-015 done 2026-07-22, see `.specs/features/e03-f09-moderation-workflow/spec.md`)

- `SubmitForModeration` (owner-only, Draft→PendingModeration), `ApplyModerationVerdict` (Application-layer contract the future Kafka consumer calls; Approved→Active, Rejected/PendingReview held in PendingModeration, idempotent), `AdminReviewAsset` override (ADR-AR-008) — DONE
- Verified real event contract against `rentifyx-ai-services`: `AssetMediaModerated(AssetId, Verdict, Labels, TopConfidence, Timestamp, SchemaVersion)`, local `ModerationVerdict` enum mirrors it without a package dependency
- Kafka consumer `IHostedService` itself — still PLANNED (E-04, same as DynamoDB repository and S3 storage adapter)

---

## M4 — Infrastructure Layer — AWS Integration

**Goal:** DynamoDB, S3, Kafka all wired and integration-tested via Testcontainers.
**Target:** Day 14–17 — E-04

### Features

**DynamoDB Repository & Outbox** — DONE (2026-07-23, see `.specs/features/e04-f10-dynamodb-repository/`)

- `DynamoDbAssetRepository`/`DynamoDbCategoryRepository`, single-table design (ADR-AR-009) — DONE
- Outbox delivery via `PeriodicTimer` poll-loop `OutboxPublisher` `IHostedService` (ADR-AR-010) — DONE. **Correction:** the plan's original text said "DynamoDB Streams, consistent with identity-api's DEF-005" — that was wrong, identity-api's real DEF-005 rejected Streams (no Lambda/compute available) in favor of the same poll-loop pattern implemented here.
- Cursor-paginated `SearchAsync` (GSI2/GSI4 + FilterExpression), `PageTokenCodec`, `InvalidPageTokenException` translated to `Error.Validation` at `SearchAssetsHandler` — DONE

**S3 Media Storage** — DONE (2026-07-23)

- `S3MediaStorageService` real presigned-URL + `ValidateUploadAsync` implementation via AWSSDK.S3, `assets/{ownerId}/{assetId}/{filename}` key convention confirmed cross-repo against `rentifyx-ai-services`'s `AssetKeyConventionFilter` (STATE.md G-001 resolved) — DONE
- Terraform S3 bucket (CORS, presigned-URL-only bucket policy, multipart cleanup lifecycle) — still PLANNED, deferred to E-06 (`iac/` has no module scaffolding for any resource yet, documented as a Known Gap in F-11's spec.md)

**Cross-Service Integration** — DONE (2026-07-24, code-complete; integration-test run pending, see STATE.md G-006)

- `OwnerStatusConsumer`: Kafka consumer for identity-api's `user-lifecycle-events` topic (`UserSuspended`/`UserAccountDeleted` — real event name, not the `UserDeleted` earlier assumed), DynamoDB-backed local owner-status cache (`DynamoDbOwnerStatusValidator`) — DONE
- Fail-closed decision for stale/missing cache confirmed by user, ADR-AR-011 written — DONE. Cold-start backfill strategy still open (STATE.md G-005)
- `ModerationVerdictConsumer`: Kafka consumer `BackgroundService` for `asset-media-moderated` from `rentifyx-ai-services`, calling F-09's `ApplyModerationVerdictHandler` (plan T-076, ADR-AR-008) — DONE

---

## M5 — API Layer — Endpoints & Security

**Goal:** All endpoints live, OWASP hardened, fully documented.
**Target:** Day 18–19 — E-05

### Features

**Minimal API Endpoints** — DONE (2026-07-24, see `.specs/features/e05-f13-minimal-api-endpoints/`)

- Assets: Create, GetById, Search, RequestMediaUpload, ConfirmMediaUpload, SubmitForModeration — DONE
- Categories: Create/Update/List — DONE
- AdminReview — DONE
- All endpoints `AllowAnonymous` (real admin-gating is Security Hardening's job, not this feature's — `IsAdmin`/`OwnerId` still caller-supplied body fields, unchanged from E-03/E-04)

**Security Hardening** — IN PROGRESS

- Rate limiting (IP + user) — DONE (fixed-window, `RateLimitExtension`, pre-existing before this slice)
- Security headers (HSTS/CSP/etc.) — DONE (2026-07-24): `SecurityHeadersMiddleware` sets `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` on every response plus a strict `Content-Security-Policy` (`default-src 'none'`) outside Development (Scalar's docs UI is HTML and only mapped in Development); `app.UseHsts()` wired behind the same `!IsDevelopment()` gate as `UseHttpsRedirection`
- Request size limiting — DONE (2026-07-24): `RequestSizeLimitMiddleware` + Kestrel `MaxRequestBodySize`, both driven by `RequestSizeLimit:MaxBytes` (default 1 MB — this API only receives JSON metadata, file uploads go direct-to-S3 per ADR-AR-005)
- Role-based authorization for admin endpoints — still PLANNED, explicitly out of scope for this slice (JWT-claims wiring, `.RequireAuthorization()`, `IsAdmin`/`OwnerId` handling untouched)

**API Docs** — DONE (2026-07-24)

- Scalar UI + OpenAPI 3.1, example bodies, XML doc comments — DONE. Fixed stale "Clean Architecture Template" document description; added `AddOpenApiOperationTransformer` examples to all 10 endpoints (request-body examples on the 7 endpoints with a body, response examples via `.Produces<T>()` + transformer on the 3 GET-only endpoints); `///` XML doc comments on all 10 endpoint classes + `OpenApiExtensions`; `GenerateDocumentationFile` enabled on the Api project with CS1591 suppressed project-wide (documented tradeoff — the rest of the Api project's public surface, e.g. `Messaging`/`Extensions`/`Middlewares`, was out of scope for this feature and TreatWarningsAsErrors=true would otherwise fail the build over unrelated files)

---

## M6 — IaC & Production Readiness

**Goal:** Full Terraform, Helm, SLOs, ADR set finalized, tag v1.0.0.
**Target:** Day 20 — E-06

### Features

**Terraform & Kubernetes** — PLANNED

- DynamoDB (Streams enabled), S3, KMS modules; IRSA IAM roles; SSM Parameter Store cross-repo outputs (`/rentifyx/asset-registry/*`); Helm chart (HPA min 2/max 10)

**Observability, SLOs & Ship Gate** — PLANNED

- SLOs (`/assets/search` p99 < 400ms, availability > 99.9%, error rate < 0.1%), custom OTEL metrics, CloudWatch/Datadog alerting
- Final OWASP ZAP scan, secrets audit, admin-authz penetration-style test, coverage ≥80%, ADRs AR-001–011 finalized, tag v1.0.0

---

## Future Considerations

- OpenSearch/ElasticSearch migration once catalog exceeds ~10k assets (watch item from ADR-AR-007)
- Booking/availability calendar — owned by `leasing-api`, not this repo
