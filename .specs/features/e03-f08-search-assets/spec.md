# Search & Discovery (F-08 / US-014) Specification

## Problem Statement

Buyers/renters browsing the marketplace need to find assets by category, price range, and
keyword, with paginated results. Today no query surface exists — `GetByIdAsync` and
`GetByOwnerAsync` are the only asset reads. Domain contracts for search already exist
(`AssetSearchFilter`, `ISearchRepository<T,TFilter>`, `IAssetRepository.SearchAsync` from E-02);
this feature adds the Application-layer use case and endpoint on top of them.

**Contract correction made in this pass:** `AssetSearchFilter`/`ISearchRepository` originally
modeled offset pagination (`Page`/`PageSize` + `PagedResult<T>.Total`), which doesn't map to
DynamoDB — there's no native "skip N items", and computing a total match count would require a
full table scan. Reworked to cursor pagination before building the handler on top of it:
`AssetSearchFilter` now takes `PageSize` + `NextPageToken` (opaque string), and
`ISearchRepository<T,TFilter>.SearchAsync` returns a new `CursorPagedResult<T>(Items,
NextPageToken)` instead of the generic `PagedResult<T>` (which stays as-is for the EF-shaped
`IGetAllRepository<T,TFilter>`, unrelated to this feature).

## Goals

- [ ] Public `SearchAssets` use case returning paginated, filtered results (category, price range, keyword)
- [ ] Only `Active` assets are searchable — draft/pending/suspended/archived assets never leak into public search results
- [ ] Pagination is bounded (page size capped) to protect the DynamoDB `contains`-filter query path (ADR-AR-007 watch item)

## Out of Scope

| Feature | Reason |
|---|---|
| OpenSearch/ElasticSearch | Deferred past ~10k assets, DEF-001 |
| Full-text relevance ranking / fuzzy match | `Keyword` is a simple substring/contains filter on title, not a search-engine feature |
| Result caching | No cache infra exists yet, same deferral as `ListCategories` (F-07) |
| Sort options (price asc/desc, newest) | Not in original US-014 scope; filter+paginate only |
| DynamoDB GSI/table design itself | Infrastructure concern, E-04 — this spec only defines the Application contract the future `DynamoDbAssetRepository.SearchAsync` must satisfy |

---

## User Stories

### P1: Browse assets by category and price ⭐ MVP

**User Story**: As a marketplace visitor, I want to filter assets by category and price range so
that I can find listings relevant to my budget and interest.

**Why P1**: Core discovery path — without this, published assets are unreachable except by direct ID.

**Acceptance Criteria**:

1. WHEN a caller submits `categoryId` THEN system SHALL return only `Active` assets with that `CategoryId`
2. WHEN a caller submits `minPrice`/`maxPrice` THEN system SHALL return only `Active` assets whose `Price.Amount` falls within `[minPrice, maxPrice]` inclusive
3. WHEN `minPrice > maxPrice` THEN system SHALL return a validation error, not call the repository
4. WHEN no filters are supplied THEN system SHALL return all `Active` assets, paginated
5. WHEN `pageSize` is supplied THEN system SHALL return up to that many items plus a `NextPageToken` (opaque cursor) when more results exist, `null` when the result set is exhausted
6. WHEN a caller submits a previously returned `NextPageToken` THEN system SHALL resume from that cursor, applying the same filters

**Independent Test**: Seed assets across two categories/price bands, call `SearchAssets` with each filter combination, assert only matching `Active` assets are returned.

---

### P2: Keyword search on title

**User Story**: As a marketplace visitor, I want to search assets by keyword so that I can find
listings without knowing the exact category.

**Why P2**: Valuable but combinable with P1 filters rather than a blocking dependency; MVP ships without it if time-boxed, but included in this pass per current AssetSearchFilter contract.

**Acceptance Criteria**:

1. WHEN a caller submits `keyword` THEN system SHALL return only `Active` assets whose `Title` contains the keyword (case-insensitive substring match)
2. WHEN `keyword` is combined with `categoryId`/price filters THEN system SHALL apply all filters together (AND semantics)
3. WHEN `keyword` is empty/whitespace-only THEN system SHALL treat it as not supplied (no filter applied), not as a zero-match filter

**Independent Test**: Seed assets with distinct titles, search by a substring of one title, assert only that asset matches.

---

## Edge Cases

- WHEN `pageSize` exceeds the max allowed (30) THEN system SHALL return a validation error, not silently clamp — protects the DynamoDB `contains`-filter scan cost (ADR-AR-007 watch item)
- WHEN `pageSize` < 1 THEN system SHALL return a validation error
- WHEN `categoryId` references a non-existent category THEN system SHALL return an empty result set, not an error (no assets can match a category with zero members)
- WHEN `minPrice` or `maxPrice` is negative THEN system SHALL return a validation error (mirrors `Money` VO's non-negative invariant)
- WHEN no assets match any filter combination THEN system SHALL return an empty `Items` list with `NextPageToken = null`, not an error
- WHEN `NextPageToken` is present but empty/whitespace THEN system SHALL treat it as not supplied (start from the beginning) — token *format*/tamper validation is an Infrastructure concern (E-04, opaque to Application layer)

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| SRCH-01 | P1: category filter | Application | Pending |
| SRCH-02 | P1: price range filter | Application | Pending |
| SRCH-03 | P1: min>max validation | Application | Pending |
| SRCH-04 | P1: unfiltered pagination | Application | Pending |
| SRCH-05 | P1: pageSize + NextPageToken slicing | Application | Pending |
| SRCH-06 | P1: resume from NextPageToken cursor | Application | Pending |
| SRCH-07 | P2: keyword substring match | Application | Pending |
| SRCH-08 | P2: combined AND filters | Application | Pending |
| SRCH-09 | P2: blank keyword ignored | Application | Pending |
| SRCH-10 | Edge: pageSize cap (30) rejected, not clamped | Application | Pending |
| SRCH-11 | Edge: pageSize lower-bound validation | Application | Pending |
| SRCH-12 | Edge: negative price validation | Application | Pending |
| SRCH-13 | Edge: blank NextPageToken treated as absent | Application | Pending |
| SRCH-14 | Public access — only Active assets returned regardless of caller | Application | Pending |

**Coverage:** 14 total, 14 mapped to tasks (implicit, ≤5 files — Tasks phase skipped), 0 unmapped. Status: Verified (2026-07-22, 12 validator tests + 5 handler tests, all green).

---

## Success Criteria

- [ ] `SearchAssets` never returns non-`Active` assets under any filter combination
- [ ] Validator rejects out-of-bound `page`/`pageSize`/`minPrice`>`maxPrice`/negative price before repository is called
- [ ] All P1+P2 acceptance criteria covered by validator + handler tests (Moq), mirroring F-05/F-06/F-07 test depth
