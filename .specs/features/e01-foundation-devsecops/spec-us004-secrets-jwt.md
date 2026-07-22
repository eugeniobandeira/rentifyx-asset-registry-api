# E-01 · US-004 Secrets & Cross-Service Auth Specification

**Scope note:** second and final slice of E-01. Covers T-020–T-023 from the plan.

## Problem Statement

The service must never hardcode secrets, and must validate JWTs issued by `identity-api` without an API Gateway JWT authorizer. Research into the actual `identity-api` repo found the plan/CLAUDE.md's original assumption (HS256, shared symmetric key) was wrong — `identity-api` signs with RS256 (ADR-006 there). This spec corrects course: RS256, public-key validation.

## Goals

- [ ] Secrets (JWT public key, any future shared secrets) load from AWS Secrets Manager, never from appsettings/env vars in plaintext
- [ ] Requests bearing a JWT signed by `identity-api`'s private key validate successfully; requests with invalid/expired/wrong-issuer tokens are rejected (401)
- [ ] ADR-AR-001 documents the RS256 decision and corrects the earlier HS256 assumption

## Out of Scope

| Item | Reason |
|---|---|
| Token issuance (`TokenService`/`GenerateAccessToken`) | This repo only validates tokens issued by `identity-api`; it never issues its own |
| Role-based authorization middleware for admin endpoints (T-114) | E-05 (API Layer), not this slice — this slice only wires authentication (who you are), not authorization (what you can do) |
| A real DI-injectable `ISecretsProvider` interface | Doesn't exist in `identity-api` either (planned there, never built); copying the actual working `ConfigurationProvider` pattern instead, per user decision 2026-07-22 |
| LocalStack Secrets Manager seeding scripts | Covered by existing LocalStack/Aspire AppHost work (T-012/T-013), not duplicated here |

## Requirements (WHEN/THEN)

1. WHEN the app starts THEN it SHALL load `Jwt:Issuer`, `Jwt:Audience`, `Jwt:PublicKeyPem` (and any other secret values) from AWS Secrets Manager via a config-builder extension (`AddSecretsManager`), merged into `IConfiguration` before other DI registration runs
2. WHEN `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` is `Testing` THEN the Secrets Manager fetch SHALL be skipped (matches `identity-api`'s pattern — avoids AWS calls in unit/integration test runs)
3. WHEN a request arrives with a valid RS256 JWT (correct issuer, audience, not expired, signature verifies against the public key) THEN the request SHALL be authenticated
4. WHEN a request arrives with an expired, wrong-issuer/audience, or bad-signature JWT THEN the request SHALL be rejected with 401
5. WHEN the Secrets Manager secret hasn't been seeded yet (local/dev) THEN the app SHALL start anyway (matches `identity-api`'s `ResourceNotFoundException` → skip-silently behavior), not crash on boot

## Requirement Traceability

| ID | Requirement | Status |
|---|---|---|
| SEC-01 | `AddSecretsManager` config-provider extension, copied from identity-api pattern | Pending |
| SEC-02 | JWT bearer authentication wired with RS256 `TokenValidationParameters` | Pending |
| SEC-03 | ADR-AR-001 (corrected: RS256, not HS256) | Pending |

## Success Criteria

- [ ] `dotnet build`/`dotnet test` pass with the new Infrastructure code added (no AWS calls needed for this — Testing environment skips Secrets Manager)
- [ ] `Program.cs`/IoC wiring compiles and calls `AddSecretsManager` before `AddInfrastructure`
- [ ] ADR-AR-001 exists at `docs/decisions/001-jwt-rs256-validation.md` (numbering: `identity-api` used 001 for secrets-manager-over-appsettings and 006 for JWT algorithm choice — this repo's own ADR sequence already has 002/003 taken by E-02, so this lands as ADR-AR-001 per the plan's own numbering, first available slot here)
