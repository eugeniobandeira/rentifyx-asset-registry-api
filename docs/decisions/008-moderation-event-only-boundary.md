# ADR-AR-008: Moderation — Event-Only Boundary with `rentifyx-ai-services`

- **Date:** 2026-07-22
- **Status:** Accepted

## Context

`rentifyx-ai-services` (a sibling repo) owns image moderation via its E-02 Moderation Lambda: it scans uploaded media with Rekognition and publishes verdicts to Kafka. This repo (`rentifyx-asset-registry-api`) owns the asset lifecycle and must react to those verdicts — but the two services are deployed, versioned, and released independently, with `rentifyx-ai-services` never exposing a synchronous API (per its own ADR-AI-001/002).

Three coupling questions had to be settled before F-09 (moderation workflow, US-015) could be built:

1. Should this repo take a package/assembly dependency on `rentifyx-ai-services` to consume its `Verdict` enum and event types directly, or define its own?
2. Which of the two events `rentifyx-ai-services` publishes (`AssetMediaModerated` and `AssetPendingManualReview`) does this repo actually need to consume?
3. Where does the human-review path (admin override) get its trigger from — the `AssetPendingManualReview` event, or a separate admin-initiated action?

This ADR was referenced by name in `.specs/features/e03-f09-moderation-workflow/spec.md`, `.specs/project/STATE.md`, and `.specs/project/ROADMAP.md` as the rationale for decisions already implemented in F-09, but the ADR document itself was never written — this fills that gap after the fact, matching what F-09 actually shipped.

## Options Considered

**Coupling (Q1):**
- **Option A — Take a NuGet/package dependency on `rentifyx-ai-services`'s `Shared` library**, reusing its `Verdict` enum and event records directly. Zero duplication, but couples this repo's build/deploy to `rentifyx-ai-services`'s release cadence and internal namespace — exactly what both repos' independent-deploy ADRs (this repo's plan; `rentifyx-ai-services` ADR-AI-001) exist to avoid.
- **Option B — Define a local `ModerationVerdict` enum mirroring the three verdict values**, consumed by field-shape (Kafka event → local DTO), no assembly reference at all.

**Event consumption scope (Q2):**
- **Option A — Consume both `AssetMediaModerated` and `AssetPendingManualReview`.** More complete, but `AssetMediaModerated`'s `Verdict` already carries `PendingReview` as one of its three values — consuming both events for the same state transition is redundant, and the plan's T-076 only names `AssetMediaModerated`.
- **Option B — Consume only `AssetMediaModerated`.** `AssetPendingManualReview` is `rentifyx-ai-services`'s own signal to feed its SQS review queue/CloudWatch alarm (see its ADR-AI-004) — an operational concern for that service's review backlog, not a state this repo's asset lifecycle needs a second copy of.

**Admin override trigger (Q3):**
- **Option A — Admin review action is triggered by consuming `AssetPendingManualReview`.** Would require this repo to hold a queue/notification of pending items sourced from that event.
- **Option B — Admin override (`AdminReviewAsset`) is a standalone, directly-callable action** (once E-05 wires real endpoints), independent of any event consumption — an admin looks at assets sitting in `PendingModeration` status and decides, rather than the system pushing them from an event.

## Decision

**Q1: Option B.** No package dependency on `rentifyx-ai-services`. `Domain/Enums/ModerationVerdict.cs` mirrors `{Approved, PendingReview, Rejected}` locally. The two enums are decoupled by design — if `rentifyx-ai-services` adds a fourth verdict value someday, this repo's Kafka consumer (E-04, not yet built) will need an explicit mapping decision at that point, not an automatic pass-through.

**Q2: Option B.** Only `AssetMediaModerated` is consumed (via the future E-04 `IHostedService` per plan T-076). `AssetPendingManualReview` is explicitly out of scope for this repo — confirmed by F-09's spec.md Out of Scope table.

**Q3: Option B.** `AdminReviewAsset` (`AdminReviewAssetRequest(AssetId, Approve, IsAdmin, Reason?)`) is a standalone override, gated on caller-supplied `IsAdmin` (same temporary pattern as Categories/F-07, pending E-05's real JWT-claims wiring) and on the asset being in `PendingModeration`. It is not populated from or triggered by any event payload.

**State machine implemented (`ApplyModerationVerdictHandler`):** verdict `Approved` → `AssetEntity.Publish()` (→ `Active`, raises `AssetPublished`); `Rejected` → `AssetEntity.Archive()` (→ `Archived`, terminal) — matches `rentifyx-ai-services`' own ADR-AI-004, which treats `Approved`/`Rejected` as immediate decisions requiring no human in the loop, only `PendingReview` needs one; `PendingReview` → no state change, held in `PendingModeration`, logged only, awaiting `AdminReviewAsset`. Applying a verdict to an asset not in `PendingModeration` is a no-op (idempotent replay safety, per F-09's Goals).

`AdminReviewAssetHandler` mirrors this: `Approve` → `Publish()`; reject → `Archive()` — an admin's decision on a `PendingReview` case is as final as an automated `Rejected` verdict, so it gets the same terminal treatment rather than leaving the asset in `PendingModeration` indefinitely.

## Consequences

- `rentifyx-ai-services` can add new fields to `AssetMediaModerated` (e.g. a future `SchemaVersion` bump) without breaking this repo — this repo only reads `AssetId`/`Verdict` today, and any additive field is simply ignored by the local DTO's shape-based deserialization.
- If `rentifyx-ai-services` ever needs this repo to react to `AssetPendingManualReview` (e.g. surfacing review-queue depth in an admin UI), that is new scope requiring its own spec — not something this ADR's decision quietly already covers.
- The Kafka consumer that actually invokes `ApplyModerationVerdictHandler` doesn't exist yet (E-04) — this ADR's decisions apply to the Application-layer contract now, and constrain how that consumer must be built later (deserialize by field name into the local `ModerationVerdict`, never take a package reference to `rentifyx-ai-services`).
- Cross-repo alignment note (not yet code, tracked for E-04): both repos have independently converged on the S3 key convention `assets/{ownerId}/{assetId}/{filename}` — `rentifyx-ai-services`'s E-02 design assumes it, and this repo's `RequestMediaUploadHandlerTests` mock uses the same shape. Neither repo has a real `S3MediaStorageService`/S3 trigger implementation yet (both deferred to this repo's E-04), so this remains an assumption to confirm when that work starts, not a verified contract.
