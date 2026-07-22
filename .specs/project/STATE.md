# Project State

## Last Updated

2026-07-22

## Current Work

**2026-07-22 session, part 6: E-03/F-07 Categorization (US-013) complete (branch `feat/e03-f07-categorization`).** Domain gap found while speccing: `CategoryEntity` (E-02) only had `CreateRoot`/`CreateChild` — no rename or re-parent. Discussed scope with user: full re-parent with subtree-depth cascade was ruled out for now — re-parenting is restricted to leaf categories (no children), keeping cycle prevention to a simple self-parent + max-depth check (no ancestor-chain walk needed). Added `CategoryEntity.Rename`/`ReParent`. Delivered: `CreateCategoryHandler`/`UpdateCategoryHandler` (admin-only via caller-supplied `IsAdmin`, same temporary-gap pattern as `OwnerId` in F-05/F-06 pending E-05's real JWT-claims wiring), `ListCategoriesHandler` (public, no gate — caching deferred, no cache infra exists yet). ADR-AR-006 written. 7 validator tests + 11 handler tests (Moq) + 8 new Domain tests for Rename/ReParent, all green.

**2026-07-22 session, part 5: E-03/F-06 Media Upload Pipeline (US-012) complete (branch `feat/e03-f06-media-upload`).** Another domain-contract gap found while speccing: `IMediaStorageService.GeneratePresignedUploadUrlAsync` (E-02) took only `mimeType`/`sizeBytes` — couldn't scope the S3 key to owner+asset as the plan requires. Extended it to take `ownerId`/`assetId` and return a new `PresignedUploadUrl(Url, S3Key)` record. Delivered: `RequestMediaUploadHandler` (validates MIME/size before ever calling `IMediaStorageService`, checks asset existence/ownership), `ConfirmMediaUploadHandler` (re-validates client-supplied MIME/size defensively, attaches `Media` via `AssetEntity.AttachMedia` which raises `AssetMediaUploaded`, saves). New `AssetErrorCodes.NotFound`/`NotOwner`, validation message resource keys. ADR-AR-005 written. 10 validator tests + 9 handler tests (Moq), all green.

**2026-07-22 session, part 4: E-03/F-05 Asset Creation & Idempotency (US-010) complete (branch `feat/e03-f05-create-asset`).** First feature slice of E-03, split out per-feature rather than specifying the whole 5-feature epic at once (E-03 depends on Domain contracts only — E-04/Infrastructure doesn't exist yet, so each feature is its own spec-driven cycle). Two prerequisite fixes made before the feature itself: (1) renamed `IHandler<TRequest,TResponse>.Handle` to `HandleAsync` across the whole codebase (Example* handlers + endpoints) — CLAUDE.md's Async-suffix rule wasn't applied to this shared interface yet; (2) added `AssetEntity.IdempotencyKey` + `IAssetRepository.GetByIdempotencyKeyAsync` (E-02 didn't anticipate this field). Delivered: `CreateAssetHandler` (idempotent replay short-circuits before owner-status check; `Error.Forbidden` for suspended/deleted owners; never publishes to Kafka directly, relies on DynamoDB Streams outbox per ADR-AR-010), `CreateAssetValidator` (mirrors Domain VO bounds), `CreateAssetResponse`/`AssetMapper`, new `AssetErrorCodes`/validation message resource keys (EN + pt-BR). 10 validator tests + 4 handler tests (Moq), all green. US-011 (Draft TTL auto-expiry) deferred — DynamoDB TTL is an E-04 concern.

**2026-07-22 session, part 3: E-01/US-004 Secrets & Cross-Service Auth complete (branch `feat/e01-secrets-jwt`), M1 fully done.** Research into the actual `rentifyx-identity-api` repo found the plan's/CLAUDE.md's original HS256 assumption was wrong — `identity-api` signs with RS256 (its own ADR-006), and no real `ISecretsProvider` interface exists anywhere (only a `SecretsManagerConfigurationProvider`/`AddSecretsManager()` config-builder pattern). Corrected CLAUDE.md and copied the working pattern: secrets load from AWS Secrets Manager via a config provider (skips the AWS call in `Testing` environment, tolerates not-yet-seeded secrets), JWT bearer auth validates RS256 tokens against `identity-api`'s public key. ADR-AR-001 written (superseding the plan's HS256 assumption). `UseAuthentication`/`UseAuthorization` wired in `Program.cs`. Role-based authorization for admin endpoints is explicitly out of scope here (E-05). M1 (E-01) is now fully complete: F-01 scaffold, F-02 CI/CD gates, US-004 secrets/JWT all done; only the EF Core→DynamoDB swap (D-001) remains open, deferred to E-04 by design.

**2026-07-22 session, part 2: E-01/F-02 CI/CD Pipeline & DevSecOps Baseline complete (branch `feat/e01-cicd-devsecops`).** Added OWASP dependency-check (fails on CVSS ≥ 7) and Trivy container scan (fails on CRITICAL/HIGH) to `ci.yml`. Configured `master` branch protection via `gh api`: required status check `build-and-test` + 1 approving review, force-push/delete blocked. Coverage gate (plan's T-016) intentionally NOT added — see D-002. Secrets/JWT slice (US-004, T-020–T-023) deliberately deferred, not part of this pass. This is the first epic run under the new per-epic branch+PR workflow (see feedback memory `feedback_epic-workflow.md`).

**2026-07-22 session, part 1: E-02 Domain Model & Core Asset Logic complete.** Full spec-driven cycle (spec.md/design.md/tasks.md in `.specs/features/e02-domain-model/`), 22 atomic tasks executed via parallel sub-agents where independent, 24 commits. Delivered: `AggregateRoot`/`IDomainEvent`, `AssetStatus`/`MediaUploadStatus` enums, `AssetTitle`/`AssetDescription`/`Money`/`Media` value objects, `AssetCreated`/`AssetMediaUploaded`/`AssetPublished`/`AssetSuspended` domain events, `AssetEntity`/`CategoryEntity` aggregates with full status-lifecycle/depth-cap guards, `IAssetRepository`/`ICategoryRepository`/`IMediaStorageService`/`IOwnerStatusValidator` contracts, ADR-AR-002/003, new `03-tests/00-Domain` test project (60 tests, all green). Zero framework/AWS deps in Domain layer confirmed. Mid-flight architecture refinement: repository interfaces compose new generic `Domain/Interfaces/Common/{ISaveRepository,ISearchRepository,ISoftDeleteRepository,IGetAllRepository<T>}` building blocks rather than the `Example*` scaffold's EF-Core-shaped `IAddRepository`/`IUpdateRepository`/`IDeleteRepository` — documented in CLAUDE.md's "Adding a new feature" section. Next: E-03 (Application layer / use cases).

**2026-07-21 session: project initialized from `RentifyX_AssetRegistryAPI_Plan.md`.** Created `.specs/project/` (PROJECT.md, ROADMAP.md, STATE.md) and root `CLAUDE.md`, following the conventions established in `rentifyx-identity-api` and `rentifyx-communications-api`. No feature work started yet — the repo currently holds only the raw `dotnet new clean-arch` template output (Example* scaffold, EF Core/Npgsql Infrastructure layer, generic `IAddRepository<T>`/`IUnitOfWork`/`AppDbContext`) plus the plan document itself. Turned out the repo already had a local `.git` with 2 prior commits and a GitHub remote (`origin`) — no `git init` was actually needed; docs committed and pushed to `master`.

**2026-07-21 session, part 2: solution/project/namespace renamed from `rentifyx-asset-registry-api`/`rentifyx_asset_registry_api` to `RentifyxAssetRegistry`.** The `dotnet new clean-arch` generator had used the repo's literal folder name for the solution file, all 12 `.csproj` files/directories, and the C# namespace (auto-converted to `rentifyx_asset_registry_api` since C# identifiers can't contain hyphens) — inconsistent with `RentifyxIdentity.*`/`RentifyxCommunications.*` PascalCase convention used in sibling repos. Renamed while the repo still only holds template scaffold (low cost): all `.csproj`/directory names, `.slnx`, `Dockerfile`, `k8s/base/deployment.yaml` (OTEL_SERVICE_NAME), `.github/workflows/ci.yml`, `.hooks/pre-commit`, `README.md`, `CLAUDE.md`. Root repo folder name (`rentifyx-asset-registry-api`, lowercase-hyphen) and the plan doc's `**Repo:**` field were deliberately left untouched — those identify the GitHub repo, not the .NET solution, matching how `identity-api`/`communications-api` keep the two naming schemes separate (lowercase-hyphen for repo/k8s/infra-facing names, PascalCase for .NET solution/namespace). Verified via `dotnet build`/`dotnet test` (Release) after the rename — 12/12 projects build clean, all Example* tests still skip as before (template default), zero failures. **Real pre-existing bug found and fixed along the way, unrelated to the rename itself:** `.hooks/pre-commit` pointed at `$(git rev-parse --show-toplevel)/templates/clean-arch/<solution>.slnx` — a path that has never existed in this repo (leftover from the template generator's own source tree) — meaning the hook would have failed every local commit once `git config core.hooksPath` was ever pointed at `.hooks/`. Fixed to reference the repo-root `.slnx` directly.

## Decisions

| ID | Decision | Rationale | Date |
|---|---|---|---|
| D-002 | CI does NOT enforce an 80% coverage gate (plan's T-016 dropped). CI keeps build+test-pass verification only. | Explicit user decision, stated twice (2026-07-21 and 2026-07-22) — coverage gate adds noise without current value at this project stage | 2026-07-22 |
| D-001 | Template's default EF Core + Npgsql Infrastructure layer (`AppDbContext`, migrations, generic `IAddRepository<T>`/`IGetAllRepository<T>`/`IUpdateRepository<T>`/`IDeleteRepository<T>`, `IUnitOfWork`) must be replaced with DynamoDB per the plan — the plan's stack has no relational database. Deferred to E-01 wrap-up / E-04 (DynamoDB repository implementation). Until swapped, the `Example*` reference implementation still targets Postgres, not DynamoDB. | `dotnet new clean-arch` template is a generic starting point (its own README targets EF Core); this project's plan explicitly calls for DynamoDB single-table design (ADR-AR-009) | 2026-07-21 |

## Blockers

_None active._

## Known Gaps

| ID | Gap | Detail | Since | Resolved |
|---|---|---|---|---|

## Deferred Ideas

| ID | Idea | Deferred until |
|---|---|---|
| DEF-001 | OpenSearch/ElasticSearch migration for search | Past ~10k assets (ADR-AR-007 watch item) |
| DEF-002 | Booking/availability calendar | Owned by `leasing-api`, explicitly out of scope here |

## Open Questions

- **Fail-open vs. fail-closed** when the local owner-status cache (synced from `identity-api` via Kafka) is stale or the event hasn't arrived yet (US-019/T-098). Plan recommends fail-closed (reject creation) given LGPD/marketplace trust considerations — needs explicit confirmation before implementing, not to be assumed.

## Lessons Learned

| ID | Lesson | Context |
|---|---|---|

## Preferences

- All output (code, docs, specs, comments) must be in English (mirrors `identity-api`/`communications-api` convention — carried forward, not yet explicitly confirmed by user for this repo)

## Feature Completion Log

| Feature | Tasks | Tests | Completed |
|---|---|---|---|
| E-02 Domain Model & Core Asset Logic | 22/22 | 60/60 (Tests.Domain) | 2026-07-22 |
| E-01/F-02 CI/CD Pipeline & DevSecOps Baseline | 3/3 (CICD-01/02/03) | n/a (CI config) | 2026-07-22 |
| E-01/US-004 Secrets & Cross-Service Auth | 3/3 (SEC-01/02/03) | n/a (build+config, verified via full test suite) | 2026-07-22 |
| E-03/F-05 Asset Creation & Idempotency (US-010) | CA-01..04 | 14/14 (10 validator + 4 handler) | 2026-07-22 |
| E-03/F-06 Media Upload Pipeline (US-012) | MU-01..04 | 19/19 (10 validator + 9 handler) | 2026-07-22 |
| E-03/F-07 Categorization (US-013) | CAT-01..05 | 26/26 (7 validator + 11 handler + 8 domain) | 2026-07-22 |
