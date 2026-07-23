# F-11 · S3 Media Storage (US-018)

**Plan reference:** `RentifyX_AssetRegistryAPI_Plan.md` E-04, T-092..T-095
**Epic:** E-04 Infrastructure Layer — AWS Integration (M4)
**Depends on:** `IMediaStorageService` (E-02), `RequestMediaUploadHandler`/`ConfirmMediaUploadHandler` (E-03/F-06)

## Goal

Give `RequestMediaUploadHandler`/`ConfirmMediaUploadHandler` a real S3-backed implementation of
`IMediaStorageService` so presigned-upload-URL generation works against actual (or LocalStack-emulated)
S3, closing the last unimplemented Application-layer dependency from F-06.

## Scope

In scope:
- `S3MediaStorageService : IMediaStorageService` in `02-src/05-Infrastructure`, real presigned URL
  generation via `AWSSDK.S3`.
- S3 key convention `assets/{ownerId}/{assetId}/{filename}` (confirmed cross-repo against
  `rentifyx-ai-services`' `AssetKeyConventionFilter.cs` — see STATE.md G-001).
- `ValidateUploadAsync` — confirms an object actually exists at the expected key/size/content-type
  in the bucket (`HeadObject`), used defensively by callers that need to verify an upload really
  landed (not currently called by `ConfirmMediaUploadHandler`, which trusts the client-confirmed
  payload per ADR-AR-005 — wiring that in is out of scope here, `ConfirmMediaUploadHandler` is
  explicitly not to be touched by this feature per the parent task's instructions).
- `MediaStorageOptions` bound via `IOptions<T>` (bucket name, presigned URL expiry) — fits CLAUDE.md's
  stated criterion exactly: `S3MediaStorageService` is a DI-constructed scoped/singleton service.
- DI registration of `IAmazonS3` + `S3MediaStorageService` in `InfrastructureDependencyInjection`.
- `Testcontainers.LocalStack` integration test: presigned URL generation + actual PUT upload against
  the emulator + `ValidateUploadAsync` confirms it.

Out of scope (deferred, not this pass):
- **Terraform authoring** (T-092 bucket+CORS, T-093 bucket policy, T-094 lifecycle policy). `iac/`
  currently contains only a `README.md` — no module scaffolding exists yet anywhere in the repo, and
  the plan's own M6 (E-06) is titled "Terraform & Kubernetes" and is where DynamoDB/S3/KMS Terraform
  modules are scoped. Authoring a single S3 module now, out of order and disconnected from the DynamoDB/
  KMS modules it will need to share provider/backend config with, would mean redoing it in E-06 anyway.
  Documented as a Known Gap below instead of half-built infra.
- CORS policy, bucket versioning, "no public write" bucket policy, and the 24h multipart-cleanup
  lifecycle rule are all bucket-level/Terraform concerns — noted as deferred infra config alongside
  the Terraform authoring itself, not enforced from C#.
- Kafka `AssetMediaUploaded` consumption/publishing — unrelated, handled by the DynamoDB Streams
  outbox already (ADR-AR-010), not this feature.

## Filename generation (design decision)

`IMediaStorageService.GeneratePresignedUploadUrlAsync(ownerId, assetId, mimeType, sizeBytes, ct)`
does not take a caller-supplied filename (confirmed by reading the interface — F-06 never added one).
To satisfy the `assets/{ownerId}/{assetId}/{filename}` convention, `S3MediaStorageService` generates
the filename itself: a new `Guid` plus an extension derived from `mimeType` via a fixed map covering
exactly `ValidationConstants.MediaRules.AllowedMimeTypes` (`image/jpeg`→`jpg`, `image/png`→`png`,
`image/webp`→`webp`, `video/mp4`→`mp4`). `mimeType` is already validated (ADR-AR-005, pre-URL) by the
time it reaches this service, so the map is exhaustive for the only inputs it will ever see; an
unmapped MIME type is treated as a programmer error (falls back to `bin`, never throws) rather than a
runtime failure, since validation already gates on the same allow-list upstream.
**SPEC_DEVIATION**: filename is server-generated, not client-supplied — flagged here since it's a
genuine interface-shape decision, not a guess dressed as fact.

## Acceptance Criteria

- AC-1: `GeneratePresignedUploadUrlAsync` returns a `PresignedUploadUrl` whose `S3Key` matches
  `assets/{ownerId}/{assetId}/{generatedFilename}` and whose `Url` is a valid presigned PUT URL
  (`X-Amz-Signature` present) with an expiry from `MediaStorageOptions`.
- AC-2: The presigned URL is restricted to `PUT` with the given `Content-Type` — the object can be
  uploaded directly using only the returned URL (no additional headers/creds needed beyond `Content-Type`).
- AC-3: `ValidateUploadAsync` returns `true` only when an object exists at `media.S3Key` in the
  configured bucket with a matching size; returns `false` (not a thrown exception) when the object is
  missing — this is a runtime/business outcome per CLAUDE.md's error-handling convention, not a
  programmer error.
- AC-4: Bucket name/expiry are read via `IOptions<MediaStorageOptions>`, never hardcoded.
- AC-5: Integration test proves the full loop against LocalStack: generate URL → HTTP PUT the object
  using that URL → `ValidateUploadAsync` returns true for it and false for an unrelated key.

## Known Gap (recorded, not resolved here)

Terraform S3 bucket (CORS, versioning, presigned-only/no-public-write policy, 24h multipart lifecycle
cleanup) is deferred to E-06 (M6 "Terraform & Kubernetes") — `iac/` has no module scaffolding for any
resource yet (not just S3), so authoring S3 alone now would be inconsistent with how DynamoDB/KMS
Terraform is planned to land together.
