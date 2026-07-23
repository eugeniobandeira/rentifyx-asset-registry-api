# ADR-AR-009: DynamoDB Single-Table Design

- **Date:** 2026-07-23
- **Status:** Accepted

## Context

E-04/F-10 needed to implement `IAssetRepository`/`ICategoryRepository` against real DynamoDB. The
plan calls for "single-table design" without specifying the key schema, and `rentifyx-identity-api`
(sibling repo) already has a working precedent (its own ADR-005: single-table via `IDynamoDBContext`
+ custom POCO mappers + GSI-only queries, no `Scan`). This repo needed to decide its own key/GSI
layout, matched to `AssetEntity`/`CategoryEntity`'s actual query patterns: get-by-id, get-by-owner,
search-by-category, search-by-status, get-by-idempotency-key, list-all-categories.

## Options Considered

- **Option A — One table per aggregate** (`Assets` table, `Categories` table, `Outbox` table).
  Simpler mental model, but each table needs its own GSIs and doesn't share the identity-api
  precedent this repo's plan explicitly says to mirror.
- **Option B — Single table, generic `PK`/`SK`/`GSI1..4PK`/`GSI1..4SK` attribute names shared
  across item types**, each item type using a different subset of the 4 GSIs. Matches
  identity-api's ADR-005 exactly.

## Decision

**Option B.** One table (`AssetRegistry`, name configurable via `DynamoDbOptions`). Key schema:

| Item type | PK | SK | GSI1 | GSI2 | GSI3 | GSI4 |
|---|---|---|---|---|---|---|
| Asset | `ASSET#{id}` | `ASSET#{id}` | `OWNER#{ownerId}` / `ASSET#{createdAt:o}#{id}` | `CATEGORY#{categoryId}` / `ASSET#{createdAt:o}#{id}` | `IDEMPOTENCY#{key}` / `ASSET#{id}` | `STATUS#{status}` / `ASSET#{createdAt:o}#{id}` |
| Category | `CATEGORY#{id}` | `CATEGORY#{id}` | `CATEGORY_LIST` / `CATEGORY#{depth:D2}#{name}#{id}` | — | — | — |
| Outbox | `OUTBOX#{id}` | `OUTBOX#{id}` | `OUTBOX_STATUS#{status}` / `{createdAtUtc:o}#{id}` | — | — | — |

Category and Outbox reuse GSI1's attribute *names* for unrelated partitions — legal in DynamoDB (a
GSI is just an index over whatever attributes happen to be present on an item), avoids a 5th index.
All 4 GSIs project `ALL`. Domain stays framework-free: each aggregate maps to a plain POCO `*Item`
class (`AssetItem`/`CategoryItem`/`OutboxItem` in `Infrastructure/Persistence/Items/`) via hand-written
static mappers (`AssetDynamoDbMapper`/`CategoryDynamoDbMapper`/`OutboxDynamoDbMapper`) — never
attribute-driven auto-mapping, since VOs like `Money`/`Media`/`AssetTitle` don't map 1:1 to scalar
item properties. `IDynamoDBContext` is used for simple single-item paths (`GetByIdAsync`,
non-transactional `SaveAsync`); low-level `IAmazonDynamoDB` is used for GSI `QueryAsync`, soft-delete
`UpdateItemAsync`, and the transactional entity+outbox write. Every query goes through a GSI —
`Scan` is never used, matching identity-api's own constraint.

## Consequences

- Adding a new query pattern later (e.g. full-text search) means adding a 5th GSI or migrating to
  OpenSearch (already tracked as a watch item once the catalog passes ~10k assets) — the single-table
  design doesn't preclude either, but a 5th GSI needs a fresh review of whether the generic attribute
  names still avoid collisions across the 3 item types.
- `AssetStatus` is always persisted as its string name (never the numeric enum value), consistent
  with CLAUDE.md's enum-persistence rule — required here because `GSI4PK` embeds the status name
  directly (`STATUS#Active`), so a numeric value would break the GSI's readability entirely.
- Category re-parenting (`CategoryEntity.ReParent`, leaf-only per ADR-AR-006) doesn't cascade any
  DynamoDB key changes beyond the single item being re-parented, since `GSI1SK`'s `{depth:D2}` prefix
  is recomputed from the entity's own `Depth` at save time — no batch key rewrite needed for the
  cases this repo currently supports (leaf-only re-parent, no subtree moves).
