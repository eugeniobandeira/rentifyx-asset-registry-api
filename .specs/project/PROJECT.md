# RentifyX — Asset Registry API

**Vision:** Production-grade asset catalog microservice for RentifyX, a marketplace where people can rent anything — from real estate to equipment, tools, and vehicles.
**For:** Platform users (Owners listing items for rent, Renters browsing/searching the catalog, Admins moderating taxonomy and content)
**Solves:** Asset creation, categorization, media uploads, search, and moderation — the catalog layer every other RentifyX service (leasing, ai-services) reads from.

## Goals

- Ship all 6 epics (~111 tasks) across ~20 days with zero critical OWASP findings
- Keep the Domain layer 100% unit-tested with zero framework/AWS dependencies (E-02 gate)
- Deliver the full asset lifecycle (Draft → PendingModeration → Active → Suspended/Archived) with event-driven moderation from `rentifyx-ai-services` by E-03
- Make the service observable and operable from day one (structured logs, OTel traces, health checks, Scalar UI)

## Tech Stack

**Core:**

- Framework: .NET 10 — Minimal APIs
- Language: C# (latest)
- Database: AWS DynamoDB, single-table design (LocalStack locally)

**Key dependencies:**

- ErrorOr — result type for all handlers
- FluentValidation — request validation
- AWS S3 — presigned upload URLs for asset media
- AWS Secrets Manager + KMS — secrets and any PII-at-rest encryption
- Kafka — `AssetCreated`/`AssetMediaUploaded`/`AssetPublished`/`AssetSuspended` events out (via DynamoDB Streams outbox), owner-status events in (from `identity-api`)
- .NET Aspire — local orchestration, OTel, health checks
- Serilog — structured logging with correlation ID enrichment

## Scope

**v1 includes:**

- Asset creation with idempotency key + owner-status validation (reject suspended/deleted owners)
- Draft assets auto-expire after 30 days (DynamoDB TTL)
- Direct-to-S3 media upload: presigned PUT URL, pre-upload size/MIME validation, confirm-upload flow
- Admin-only category taxonomy (nested categories, cycle prevention)
- Search by category, price range, keyword, pagination (DynamoDB `contains` filter — v1 ceiling, OpenSearch migration documented)
- Event-driven moderation: consumes verdicts from `rentifyx-ai-services`, auto-approve/auto-reject/manual-review paths, admin override
- Cross-service owner-status sync via Kafka (`UserSuspended`/`UserDeleted` from `identity-api`), no synchronous call
- DevSecOps: coverage gate ≥80%, OWASP dependency-check, Trivy, branch protection

**Explicitly out of scope:**

- Booking/availability calendar — deferred to `leasing-api`
- Full search engine (OpenSearch/ElasticSearch) — v1 uses DynamoDB-native filtering only, flagged as a watch item past ~10k assets
- Moderation logic itself (Rekognition calls, thresholds) — lives in `rentifyx-ai-services`; this repo only consumes the verdict event

## Constraints

- Timeline: 20-day plan across 6 epics (E-01 → E-06), ~111 tasks
- Technical: AWS-native (DynamoDB, S3, KMS, Secrets Manager) — **note:** the `dotnet new clean-arch` template scaffolds EF Core + Npgsql by default; this must be swapped for DynamoDB during E-01/E-04 (see STATE.md D-001)
- Depends on: `identity-api` (owner account status via Kafka events, JWT validation — reuses identity-api's HS256 pattern, ADR-AR-001)
- Feeds: `rentifyx-ai-services` (`AssetCreated` event → moderation/enrichment)
- Compliance: OWASP Top 10
