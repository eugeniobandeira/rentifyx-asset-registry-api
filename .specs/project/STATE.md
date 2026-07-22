# Project State

## Last Updated

2026-07-21

## Current Work

**2026-07-21 session: project initialized from `RentifyX_AssetRegistryAPI_Plan.md`.** Created `.specs/project/` (PROJECT.md, ROADMAP.md, STATE.md) and root `CLAUDE.md`, following the conventions established in `rentifyx-identity-api` and `rentifyx-communications-api`. No feature work started yet — the repo currently holds only the raw `dotnet new clean-arch` template output (Example* scaffold, EF Core/Npgsql Infrastructure layer, generic `IAddRepository<T>`/`IUnitOfWork`/`AppDbContext`) plus the plan document itself. Turned out the repo already had a local `.git` with 2 prior commits and a GitHub remote (`origin`) — no `git init` was actually needed; docs committed and pushed to `master`.

**2026-07-21 session, part 2: solution/project/namespace renamed from `rentifyx-asset-registry-api`/`rentifyx_asset_registry_api` to `RentifyxAssetRegistry`.** The `dotnet new clean-arch` generator had used the repo's literal folder name for the solution file, all 12 `.csproj` files/directories, and the C# namespace (auto-converted to `rentifyx_asset_registry_api` since C# identifiers can't contain hyphens) — inconsistent with `RentifyxIdentity.*`/`RentifyxCommunications.*` PascalCase convention used in sibling repos. Renamed while the repo still only holds template scaffold (low cost): all `.csproj`/directory names, `.slnx`, `Dockerfile`, `k8s/base/deployment.yaml` (OTEL_SERVICE_NAME), `.github/workflows/ci.yml`, `.hooks/pre-commit`, `README.md`, `CLAUDE.md`. Root repo folder name (`rentifyx-asset-registry-api`, lowercase-hyphen) and the plan doc's `**Repo:**` field were deliberately left untouched — those identify the GitHub repo, not the .NET solution, matching how `identity-api`/`communications-api` keep the two naming schemes separate (lowercase-hyphen for repo/k8s/infra-facing names, PascalCase for .NET solution/namespace). Verified via `dotnet build`/`dotnet test` (Release) after the rename — 12/12 projects build clean, all Example* tests still skip as before (template default), zero failures. **Real pre-existing bug found and fixed along the way, unrelated to the rename itself:** `.hooks/pre-commit` pointed at `$(git rev-parse --show-toplevel)/templates/clean-arch/<solution>.slnx` — a path that has never existed in this repo (leftover from the template generator's own source tree) — meaning the hook would have failed every local commit once `git config core.hooksPath` was ever pointed at `.hooks/`. Fixed to reference the repo-root `.slnx` directly.

## Decisions

| ID | Decision | Rationale | Date |
|---|---|---|---|
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
