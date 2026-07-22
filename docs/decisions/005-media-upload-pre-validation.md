# ADR-005: Validate Media Before Generating a Presigned Upload URL

- **Date:** 2026-07-22
- **Status:** Accepted

## Context

Owners upload asset photos directly to S3 via a presigned URL (no proxying through this API). The validation of file size/MIME type could happen either before the presigned URL is issued (reject the request outright) or after the upload completes (accept anything, reject on confirm).

## Options Considered

- **Option A — Validate after upload** — simpler to implement (nothing to check before generating the URL), but wastes bandwidth/time on an upload that gets rejected anyway, and briefly puts an invalid/oversized file in S3 before cleanup.
- **Option B — Validate before generating the presigned URL** — `RequestMediaUploadHandler` checks `MimeType` against `ValidationConstants.MediaRules.AllowedMimeTypes` and `SizeBytes > 0` before ever calling `IMediaStorageService.GeneratePresignedUploadUrlAsync`. An invalid request never gets a URL, so no wasted upload ever happens.

## Decision

Option B, per the plan's explicit requirement (T-059). `RequestMediaUploadValidator` runs the same MIME/size checks `Media.Create` would apply, before the handler reaches `IMediaStorageService`. `ConfirmMediaUploadValidator` re-runs the identical checks — client-supplied `MimeType`/`SizeBytes` on confirm are never trusted blindly just because a URL was issued earlier (the client could lie about what it actually uploaded).

## Consequences

- No S3 storage cost/cleanup burden for rejected uploads — they never get a URL to upload against.
- Both `RequestMediaUploadHandler` and `ConfirmMediaUploadHandler` duplicate the same MIME/size validation rules (once per handler's validator) — an accepted tradeoff for defense-in-depth over the client-controlled confirm payload, rather than trusting the request-time check alone.
- E-04's `S3MediaStorageService` (real implementation) still needs its own bucket-policy-level enforcement (ADR-AR-011 territory, ADR-AR-005 here only covers the Application-layer check) since a malicious client could bypass this API and call S3 directly with a stolen presigned URL — that's out of scope for this Application-layer decision.
