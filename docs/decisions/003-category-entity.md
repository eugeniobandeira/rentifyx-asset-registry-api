# ADR-003: Category as a First-Class Entity

- **Date:** 2026-07-22
- **Status:** Accepted

## Context

`AssetEntity.CategoryId` needs a taxonomy to point at. The plan calls for admin-managed, nested categories (e.g. `Electronics > Phones > Accessories`), not a flat product-type label.

## Options Considered

- **Option A — Enum `AssetCategory`** — zero storage cost, compile-time safety, but every new/renamed category requires a code change + redeploy; can't express nesting or admin-driven taxonomy at all.
- **Option B — First-class `CategoryEntity` with `ParentCategoryId`, admin-managed via its own repository** — categories are data, not code; supports nesting; matches ADR-AR-006 (category creation/mutation is admin-only, via API — not a deploy).
- **Option C — Flat string tag with no hierarchy** — simplest, but the plan explicitly asks for nested taxonomy (`ParentCategoryId`); a flat tag can't express "Phones is under Electronics."

## Decision

Option B. `CategoryEntity` is a first-class aggregate (`Id`, `Name`, `ParentCategoryId`, `Depth`), created via `CreateRoot(name)` (depth 1) or `CreateChild(name, parent)` (depth = `parent.Depth + 1`).

**Depth is capped at 3** (`ValidationConstants.CategoryRules.MaxDepth`) — three levels (e.g. `Electronics > Phones > Accessories`) covers the marketplace taxonomies this system targets without the UI/search complexity of unbounded nesting.

**Cycle prevention by construction, not by validation:** `CreateChild` takes the live parent `CategoryEntity` object, not a bare `ParentCategoryId` GUID. This means:
- Depth can be computed and capped with **zero I/O in the Domain layer** — no repository call needed inside `CreateChild` to walk the ancestor chain.
- A cycle (a category becoming its own ancestor) is structurally impossible: you can only build a child of a category that already exists as a constructed object in memory, so there's no code path that lets a not-yet-created category reference itself or a not-yet-created descendant.
- The tradeoff: the caller (Application layer, E-03) is responsible for loading the parent entity via `ICategoryRepository` before calling `CreateChild` — Domain enforces the invariant, Application supplies the data.

## Consequences

- E-03's `CreateCategory` handler must fetch the parent via `ICategoryRepository.GetByIdAsync` before calling `CategoryEntity.CreateChild` — this is a required step, not optional, or the depth guard can't run.
- Renaming/re-parenting a category (ADR-AR-006, admin-only) is a data operation via the API, no redeploy needed.
- If the 3-level cap proves too shallow for a future catalog, raising `MaxDepth` is a one-constant change plus a migration of existing category depths — no structural rework of `CategoryEntity` itself.
