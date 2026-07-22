# Moderation Workflow (F-09 / US-015) Specification

## Problem Statement

Assets must pass automated (and occasionally manual) moderation before becoming publicly visible.
Today `AssetEntity` already models the `Draft → PendingModeration → Active` lifecycle
(`SubmitForModeration()`/`Publish()`, from E-02), but no Application-layer use case exists to
trigger the owner-initiated transition, to consume the moderation verdict from
`rentifyx-ai-services`, or for an admin to override a stuck manual-review case.

**External contract (verified against `rentifyx-ai-services` repo, not guessed):**
`rentifyx-ai-services` publishes `AssetMediaModerated(AssetId, Verdict, Labels, TopConfidence,
Timestamp, SchemaVersion)` to Kafka, where `Verdict` ∈ `{Approved, PendingReview, Rejected}`
(confidence thresholds per its ADR-AI-003: ≥90% auto-reject, 60–90% manual review, <60%
auto-approve). This repo does not take a package dependency on `rentifyx-ai-services` — per
ADR-AR-008 (event-only boundary), we define our own local `ModerationVerdict` enum mirroring the
three values, decoupled from the publisher's assembly.

**Scope boundary (matches F-05..F-08 precedent):** the actual Kafka consumer (`IHostedService`
per plan's T-076) is Infrastructure/E-04 — not built yet, same as the DynamoDB repository and S3
storage adapter. This feature delivers the Application-layer handler that consumer will call once
it exists (`ApplyModerationVerdict`), plus the owner-initiated submission and the admin override,
both directly callable once endpoints land in E-05.

## Goals

- [ ] Owner can submit a `Draft` asset for moderation (`SubmitForModeration`)
- [ ] A moderation verdict (from the future Kafka consumer) transitions the asset per plan T-077/T-078: `Approved` → `Active` (+`AssetPublished`); `Rejected`/`PendingReview` → held in `PendingModeration`, no state change
- [ ] Admin can override a `PendingModeration` asset — approve (→ `Active`) or reject (stays `PendingModeration`, reason logged for audit) — same caller-supplied `IsAdmin` temporary-gap pattern as Categories (F-07) pending E-05's real JWT-claims wiring
- [ ] Verdict consumption is idempotent — replaying the same event twice must not double-publish or error

## Out of Scope

| Feature | Reason |
|---|---|
| Kafka consumer `IHostedService` itself | Infrastructure concern, E-04 — this spec only defines the Application contract it will call |
| `Rejected`/`ManualReview` as a new `AssetStatus` | Plan is explicit: reject/manual-review "held in PendingModeration, admin notified" — no new domain status |
| A "revert to Draft" transition after rejection | Not in plan scope; owner has no resubmission path in this pass — noted as a gap below |
| Admin notification delivery (email/Slack/etc.) | Not specified by plan beyond "admin notified" — this pass only logs, no notification channel exists |
| `AssetPendingManualReview` event consumption | Separate event from `rentifyx-ai-services` (feeds an admin review queue/UI); plan's T-076 only names `AssetMediaModerated`, whose `Verdict` already carries `PendingReview` — redundant to consume both for the state machine |
| `AssetEnrichmentSuggested`/`AssetDuplicateSuspected` | Different `rentifyx-ai-services` events, no relation to moderation |

---

## User Stories

### P1: Owner submits asset for moderation ⭐ MVP

**User Story**: As an asset owner, I want to submit my draft asset for moderation so that it can
become publicly visible.

**Why P1**: Entry point to the whole moderation flow — nothing downstream works without it.

**Acceptance Criteria**:

1. WHEN the owner submits a `Draft` asset they own THEN system SHALL transition it to `PendingModeration`
2. WHEN a non-owner attempts to submit THEN system SHALL return `Forbidden` (`Asset.NotOwner`), matching `ConfirmMediaUpload`'s existing check
3. WHEN the asset does not exist THEN system SHALL return `NotFound`
4. WHEN the asset is not in `Draft` status THEN system SHALL return a validation/conflict error, not throw or silently no-op

**Independent Test**: Create a Draft asset, submit as owner, assert status is `PendingModeration`.

---

### P1: Apply moderation verdict from rentifyx-ai-services ⭐ MVP

**User Story**: As the system, I want to apply the moderation verdict from `rentifyx-ai-services`
so that approved assets go live without manual intervention.

**Why P1**: Core automation — without it every asset would need manual admin review.

**Acceptance Criteria**:

1. WHEN verdict is `Approved` for a `PendingModeration` asset THEN system SHALL transition it to `Active` and raise `AssetPublished`
2. WHEN verdict is `Rejected` or `PendingReview` THEN system SHALL leave the asset in `PendingModeration` (no state change) and log the outcome
3. WHEN the asset is not currently `PendingModeration` (e.g. verdict replayed after already `Active`) THEN system SHALL no-op and return the current state, not error — this is the idempotency guarantee for at-least-once Kafka delivery
4. WHEN the asset does not exist THEN system SHALL return `NotFound`

**Independent Test**: Submit an asset for moderation, apply `Approved` verdict, assert `Active` + no error on a second identical call.

---

### P2: Admin overrides a stuck moderation case

**User Story**: As an admin, I want to manually approve or reject a `PendingModeration` asset so
that cases the automated pipeline couldn't resolve don't stay stuck forever.

**Why P2**: Necessary escape hatch for `PendingReview`/`Rejected` verdicts, but not required for the
automated happy path to work — P1s ship without it.

**Acceptance Criteria**:

1. WHEN a caller with `IsAdmin = true` approves a `PendingModeration` asset THEN system SHALL transition it to `Active` and raise `AssetPublished`
2. WHEN a caller with `IsAdmin = true` rejects a `PendingModeration` asset THEN system SHALL leave it in `PendingModeration` and log the reason (no persisted state change — no field exists to store it)
3. WHEN a caller with `IsAdmin = false` attempts either action THEN system SHALL return `Forbidden`
4. WHEN the target asset is not `PendingModeration` THEN system SHALL return a validation/conflict error — nothing to review

**Independent Test**: Submit an asset for moderation, admin-approve it, assert `Active`.

---

## Edge Cases

- WHEN `SubmitForModeration` is called on an already-`PendingModeration` asset THEN system SHALL return a conflict error (mirrors `AssetEntity.SubmitForModeration()`'s existing `InvalidOperationException` guard, translated to `ErrorOr` at the Application boundary rather than letting the exception propagate)
- WHEN `ApplyModerationVerdict` receives an unrecognized/future verdict value THEN system SHALL treat it conservatively as "no state change" (same as `Rejected`/`PendingReview`) rather than crash — forward-compatible with `rentifyx-ai-services` adding verdict values later
- WHEN `AdminReviewAsset` targets a non-existent asset THEN system SHALL return `NotFound`

## Known Gap (documented, not solved here)

Once an asset lands in `PendingModeration` with `Rejected`/manual reject, there is currently no
path back to `Draft` for the owner to fix and resubmit — it stays there until an admin approves it.
Flagged as a deferred idea, not blocking this pass (plan does not call for a revert transition).

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| MOD-01 | P1: Draft→PendingModeration transition | Application | Pending |
| MOD-02 | P1: Forbidden for non-owner submit | Application | Pending |
| MOD-03 | P1: NotFound for missing asset (submit) | Application | Pending |
| MOD-04 | P1: conflict when not Draft (submit) | Application | Pending |
| MOD-05 | P1: Approved verdict → Active + AssetPublished | Application | Pending |
| MOD-06 | P1: Rejected/PendingReview verdict → no state change | Application | Pending |
| MOD-07 | P1: idempotent verdict replay (already non-PendingModeration → no-op) | Application | Pending |
| MOD-08 | P1: NotFound for missing asset (verdict) | Application | Pending |
| MOD-09 | P2: admin approve → Active + AssetPublished | Application | Pending |
| MOD-10 | P2: admin reject → no state change, reason logged | Application | Pending |
| MOD-11 | P2: Forbidden for non-admin caller | Application | Pending |
| MOD-12 | P2: conflict when target not PendingModeration | Application | Pending |
| MOD-13 | Edge: unrecognized verdict treated as no-op | Application | Pending |

**Coverage:** 13 total, 13 mapped to tasks (implicit, execution plan below — Tasks phase skipped), 0 unmapped

---

## Success Criteria

- [ ] All three handlers (`SubmitForModeration`, `ApplyModerationVerdict`, `AdminReviewAsset`) covered by validator + handler tests (Moq), mirroring F-05..F-08 depth
- [ ] `ApplyModerationVerdict` is provably idempotent under test (replay after `Active` is a no-op, not an error)
- [ ] No new `AssetStatus` value introduced — matches plan's explicit "held in PendingModeration" design
