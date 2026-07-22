# Roadmap

**Current Milestone:** M2 — Domain Model & Core Asset Logic (complete) → next up M1 remaining items or M3
**Status:** M1 template scaffold present (EF Core/Postgres default, not yet swapped to DynamoDB); M2 domain model complete (E-02, 2026-07-22)

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

**CI/CD Pipeline & DevSecOps Baseline** — PLANNED

- GitHub Actions base workflow exists (`ci.yml`) — needs coverage gate ≥80%, OWASP dependency-check, Trivy scan, branch protection
- `ISecretsProvider` abstraction, AWS Secrets Manager wiring, JWT validation against identity-api's HS256 signing key
- ADR-AR-001: reuse of identity-api's HS256 JWT validation pattern

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

**Asset Creation & Idempotency** — PLANNED

- `CreateAsset` with idempotency key check, owner-status validation, `AssetCreated` Kafka outbox publish
- Draft TTL (30 days, ADR-AR-004)

**Media Upload Pipeline** — PLANNED

- `RequestMediaUpload` (pre-validate size/MIME before presigned URL — ADR-AR-005), `ConfirmMediaUpload`

**Categorization** — PLANNED

- Admin-only `CreateCategory`/`UpdateCategory`, public cached `ListCategories`, re-parenting cycle prevention (ADR-AR-006)

**Search & Discovery** — PLANNED

- `SearchAssets` (category/price/keyword/pagination), GSI design, DynamoDB `contains` filter (ADR-AR-007, watch item past ~10k assets)

**Moderation Workflow** — PLANNED

- `SubmitForModeration`, Kafka consumer for `AssetMediaModerated` from `rentifyx-ai-services`, auto-approve/reject/manual-review, `AdminReviewAsset` override (ADR-AR-008)

---

## M4 — Infrastructure Layer — AWS Integration

**Goal:** DynamoDB, S3, Kafka all wired and integration-tested via Testcontainers.
**Target:** Day 14–17 — E-04

### Features

**DynamoDB Repository & Outbox** — PLANNED

- `DynamoDbAssetRepository`/`DynamoDbCategoryRepository`, single-table design (ADR-AR-009)
- DynamoDB Streams as Outbox mechanism (ADR-AR-010, consistent with identity-api's DEF-005 resolution), DLQ after 3 retries

**S3 Media Storage** — PLANNED

- Terraform S3 bucket (CORS scoped to `rentifyx-web`), presigned-URL-only bucket policy, multipart cleanup lifecycle

**Cross-Service Integration** — PLANNED

- Kafka consumer for `UserSuspended`/`UserDeleted` from `identity-api`, local owner-status cache
- Fail-open vs. fail-closed decision for stale cache (ADR-AR-011) — recommend fail-closed, needs confirmation before implementation

---

## M5 — API Layer — Endpoints & Security

**Goal:** All endpoints live, OWASP hardened, fully documented.
**Target:** Day 18–19 — E-05

### Features

**Minimal API Endpoints** — PLANNED

- Assets: Create, GetById, Search, RequestMediaUpload, ConfirmMediaUpload
- Categories: Create/Update/List (admin-gated where applicable)
- AdminReview (admin-only)

**Security Hardening** — PLANNED

- Rate limiting (IP + user), security headers (HSTS/CSP/etc.), request size limiting, role-based authorization for admin endpoints

**API Docs** — PLANNED

- Scalar UI + OpenAPI 3.1, example bodies, XML doc comments

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
