# E-03 · F-07 Categorization Specification

**Scope note:** third feature slice of E-03. Covers US-013 (T-064–T-068).

## Problem Statement

Admins manage the category taxonomy (create, rename, re-parent); everyone else can list categories read-only. `CategoryEntity` (E-02) only has `CreateRoot`/`CreateChild` factories — no rename or re-parent capability exists yet.

## Goals

- [ ] `CreateCategory`/`UpdateCategory` reject non-admin callers
- [ ] Renaming a category never touches its position in the tree
- [ ] Re-parenting a category is rejected if it has children (avoids the subtree-depth-cascade problem entirely — see Domain Changes below)
- [ ] `ListCategories` has no admin gate — public read

## Out of Scope

| Item | Reason |
|---|---|
| Full re-parent with subtree depth cascade | Explicit scope decision (2026-07-22): re-parenting is only allowed on leaf categories (no children). Moving a category with descendants would require recomputing every descendant's `Depth` recursively — deferred until real usage data shows it's needed |
| Real JWT-claims-based admin check | No claims-extraction wiring exists in Application layer yet (E-05 territory, same gap noted in F-05/F-06 specs). `IsAdmin` is a caller-supplied bool on the request, same shape as `OwnerId` elsewhere — E-05 will populate it from the JWT `role` claim |
| Response caching for `ListCategories` | No caching infrastructure exists yet (no `IMemoryCache`/distributed cache wiring). "cached" from the plan is an E-04/E-05 concern (e.g. a decorator around `ICategoryRepository` or API-level response caching) — not implemented in this Application-layer slice |

## Domain Changes Required (prerequisite)

`CategoryEntity` gains two methods:

- `Rename(string name)` — guards non-null/whitespace, always allowed regardless of tree position
- `ReParent(CategoryEntity newParent)` — guards: `newParent` not null, `newParent.Id != Id` (a category cannot become its own parent), `newParent.Depth < CategoryRules.MaxDepth` (same cap as `CreateChild`). Sets `ParentCategoryId`/`Depth` from `newParent`. The "no children" precondition is checked by the caller (Application layer, via `ICategoryRepository.GetAllAsync` + a scan for any category whose `ParentCategoryId` matches) **before** calling `ReParent` — same "Domain enforces what it can check locally, Application supplies pre-verified context" pattern as `CreateChild`'s live-parent-object design (ADR-AR-003).

## User Stories

### P1: Create Category ⭐ MVP

**Acceptance Criteria**:

1. WHEN `CreateCategoryRequest.IsAdmin` is `false` THEN system SHALL return `Error.Forbidden`, never touch the repository
2. WHEN `ParentCategoryId` is `null` THEN system SHALL call `CategoryEntity.CreateRoot`
3. WHEN `ParentCategoryId` is set but no such category exists THEN system SHALL return `Error.NotFound`
4. WHEN `ParentCategoryId` refers to a category at max depth THEN system SHALL propagate the Domain guard's rejection as a validation error
5. WHEN admin + valid parent (or none) THEN system SHALL save the new category

---

### P1: Update Category (Rename / Re-parent) ⭐ MVP

**Acceptance Criteria**:

1. WHEN `UpdateCategoryRequest.IsAdmin` is `false` THEN system SHALL return `Error.Forbidden`
2. WHEN the category doesn't exist THEN system SHALL return `Error.NotFound`
3. WHEN `NewName` is provided THEN system SHALL rename regardless of tree position
4. WHEN `NewParentCategoryId` is provided AND the category has at least one child THEN system SHALL return a validation error, SHALL NOT re-parent
5. WHEN `NewParentCategoryId` is provided AND the category has no children THEN system SHALL re-parent (subject to `ReParent`'s own guards: no self-parenting, depth cap)

---

### P1: List Categories ⭐ MVP

**Acceptance Criteria**:

1. WHEN `ListCategoriesRequest` is submitted (no admin gate) THEN system SHALL return all categories via `ICategoryRepository.GetAllAsync`

---

## Requirement Traceability

| ID | Requirement | Status |
|---|---|---|
| CAT-01 | `CategoryEntity.Rename`/`ReParent` | Pending |
| CAT-02 | `CreateCategoryHandler` — admin-only | Pending |
| CAT-03 | `UpdateCategoryHandler` — admin-only, leaf-only re-parent | Pending |
| CAT-04 | `ListCategoriesHandler` — public | Pending |
| CAT-05 | ADR-AR-006 (admin-only category mutation) | Pending |

## Success Criteria

- [ ] `dotnet test` — non-admin rejected, admin succeeds, re-parent-with-children rejected, re-parent-cycle (self-parent) rejected
