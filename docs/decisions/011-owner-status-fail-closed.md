# ADR-AR-011: Owner-Status Cache — Fail-Closed on Missing/Stale Entry

- **Date:** 2026-07-24
- **Status:** Accepted

## Context

`CreateAssetHandler` (F-05) already calls `IOwnerStatusValidator.IsOwnerActiveAsync(ownerId)` before allowing
asset creation, but no implementation of that interface existed until F-12: the plan's local owner-status
cache, synced from `identity-api`'s `user-lifecycle-events` Kafka topic (`UserSuspended`/`UserAccountDeleted`),
had not been built. F-12 closes that gap with `DynamoDbOwnerStatusValidator` and `OwnerStatusConsumer`.

The open question (tracked since the 2026-07-22 E-04 kickoff session, plan's own recommendation) was what
`IsOwnerActiveAsync` should return when the cache has **no entry** for an owner — either because the owner
predates this feature (cache starts empty on first deploy), or because the sync event hasn't arrived yet
(replication lag, consumer restart, etc.).

## Options Considered

- **Fail-open** — treat a missing cache entry as active, only reject when an explicit `IsActive=false` entry
  exists. Never blocks legitimate owners on cache lag, but a suspended/deleted owner can create assets for
  as long as the event takes to arrive and be processed — a real window given Kafka's at-least-once,
  eventually-consistent delivery.
- **Fail-closed** — treat a missing cache entry as *not verified active*, reject asset creation until a
  cache entry exists. Never lets a suspended/deleted owner slip through, at the cost of rejecting every
  `CreateAsset` call from an owner the cache hasn't heard about yet (including, on first deploy, literally
  every owner — see Consequences).

## Decision

**Fail-closed.** Confirmed explicitly with the user (2026-07-24 session) rather than assumed — this was the
plan's own recommendation, citing LGPD/marketplace-trust considerations over availability. Implemented in
`DynamoDbOwnerStatusValidator.IsOwnerActiveAsync`: `GetItemAsync`/`LoadAsync` returning no item → `return
false`, same as an explicit `IsActive=false` entry.

## Consequences

- **Cold-start rejection window.** On first deploy of this feature, the owner-status cache starts empty —
  every `CreateAsset` call is rejected until `identity-api` republishes a lifecycle event for that owner (or
  until a backfill mechanism populates the cache). This is a real, user-facing regression risk if shipped
  without a backfill plan, not a theoretical edge case. **Not solved by F-12 itself** — F-12 only builds the
  live event consumer, not a historical snapshot loader. Tracked as an open item (see STATE.md).
- No `UserReactivated`/equivalent event exists in `identity-api` per the plan — once an owner's cache entry
  is flipped to `IsActive=false`, `OwnerStatusConsumer` never flips it back. This is intentional (matches
  what `identity-api` actually publishes), not a gap in this repo's consumer logic.
- This decision only governs `CreateAssetHandler`'s check today. Any future handler that also needs
  owner-activity gating inherits the same fail-closed default by depending on the same
  `IOwnerStatusValidator` — a deliberate default, not something each caller re-decides.
