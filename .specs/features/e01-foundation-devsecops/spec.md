# E-01 · CI/CD Pipeline & DevSecOps Baseline Specification (F-02 slice)

**Scope note:** this spec covers only F-02 (CI/CD Pipeline & DevSecOps Baseline) from the E-01 plan section. F-01 (repo scaffold) is already done. US-004 (secrets/JWT, T-020–T-023) is a separate, larger slice — deferred, not in this pass.

## Problem Statement

`.github/workflows/ci.yml` currently only builds and runs tests. The plan (US-003) calls for automated security gates so vulnerabilities never reach `main`: dependency scanning, container scanning, and branch protection.

## Goals

- [ ] CI fails the build when a NuGet dependency has a known vulnerability at CVSS ≥ 7
- [ ] CI fails the build when the Docker image has a CRITICAL or HIGH severity vulnerability
- [ ] `main` branch requires CI green + 1 PR review before merge

## Out of Scope

| Item | Reason |
|---|---|
| Coverage gate (≥80%, coverlet+ReportGenerator) | D-002 — explicit user decision, CI verifies tests pass, no coverage threshold enforced |
| `ISecretsProvider` / AWS Secrets Manager / JWT validation (T-020–T-023) | Separate, larger slice — not part of this CI/CD pass |
| LocalStack in Aspire AppHost (T-012–T-014) | Separate F-01 item, not CI/CD |

## Requirements (WHEN/THEN)

1. WHEN a PR is opened against `master` THEN CI SHALL run build → test (existing, unchanged) → OWASP dependency-check → Trivy container scan
2. WHEN a NuGet package has a known CVE with CVSS ≥ 7 THEN the dependency-check step SHALL fail the workflow
3. WHEN the built Docker image has a CRITICAL or HIGH vulnerability THEN the Trivy step SHALL fail the workflow
4. WHEN someone attempts to merge a PR into `master` without CI passing or without 1 approving review THEN GitHub SHALL block the merge (branch protection rule)

## Requirement Traceability

| ID | Requirement | Status |
|---|---|---|
| CICD-01 | OWASP dependency-check step, fail on CVSS ≥ 7 | Pending |
| CICD-02 | Trivy container scan step, fail on CRITICAL/HIGH | Pending |
| CICD-03 | Branch protection on `master`: CI green + 1 review | Pending |

## Success Criteria

- [ ] `ci.yml` runs dependency-check and Trivy on every PR to `master`
- [ ] A deliberately-vulnerable test dependency (if injected) would fail the pipeline (verified by reading the action's documented behavior, not by actually introducing a CVE into this repo)
- [ ] `master` branch protection rule visible via `gh api repos/:owner/:repo/branches/master/protection` after configuration
