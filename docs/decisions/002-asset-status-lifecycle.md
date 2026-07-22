# ADR-002: Asset Status Lifecycle

- **Date:** 2026-07-22
- **Status:** Accepted

## Context

`AssetEntity` needs a status field driving what an owner/admin can do with an asset and what's visible in search. A naive model has only two states (unpublished/published). The plan requires moderation (via `rentifyx-ai-services`) between an owner submitting an asset and it going live, plus the ability to suspend a live asset and archive one permanently.

## Options Considered

- **Option A — Two states (`Draft`, `Active`)** — simplest possible model; moderation would have to be tracked out-of-band (e.g. a separate `ModerationStatus` field or table), decoupled from the asset's own lifecycle.
- **Option B — Five states (`Draft`, `PendingModeration`, `Active`, `Suspended`, `Archived`)** — moderation is a first-class status, not a side table; suspension and archival are explicit terminal-ish states with their own transition rules.
- **Option C — Boolean flags (`IsPublished`, `IsSuspended`, `IsArchived`)** — avoids an enum but allows invalid combinations (e.g. `IsPublished && IsArchived` both true) unless guarded manually everywhere.

## Decision

Option B. `AssetStatus`: `Draft → PendingModeration → Active → Suspended ⇄ Active`, and `Archived` reachable from any non-`Archived` state as a terminal sink.

`PendingModeration` is a **distinct state from `Draft`**, not a sub-flag of it, because:
- An owner can still edit a `Draft` freely (it was never submitted); once `PendingModeration`, the asset is frozen pending an external verdict from `rentifyx-ai-services` — editing during that window would invalidate the moderation result.
- Search/discovery must never show `PendingModeration` assets — if moderation were a flag on `Draft`, every query would need to check two fields (`Status == Draft && !IsSubmittedForModeration`) instead of one (`Status == Active`).
- `AssetEntity.Publish()` can only transition `PendingModeration → Active` — this makes "skip moderation" a structurally impossible call (`Draft → Active` directly throws `InvalidOperationException`), rather than a rule that has to be re-checked by every caller.

`Suspended → Active` (reinstatement) does not raise a domain event distinct from the original `AssetPublished` — the plan's outbound Kafka contract (`AssetCreated`/`AssetMediaUploaded`/`AssetPublished`/`AssetSuspended`) has no `AssetReinstated` event; this is a deliberate scope boundary for E-02, revisit in E-03 if downstream services need to distinguish first-publish from reinstatement.

`Archived` is a one-way sink: once archived, no further transition is allowed (`Archive()` throws if already `Archived`; no other method accepts `Archived` as a starting state).

## Consequences

- Every state-transition method on `AssetEntity` (`SubmitForModeration`, `Publish`, `Suspend`, `Reinstate`, `Archive`) enforces its own precondition and throws `InvalidOperationException` on violation — invalid asset states are unrepresentable, not just discouraged.
- Search/Infrastructure (E-04) can filter on a single `Status == Active` condition with no compound boolean logic.
- Adding a future status (e.g. a "Rejected" terminal state distinct from `Archived`) requires touching the enum and every transition guard that currently treats `Archived` as the only terminal sink — a known, bounded cost, not a blocker.
