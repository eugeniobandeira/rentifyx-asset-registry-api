# E-03 · F-06 Media Upload Pipeline Specification

**Scope note:** second feature slice of E-03. Covers US-012 (T-058–T-063) in full.

## Problem Statement

An owner needs to upload photos for an existing asset via a secure, direct-to-S3 flow: request a presigned upload URL (validated before it's issued, not after upload), upload directly to S3 client-side, then confirm completion so the asset records the media and raises `AssetMediaUploaded`.

## Goals

- [ ] File size/MIME type are validated **before** a presigned URL is ever generated (ADR-AR-005 — ties back to the plan's explicit requirement, not a general best-practice restatement)
- [ ] The presigned URL is scoped to a specific S3 object key derived from owner + asset (not a caller-supplied key, preventing path traversal/overwrite of other assets' media)
- [ ] `ConfirmMediaUpload` attaches the media to the asset and raises `AssetMediaUploaded` only after confirmation — never optimistically on request

## Out of Scope

| Item | Reason |
|---|---|
| Actual S3 client / presigned URL generation logic | `IMediaStorageService` implementation is E-04 (Infrastructure); this slice only defines/extends the contract and consumes it via a test double |
| Ownership authorization via JWT claims | No claims-extraction wiring exists yet in Application layer (E-05 territory); this slice checks ownership by comparing `AssetEntity.OwnerId` to a caller-supplied `OwnerId` on the request, same shape as F-05's `CreateAssetRequest` |
| Multiple media items per confirm call | One `ConfirmMediaUpload` = one media item, matching `AssetEntity.AttachMedia(Media)`'s single-item signature |

## Domain Changes Required (prerequisite)

`IMediaStorageService.GeneratePresignedUploadUrlAsync` currently only accepts `(mimeType, sizeBytes)` — it can't scope the S3 key to owner+asset because it doesn't receive them. Extending it:

```csharp
Task<PresignedUploadUrl> GeneratePresignedUploadUrlAsync(
    Guid ownerId,
    Guid assetId,
    string mimeType,
    long sizeBytes,
    CancellationToken ct = default);
```

New `PresignedUploadUrl(string Url, string S3Key)` record in `Domain/Interfaces/Media/` — the caller (`RequestMediaUploadHandler`) needs the `S3Key` to hand back to the client, which the client then supplies again on `ConfirmMediaUpload`.

## User Stories

### P1: Request Media Upload ⭐ MVP

**User Story**: As an owner, I want to request a presigned upload URL for my asset's media, validated up front so I never waste an upload on a rejected file.

**Acceptance Criteria**:

1. WHEN `RequestMediaUploadRequest` (AssetId, OwnerId, MimeType, SizeBytes) is submitted THEN system SHALL validate `MimeType` against `ValidationConstants.MediaRules.AllowedMimeTypes` and `SizeBytes` bounds BEFORE calling `IMediaStorageService`
2. WHEN the asset doesn't exist THEN system SHALL return `Error.NotFound`
3. WHEN the asset exists but `OwnerId` doesn't match `AssetEntity.OwnerId` THEN system SHALL return `Error.Forbidden`
4. WHEN validation and ownership pass THEN system SHALL call `IMediaStorageService.GeneratePresignedUploadUrlAsync(ownerId, assetId, mimeType, sizeBytes)` and return the URL + S3 key to the caller
5. WHEN `SizeBytes` is non-positive or `MimeType` isn't in the allowed set THEN system SHALL reject with a validation error, never reaching `IMediaStorageService`

**Independent Test**: Mock `IAssetRepository`/`IMediaStorageService`; assert oversized/disallowed-MIME requests never call `GeneratePresignedUploadUrlAsync`.

---

### P1: Confirm Media Upload ⭐ MVP

**User Story**: As an owner, I want to confirm my upload completed so the asset records the media.

**Acceptance Criteria**:

1. WHEN `ConfirmMediaUploadRequest` (AssetId, OwnerId, S3Key, MimeType, SizeBytes) is submitted THEN system SHALL re-validate MIME/size the same way as request-time (client-controlled fields, never trusted blindly on confirm)
2. WHEN the asset doesn't exist THEN system SHALL return `Error.NotFound`
3. WHEN `OwnerId` doesn't match THEN system SHALL return `Error.Forbidden`
4. WHEN validation and ownership pass THEN system SHALL construct a `Media` VO, call `AssetEntity.AttachMedia(media)` (raises `AssetMediaUploaded`), then `IAssetRepository.SaveAsync`

**Independent Test**: Mock repository; assert `AttachMedia`'s resulting `AssetMediaUploaded` event carries the right `S3Key`, and `SaveAsync` is called exactly once.

---

## Requirement Traceability

| ID | Requirement | Status |
|---|---|---|
| MU-01 | `IMediaStorageService` extended with ownerId/assetId scoping + `PresignedUploadUrl` | Pending |
| MU-02 | `RequestMediaUploadHandler` — pre-validation before presigned URL | Pending |
| MU-03 | `ConfirmMediaUploadHandler` — attaches media, raises `AssetMediaUploaded` | Pending |
| MU-04 | ADR-AR-005 (pre-validation before presigned URL vs. post-upload) | Pending |

## Success Criteria

- [ ] `dotnet test` — new Handler tests (Moq) covering oversized/disallowed-MIME rejection and the valid flow, per T-063
- [ ] Neither handler calls `IMediaStorageService`/`SaveAsync` when validation or ownership fails
