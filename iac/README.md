# Infrastructure as Code

## Current state: no real IaC exists here yet

This is a **known gap**, not an oversight to be worked around. There is no Terraform (or any
other IaC) implemented or applied for this service. Concretely, as of this writing:

- No `.tf` files anywhere under `iac/`
- No EC2 instance, ECR repository, DynamoDB table, KMS key, or Secrets Manager secret has ever
  been provisioned for `rentifyx-asset-registry-api`
- Nothing in this folder has ever been `terraform apply`'d

This work is tracked as **E-06** in the project roadmap (`.specs/project/ROADMAP.md`) and is not
started. Do not assume any AWS resource exists for this service just because the application code
references DynamoDB/S3/Kafka/Secrets Manager clients — those integrations are built and tested
(via LocalStack/Testcontainers) against infrastructure that does not yet exist in a real AWS
account for this repo.

## The expected pattern (from sibling repos)

Two other RentifyX services already have this fully built out and are the reference
implementation to follow when this repo's IaC is built:

- `rentifyx-identity-api/iac/terraform/`
- `rentifyx-communications-api/iac/terraform/`

Both follow the same module layout:

```
iac/terraform/
├── modules/
│   ├── dynamodb/
│   ├── ec2/
│   ├── github-actions/
│   ├── iam/
│   ├── kms/
│   ├── secrets/
│   └── ses/            (communications-api only, or other service-specific modules)
├── main.tf / variables.tf / outputs.tf (per environment or at root)
```

Each module provisions one concern (data table, key management, secrets, compute, IAM roles,
CI/CD OIDC trust for GitHub Actions, etc.) and the environment config composes them together. When
this service gets real IaC, it should mirror this structure rather than inventing a new one.

## What would need to exist for this service to run in production

Following the same pattern as the sibling repos, and matching what `02-src/05-Infrastructure`
already assumes at the application layer:

- **`modules/dynamodb/`** — the single-table Asset Registry table (assets + categories +
  owner-status cache), matching ADR-AR-009's single-table design, with the GSIs the repository
  code already queries against (`DynamoDbAssetRepository`, `DynamoDbCategoryRepository`,
  `DynamoDbOwnerStatusValidator`)
- **`modules/kms/`** — a KMS key for encrypting the DynamoDB table and any S3 objects at rest
- **`modules/secrets/`** — AWS Secrets Manager secrets consumed by
  `SecretsManagerConfigurationProvider`/`AddSecretsManager()` (e.g. `identity-api`'s RS256 public
  key used for JWT validation, per ADR-AR-001)
- **`modules/ec2/`** plus an **ECR repository** — compute to actually run the API and a registry
  for its container image, neither of which currently exists
- **`modules/iam/`** — roles/policies for the compute identity to read/write the DynamoDB table,
  read/write the S3 media bucket, read the relevant Secrets Manager secrets, and assume the right
  execution role
- **`modules/github-actions/`** — OIDC trust so CI/CD can deploy without long-lived AWS keys,
  mirroring the sibling repos' pattern
- **An S3 bucket** for media uploads (presigned URLs), including CORS and a presigned-URL-only
  bucket policy — already flagged as deferred to E-06 in F-11's spec (see
  `.specs/project/ROADMAP.md`)
- **Integration with `rentifyx-platform` via `terraform_remote_state`** — this service does not
  own its own VPC or Kafka cluster. Like the sibling services, it should read VPC subnets/security
  groups and the Kafka broker endpoints from `rentifyx-platform`'s remote state rather than
  redefining them here
- **Kafka topic provisioning** (or confirmation that `rentifyx-platform` owns topic creation) for
  the topics this service already produces to and consumes from in code: `AssetCreated`,
  `AssetMediaUploaded`, `AssetPublished`, `AssetSuspended` (outbound via `OutboxPublisher`), and
  `user-lifecycle-events`, `asset-media-moderated` (inbound, consumed by `OwnerStatusConsumer` and
  `ModerationVerdictConsumer`, both built in F-12)

## Do not

- Do not write Terraform that references resources as if they already exist elsewhere (e.g.
  hardcoded ARNs, IDs) — there is nothing to reference yet
- Do not assume LocalStack/Testcontainers config in `03-tests/04-Repositories/` implies any real
  AWS provisioning — those are local-only test doubles used for automated tests
- Do not backfill this README with resources "because the other repos have them" — only document
  what actually exists once it has been applied
