# RentifyX Asset Registry API — Project Plan

**Repo:** `rentifyx-asset-registry-api`
**Stack:** .NET 10 · Minimal APIs · Clean Architecture · DDD · TDD · AWS (DynamoDB, S3, KMS, Secrets Manager, EKS/Fargate) · Terraform · .NET Aspire · Docker · GitHub Actions · OpenTelemetry · Serilog · Kafka · ErrorOr\<T\>
**Template:** `dotnet new install EugenioBandeira.CleanArchTemplate && dotnet new clean-arch -n RentifyX.AssetRegistry`
**Estimate:** 6 Epics · ~20 days · ~111 tasks
**Scope:** Asset creation, categorization, media uploads (presigned S3 URLs), search, moderation.
**Explicitly out of scope:** Booking/availability calendar — deferred to `leasing-api`.
**Depends on:** `identity-api` (owner account status, JWT validation) · **Feeds:** `rentifyx-ai-services` (AssetCreated event → moderation/enrichment)

---

## Epic Overview

| # | Epic | Days | Goal |
|---|------|------|------|
| E-01 | Project Foundation & DevSecOps Pipeline | 1–3 | Template gives Day 1–2 free — focus on CI security gates, secrets, S3 bucket bootstrap |
| E-02 | Domain Model & Core Asset Logic | 4–7 | Pure domain — Asset aggregate, Category, Media VOs, status lifecycle, zero framework deps |
| E-03 | Application Layer — Use Cases | 8–13 | Create, categorize, media upload, search, moderation use cases |
| E-04 | Infrastructure Layer — AWS Integration | 14–17 | DynamoDB single-table, S3 presigned URLs, Kafka event publishing, cross-service auth |
| E-05 | API Layer — Endpoints & Security | 18–19 | Minimal API endpoints, OWASP hardening, admin authorization |
| E-06 | IaC & Production Readiness | 20 | Terraform, Helm/EKS, SLOs, ADR finalization, v1.0.0 ship |

---

## E-01 · Project Foundation & DevSecOps Pipeline (Day 1–3)

**Goal:** Template gives Day 1–2 for free — focus on CI security gates, S3 bucket bootstrap, secrets.

### F-01 · Repo & Solution Structure

**US-001** — As a dev, I want a clean solution scaffold so I can start coding without friction
- [ ] T-001 `[AUTO]` Run: `dotnet new install EugenioBandeira.CleanArchTemplate && dotnet new clean-arch -n RentifyX.AssetRegistry`
- [ ] T-002 `[AUTO]` Solution scaffold: API, Application, Domain, Infrastructure, Tests layers
- [ ] T-003 `[AUTO]` Directory.Packages.props with centralized versioning
- [ ] T-004 `[AUTO]` Directory.Build.props (Nullable, TreatWarningsAsErrors)
- [ ] T-005 `[AUTO]` .NET Aspire AppHost + ServiceDefaults projects
- [ ] T-006 `[AUTO]` Serilog + CorrelationId middleware + GlobalExceptionHandler
- [ ] T-007 `[AUTO]` OpenTelemetry traces + metrics via ServiceDefaults
- [ ] T-008 `[AUTO]` Health checks `/health/live` + `/health/ready`
- [ ] T-009 `[AUTO]` Scalar UI + endpoint auto-discovery via reflection
- [ ] T-010 `[AUTO]` ErrorOr\<T\> as standard result type
- [ ] T-011 Copy updated `.editorconfig` (CA5xxx security rules) into repo

**US-002** — As a dev, I want AWS containers in Aspire so I can run DynamoDB, S3 locally with one command
- [ ] T-012 Add LocalStack container to AppHost (DynamoDB, S3, KMS)
- [ ] T-013 Add LocalStack init scripts: create DynamoDB table, create media S3 bucket + CORS config
- [ ] T-014 Validate: `dotnet run --project AppHost` boots all containers cleanly

### F-02 · CI/CD Pipeline & DevSecOps Baseline

**US-003** — As a tech lead, I want automated security gates so vulnerabilities never reach main
- [ ] T-015 `[AUTO]` GitHub Actions base workflow: build → test
- [ ] T-016 Extend CI: coverage gate ≥80% (coverlet + ReportGenerator)
- [ ] T-017 Add OWASP dependency-check step (NuGet vulnerability scan)
- [ ] T-018 Add Trivy container scan step for Docker image
- [ ] T-019 Configure branch protection: CI green + 1 PR review before merge to main

**US-004** — As a dev, I want secrets and cross-service auth configured so the service never hardcodes credentials
- [ ] T-020 Add `ISecretsProvider` abstraction in Infrastructure layer (reused pattern from identity-api)
- [ ] T-021 Configure AWSSDK.SecretsManager — load any signing/shared secrets at startup
- [ ] T-022 Configure JWT validation against identity-api's signing key (HS256, in-service validation — no API Gateway JWT authorizer, per ADR established in identity-api)
- [ ] T-023 Document ADR-AR-001: reuse of identity-api's HS256 JWT validation pattern

---

## E-02 · Domain Model & Core Asset Logic (Day 4–7)

**Goal:** Pure domain — no frameworks, no AWS, fully unit-tested.

### F-03 · Asset Aggregate & Value Objects

**US-005** — As a domain expert, I want a rich Asset aggregate that enforces business rules
- [ ] T-024 Define Asset aggregate root: Id, OwnerId, Title, Description, CategoryId, Status, CreatedAt, UpdatedAt
- [ ] T-025 Create AssetTitle / AssetDescription value objects (length + content validation)
- [ ] T-026 Create Money value object for pricing fields (currency + amount, BRL default)
- [ ] T-027 Create Media value object: S3 key, MIME type, size, upload status
- [ ] T-028 Create AssetStatus enum: Draft | PendingModeration | Active | Suspended | Archived
- [ ] T-029 Create Category entity: Id, Name, ParentCategoryId (supports nested categories)

**US-006** — As a dev, I want domain events so other services react to asset changes via Kafka
- [ ] T-030 Reuse IEvent/IDomainEvent interfaces (shared contract with identity-api pattern)
- [ ] T-031 Create AssetCreated domain event (AssetId, OwnerId, CategoryId, OccurredAt)
- [ ] T-032 Create AssetMediaUploaded domain event
- [ ] T-033 Create AssetPublished domain event (Draft → Active transition)
- [ ] T-034 Create AssetSuspended domain event (reason, suspendedBy)
- [ ] T-035 Add RaiseDomainEvent() to AggregateRoot base class

### F-04 · Domain Services & Repository Contracts

**US-007** — As a dev, I want domain-layer contracts so Infrastructure can be swapped freely
- [ ] T-036 Define IAssetRepository: GetById, GetByOwner, Save, SoftDelete, Search
- [ ] T-037 Define ICategoryRepository: GetById, GetAll, Save (admin-only writes)
- [ ] T-038 Define IMediaStorageService: GeneratePresignedUploadUrl, ValidateUpload
- [ ] T-039 Define IOwnerStatusValidator: IsOwnerActive(ownerId) — backed by local cache of identity-api events

**US-008** — As a dev, I want 100% unit-tested domain layer with no I/O
- [ ] T-040 Unit tests: AssetTitle/Description VOs — valid, invalid, edge cases
- [ ] T-041 Unit tests: Media VO — MIME/size validation combinations
- [ ] T-042 Unit tests: Asset aggregate — state transitions (Draft→PendingModeration→Active→Suspended)
- [ ] T-043 Unit tests: Category — nested category depth limits
- [ ] T-044 Unit tests: Domain events — correct payload, zero framework deps

**US-009** — As an architect, I want ADRs for every key domain decision
- [ ] T-045 ADR-AR-002: Asset status lifecycle rationale (why PendingModeration is a distinct state from Draft)
- [ ] T-046 ADR-AR-003: Category as separate entity vs. embedded enum — supports admin-managed taxonomy
- [ ] T-047 Review: zero framework dependencies + zero AWS references in Domain layer

---

## E-03 · Application Layer — Use Cases (Day 8–13)

**Goal:** All asset use cases implemented via IHandler\<TRequest,TResponse\>.

### F-05 · Asset Creation & Idempotency

**US-010** — As an owner, I want to create an asset listing so I can offer it for rent
- [ ] T-048 `[AUTO]` Feature folder: Application/Features/Assets/Create/
- [ ] T-049 Create CreateAssetRequest + CreateAssetHandler
- [ ] T-050 Add CreateAssetValidator (FluentValidation): title, description, category, price rules
- [ ] T-051 **Idempotency check**: DynamoDB conditional write keyed by client-supplied idempotency key, rejects duplicate submissions
- [ ] T-052 **Owner account status validation**: check IOwnerStatusValidator before allowing creation (reject if suspended/deleted)
- [ ] T-053 Publish AssetCreated to Kafka Outbox on success
- [ ] T-054 Unit tests: CreateAssetHandler — success + all failure paths (duplicate key, suspended owner, invalid data)

**US-011** — As an owner, I want unfinished listings to expire automatically so stale drafts don't clutter the catalog
- [ ] T-055 DynamoDB TTL on Draft-status assets: auto-delete after 30 days (mirrors identity-api pattern)
- [ ] T-056 ADR-AR-004: 30-day Draft TTL rationale
- [ ] T-057 Unit tests: TTL field correctly set on Draft creation, cleared on transition to PendingModeration

### F-06 · Media Upload Pipeline

**US-012** — As an owner, I want to upload photos for my asset via a secure, direct-to-S3 flow
- [ ] T-058 Create RequestMediaUploadRequest + handler
- [ ] T-059 **Validate file size/MIME type BEFORE generating the presigned URL** (reject oversized/disallowed types up front, not after upload)
- [ ] T-060 Generate S3 presigned PUT URL scoped to a specific object key (owner + asset scoped path)
- [ ] T-061 Create ConfirmMediaUploadRequest + handler (client confirms upload completion, triggers AssetMediaUploaded event)
- [ ] T-062 ADR-AR-005: pre-validation before presigned URL generation vs. post-upload validation
- [ ] T-063 Unit tests: oversized file rejection, disallowed MIME type, valid flow

### F-07 · Categorization

**US-013** — As an admin, I want to manage the category taxonomy so listings stay organized
- [ ] T-064 Create CreateCategoryRequest + handler — **admin-only** (role-based authorization)
- [ ] T-065 Create UpdateCategoryRequest + handler (rename, re-parent) — admin-only
- [ ] T-066 Create ListCategoriesRequest + handler (public read, cached)
- [ ] T-067 ADR-AR-006: admin-only Category creation/mutation — prevents unmoderated taxonomy sprawl
- [ ] T-068 Unit tests: non-admin rejected, admin succeeds, re-parenting cycle prevention

### F-08 · Search & Discovery

**US-014** — As a renter, I want to search assets by category, price range, and keyword
- [ ] T-069 Create SearchAssetsRequest + handler (category filter, price range, keyword, pagination)
- [ ] T-070 DynamoDB GSI design: GSI1=CATEGORY#{categoryId}, GSI2=STATUS#{status}#CREATED#{createdAt}
- [ ] T-071 Keyword search: DynamoDB `contains` filter for v1 (documented scalability ceiling)
- [ ] T-072 **Watch item**: flag that DynamoDB-native search will need OpenSearch/ElasticSearch once catalog exceeds ~10k assets
- [ ] T-073 ADR-AR-007: DynamoDB search for v1, OpenSearch migration path documented for scale
- [ ] T-074 Unit tests: filter combinations, pagination boundaries, empty result sets

### F-09 · Moderation Workflow

**US-015** — As the system, I want assets to pass moderation before becoming publicly visible
- [ ] T-075 Create SubmitForModerationRequest + handler (Draft → PendingModeration transition)
- [ ] T-076 Consume AssetMediaModerated event from rentifyx-ai-services (Kafka consumer, IHostedService)
- [ ] T-077 Auto-approve path: verdict=approved → PendingModeration → Active, publish AssetPublished
- [ ] T-078 Auto-reject / manual-review path: verdict=rejected or pending-review → held in PendingModeration, admin notified
- [ ] T-079 Create AdminReviewAssetRequest + handler (manual override: approve/reject)
- [ ] T-080 ADR-AR-008: moderation is event-driven from rentifyx-ai-services, not a synchronous call
- [ ] T-081 Unit tests: all verdict paths, admin override, idempotent event consumption

---

## E-04 · Infrastructure Layer — AWS Integration (Day 14–17)

**Goal:** DynamoDB, S3, Kafka all wired and integration-tested via Testcontainers.

### F-10 · DynamoDB Repository & Outbox

**US-016** — As a dev, I want a DynamoDB-backed asset repository
- [ ] T-082 Implement DynamoDbAssetRepository: Save, GetById, GetByOwner, Search, SoftDelete
- [ ] T-083 DynamoDB single-table design: PK=ASSET#{id}, GSI1=CATEGORY#{categoryId}, GSI2=STATUS#{status}
- [ ] T-084 Implement DynamoDbCategoryRepository (small table, heavily cached)
- [ ] T-085 Testcontainers.DynamoDb + LocalStack integration tests for all repository methods
- [ ] T-086 ADR-AR-009: single-table DynamoDB design rationale (mirrors identity-api ADR-005)

**US-017** — As a dev, I want the Outbox Pattern via DynamoDB Streams (not a custom OutboxPublisher)
- [ ] T-087 Enable DynamoDB Streams on the Asset table
- [ ] T-088 Implement Streams-to-Kafka bridge (Lambda or IHostedService consumer — decide per DEF-005 precedent from identity-api)
- [ ] T-089 Dead-letter handling: after 3 retries → move to DLQ Kafka topic
- [ ] T-090 Integration tests: stream event triggers Kafka publish end-to-end
- [ ] T-091 ADR-AR-010: DynamoDB Streams over custom Outbox table (consistent with identity-api DEF-005 resolution)

### F-11 · S3 Media Storage

**US-018** — As a dev, I want S3 configured securely for direct owner uploads
- [ ] T-092 Terraform: S3 bucket with CORS policy scoped to rentifyx-web origin only
- [ ] T-093 Bucket policy: presigned URLs only, no public write, versioning enabled
- [ ] T-094 Lifecycle policy: incomplete multipart uploads cleaned up after 24h
- [ ] T-095 Testcontainers.LocalStack integration test: presigned URL generation + upload flow

### F-12 · Cross-Service Integration

**US-019** — As a dev, I want owner status kept in sync without a synchronous call to identity-api
- [ ] T-096 Kafka consumer: subscribe to UserSuspended / UserDeleted events from identity-api
- [ ] T-097 Local cache (DynamoDB or in-memory + TTL) of owner status, updated on event consumption
- [ ] T-098 Fallback behavior if cache is stale/missing: fail closed (reject creation) vs. fail open — decide and document
- [ ] T-099 ADR-AR-011: event-driven owner status sync over synchronous HTTP call to identity-api
- [ ] T-100 Integration tests: event consumption updates cache, creation respects cached status

---

## E-05 · API Layer — Endpoints & Security (Day 18–19)

**Goal:** All endpoints live, OWASP hardened, fully documented.

### F-13 · Minimal API Endpoints

**US-020** — As a client, I want clean REST endpoints for the asset catalog
- [ ] T-101 `[AUTO]` Endpoint auto-registration via IEndpoint reflection
- [ ] T-102 Api/Endpoints/Assets/Create.cs → `POST /api/v1/assets`
- [ ] T-103 Api/Endpoints/Assets/GetById.cs → `GET /api/v1/assets/{id}`
- [ ] T-104 Api/Endpoints/Assets/Search.cs → `GET /api/v1/assets/search`
- [ ] T-105 Api/Endpoints/Assets/RequestMediaUpload.cs → `POST /api/v1/assets/{id}/media/upload-url`
- [ ] T-106 Api/Endpoints/Assets/ConfirmMediaUpload.cs → `POST /api/v1/assets/{id}/media/confirm`
- [ ] T-107 Api/Endpoints/Categories/Create.cs, Update.cs, List.cs (admin-gated where applicable)
- [ ] T-108 Api/Endpoints/Assets/AdminReview.cs → `POST /api/v1/assets/{id}/review` (admin-only)

**US-021** — As a security engineer, I want hardened HTTP middleware
- [ ] T-109 `[AUTO]` GlobalExceptionHandler: no stack trace in responses
- [ ] T-110 `[AUTO]` CorrelationId middleware
- [ ] T-111 Rate limiting middleware: IP-based + user-based
- [ ] T-112 Security headers: HSTS, X-Content-Type-Options, X-Frame-Options, CSP
- [ ] T-113 Request size limiting middleware (relevant given media metadata payloads)
- [ ] T-114 Role-based authorization middleware for admin-only endpoints (Category, AdminReview)

**US-022** — As a dev, I want auto-generated API docs
- [ ] T-115 `[AUTO]` Scalar UI at `/scalar` with OpenAPI 3.1 schema
- [ ] T-116 Add example request/response bodies to all endpoints
- [ ] T-117 XML doc comments on all endpoint handlers

---

## E-06 · Infrastructure as Code & Production Readiness (Day 20)

**Goal:** Full Terraform, Helm, SLOs, ADR set finalized, tag v1.0.0.

### F-14 · Terraform & Kubernetes

**US-023** — As a DevOps engineer, I want 100% IaC so infra is reproducible
- [ ] T-118 Terraform module: aws_dynamodb_table (assets, categories) with Streams enabled
- [ ] T-119 Terraform module: aws_s3_bucket (media) with CORS + lifecycle policy
- [ ] T-120 Terraform module: aws_kms_key (if any PII fields require encryption at rest)
- [ ] T-121 Terraform: IAM roles for EKS service account (IRSA), least-privilege scoped to this service's resources only
- [ ] T-122 Cross-repo output sharing via SSM Parameter Store under `/rentifyx/asset-registry/*` (consistent with platform pattern)
- [ ] T-123 Helm chart: Deployment, Service, HPA (min 2 / max 10 replicas)
- [ ] T-124 Liveness/readiness probes, resource requests/limits, PodDisruptionBudget

### F-15 · Observability, SLOs & Ship Gate

**US-024** — As a dev, I want SLOs and custom metrics so degradation is caught early
- [ ] T-125 Define SLOs: `/assets/search` p99 < 400ms, availability > 99.9%, error rate < 0.1%
- [ ] T-126 Custom OTEL metrics: `assets_created_total`, `media_uploads_total`, `moderation_verdicts_total`, `search_latency_ms`
- [ ] T-127 CloudWatch/Datadog dashboard + alert: PagerDuty trigger if error rate > 1% for 5min

**US-025** — As a tech lead, I want a final security review before v1.0.0
- [ ] T-128 Run OWASP ZAP scan against local env — fix all High/Critical findings
- [ ] T-129 Verify: no secrets in code/logs/error responses
- [ ] T-130 Verify: admin-only endpoints reject non-admin roles correctly (penetration-style test)
- [ ] T-131 Final coverage run: enforce ≥80% across all layers
- [ ] T-132 Finalize ADRs AR-001 through AR-011, cross-link with identity-api and ai-services ADRs
- [ ] T-133 Tag v1.0.0 → push Docker image to ECR → trigger staging deploy via GitHub Actions

---

## Known Decisions & Watch Items

| ID | Decision |
|----|----------|
| ADR-AR-001 | Reuse identity-api's HS256 JWT validation pattern — no API Gateway JWT authorizer |
| ADR-AR-002 | Asset status lifecycle: Draft → PendingModeration → Active → Suspended/Archived |
| ADR-AR-003 | Category as a first-class entity, not an enum — supports admin-managed nested taxonomy |
| ADR-AR-004 | 30-day Draft TTL, mirroring identity-api's unverified-account TTL pattern |
| ADR-AR-005 | File size/MIME validation happens **before** presigned URL generation, not after upload |
| ADR-AR-006 | Category creation/mutation is **admin-only** |
| ADR-AR-007 | DynamoDB-native search for v1; OpenSearch is the documented migration path past ~10k assets |
| ADR-AR-008 | Moderation is event-driven from `rentifyx-ai-services`, never a synchronous call |
| ADR-AR-009 | Single-table DynamoDB design (PK=ASSET#{id}, GSIs for category/status) |
| ADR-AR-010 | DynamoDB Streams as the Outbox mechanism — consistent with identity-api's DEF-005 resolution, avoids building a custom Outbox table from scratch |
| ADR-AR-011 | Owner account status is synced via Kafka events from identity-api (UserSuspended/UserDeleted), not a synchronous HTTP call — avoids a hard runtime dependency between services |

**Watch item:** DynamoDB keyword search (`contains` filter) is a v1-only decision. Flag for revisit once the catalog approaches ~10k assets — plan is OpenSearch/ElasticSearch migration, not a DynamoDB scaling fix.

**Open question carried into E-04 (US-019):** fail-open vs. fail-closed behavior when the local owner-status cache is stale or the event hasn't arrived yet. Needs an explicit decision before T-098 is implemented — recommend fail-closed (reject creation) as the safer default given LGPD/marketplace trust considerations, but this should be confirmed, not assumed.

---

## Gap Analysis vs. Original Scope Note

- Booking/availability calendar is **explicitly out of scope** here — confirmed deferred to `leasing-api`. No task in this plan touches availability, calendars, or reservation state.
- Search is intentionally scoped to "good enough for v1" (DynamoDB filter), not a full search engine — documented as a watch item, not silently deferred.
- Moderation logic itself (Rekognition calls, thresholds) lives in `rentifyx-ai-services`; this repo only consumes the resulting event and manages the state transition — keeping the event-only boundary between the two services explicit (ADR-AR-008).
