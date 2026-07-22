# ADR-006: Admin-Only Category Creation/Mutation

- **Date:** 2026-07-22
- **Status:** Accepted

## Context

Anyone could, in principle, create or rename categories if the endpoints were open. The plan requires category taxonomy to stay curated (US-013), not grow unmoderated as owners create listings.

## Options Considered

- **Option A — Open category creation** — any authenticated owner could add a category while creating an asset. Simplest, but leads to duplicate/near-duplicate categories ("Electronics" vs "electronics" vs "Electronic Devices") with no curation.
- **Option B — Admin-only creation/mutation** — only admins can `CreateCategory`/`UpdateCategory`; everyone can `ListCategories` (public read). Owners pick from an existing, curated list when creating assets.

## Decision

Option B. `CreateCategoryHandler`/`UpdateCategoryHandler` check a caller-supplied `IsAdmin` flag on the request and return `Error.Forbidden` (`Category.NotAdmin`) if it's `false`, before touching the repository. `ListCategoriesHandler` has no such gate.

**Scope note on this pass:** `IsAdmin` is a plain bool on the request, not yet derived from a JWT `role` claim — no claims-extraction wiring exists in the Application layer at this point in the build (same gap noted in F-05/F-06 for `OwnerId`). Wiring real role-based authorization (extracting `IsAdmin` from the validated JWT and injecting it into the request before the handler runs) is E-05 (API Layer & Security) work — this ADR covers the *domain rule* (mutation is admin-gated), not yet its enforcement mechanism.

**Re-parenting is restricted to leaf categories** (no children) for this pass — see `.specs/features/e03-f07-categorization/spec.md`'s Out of Scope section for the full rationale (moving a subtree would require recomputing every descendant's `Depth` recursively, deferred until real usage shows it's needed).

## Consequences

- Owners creating assets choose from a fixed, admin-curated category list — no ability to introduce ad-hoc categories through the asset-creation flow.
- Until E-05 wires real JWT-claims extraction, `IsAdmin` must be supplied by whatever calls these handlers (tests, or a future API layer stub) — there's no enforcement against a caller lying about it yet. This is an accepted, temporary gap tracked for E-05.
- A leaf-only re-parent restriction means moving a category with existing children requires manually re-parenting each child first (or waiting for a future full-cascade re-parent feature) — a known UX limitation, not a bug.
