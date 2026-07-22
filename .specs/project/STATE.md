# Project State

## Last Updated

2026-07-21

## Current Work

**2026-07-21 session: project initialized from `RentifyX_AssetRegistryAPI_Plan.md`.** Created `.specs/project/` (PROJECT.md, ROADMAP.md, STATE.md) and root `CLAUDE.md`, following the conventions established in `rentifyx-identity-api` and `rentifyx-communications-api`. No feature work started yet — the repo currently holds only the raw `dotnet new clean-arch` template output (Example* scaffold, EF Core/Npgsql Infrastructure layer, generic `IAddRepository<T>`/`IUnitOfWork`/`AppDbContext`) plus the plan document itself. This repo is **not yet a git repository** — `git init` still needed before any commit.

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
