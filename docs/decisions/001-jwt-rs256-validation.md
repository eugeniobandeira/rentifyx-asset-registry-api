# ADR-001: In-Service JWT Validation via RS256 Public Key

- **Date:** 2026-07-22
- **Status:** Accepted

## Context

`identity-api` issues JWTs for cross-service auth. This service (asset-registry) must validate them without an API Gateway JWT authorizer (per the plan). The plan originally documented this as HS256 with a "shared signing key," but that assumption predates checking `identity-api`'s actual implementation.

## Options Considered

- **Option A — HS256, shared symmetric key** — what the plan/CLAUDE.md originally assumed. Requires the same secret value present in both `identity-api` (to sign) and `asset-registry` (to validate) — a shared secret both services must protect equally, and any downstream service validating tokens holds a key that could also forge them.
- **Option B — RS256, asymmetric key pair** — what `identity-api` actually implemented (ADR-006 there). `identity-api` holds the private key (signs), every downstream service (including this one) only ever needs the public key (validates). A leaked public key can't be used to forge tokens.
- **Option C — Delegate to API Gateway JWT authorizer** — explicitly rejected by the plan; no API Gateway authorizer in this architecture.

## Decision

Option B, matching `identity-api`'s real implementation. `IAssetRegistry` validates JWTs in-service via `Microsoft.AspNetCore.Authentication.JwtBearer`, configured with `TokenValidationParameters.IssuerSigningKey` built from `identity-api`'s **public** key (`Jwt:PublicKeyPem`), fetched from AWS Secrets Manager alongside `Jwt:Issuer`/`Jwt:Audience` — never a symmetric secret.

`ValidateIssuer`/`ValidateAudience`/`ValidateIssuerSigningKey` are left at their library defaults (`true`); only `ValidateLifetime = true` and `ClockSkew = TimeSpan.Zero` are set explicitly, matching `identity-api`'s own validation-side configuration exactly (this repo is a downstream consumer of the same tokens, not an independent issuer, so the validation parameters should match bit-for-bit).

Secrets load via a custom `SecretsManagerConfigurationProvider`/`AddSecretsManager()` config-builder extension — copied from `identity-api`'s working pattern — rather than a DI-injected `ISecretsProvider` interface. That interface was planned in `identity-api`'s own backlog (`T-023`) but never actually built there; there is no real prior art for it to reuse, only the config-provider approach that is actually running in production-shaped code.

## Consequences

- This service never possesses `identity-api`'s private signing key — a compromise of asset-registry's Secrets Manager entry only leaks a public key, not a forgeable secret.
- If `identity-api`'s public key rotates, this service picks it up on next restart (Secrets Manager fetch happens at app startup, not cached indefinitely) — no code change needed, only a Secrets Manager value update.
- Local/test runs skip the Secrets Manager call entirely when `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` is `Testing`, and tolerate a not-yet-seeded secret (`ResourceNotFoundException` swallowed) in other environments so the app can still boot during initial local setup — matching `identity-api`'s exact behavior.
- CLAUDE.md's security rules section, originally documented as HS256, has been corrected to RS256 (2026-07-22) to match this ADR and the actual `identity-api` implementation.
