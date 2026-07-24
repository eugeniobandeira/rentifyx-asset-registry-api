# Clean Architecture Template

![.NET](https://img.shields.io/badge/.NET-10-512BD4)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A .NET 10 project template for building production-ready Web APIs using Clean Architecture.

## Tech Stack

| Concern | Library / Technology |
|---|---|
| Framework | ASP.NET Core 10 Minimal APIs |
| Error Handling | ErrorOr 2.0.1 |
| Validation | FluentValidation 12.1.1 |
| Logging | Serilog 10.0.0 |
| API Versioning | Asp.Versioning.Http 8.1.0 |
| API Documentation | Scalar + Microsoft.AspNetCore.OpenApi |
| Orchestration | .NET Aspire 9.3.1 |
| Observability | OpenTelemetry (traces, metrics, logs) |
| Testing | xUnit, Moq, FluentAssertions, Bogus |
| Code Analysis | SonarAnalyzer.CSharp |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- .NET Aspire workload:

```bash
dotnet workload install aspire
```

## Quick Start

```bash
dotnet new install EugenioBandeira.RentifyxAssetRegistryTemplate
dotnet new clean-arch -n MyProject
```

## Project Structure

```
MyProject/
├── 01-aspire/
│   ├── 01-AppHost/
│   │   └── MyProject.AppHost/              # .NET Aspire orchestration
│   └── 02-ServiceDefaults/
│       └── MyProject.ServiceDefaults/      # OpenTelemetry, health checks, service discovery
├── 02-src/
│   ├── 01-Api/
│   │   └── MyProject.Api/                  # Endpoints, middlewares, extensions
│   ├── 02-Application/
│   │   └── MyProject.Application/          # Handlers, validators, DTOs, mappers
│   ├── 03-Domain/
│   │   └── MyProject.Domain/               # Entities, repository interfaces, constants
│   ├── 04-IoC/
│   │   └── MyProject.IoC/                  # Dependency injection wiring
│   └── 05-Infrastructure/
│       └── MyProject.Infrastructure/       # Repository implementations
├── 03-tests/
│   ├── 01-Common/                          # Shared builders (Bogus)
│   ├── 02-Validators/                      # FluentValidation unit tests
│   ├── 03-Handlers/                        # Handler unit tests
│   ├── 04-Repositories/                    # Repository tests
│   └── 05-Integration/                     # API integration tests (WebApplicationFactory)
├── docs/                                   # Architecture docs, ADRs, feature specs
├── iac/                                    # Infrastructure as Code (Terraform, Bicep, etc.)
├── k8s/                                    # Kubernetes manifests (Kustomize)
│   ├── base/
│   └── overlays/
│       ├── dev/
│       └── prod/
├── Directory.Build.props                   # Shared build settings for all projects
├── Directory.Packages.props                # Centralized NuGet package versions
├── Dockerfile
└── RentifyxAssetRegistry.slnx
```

## Architecture

### Layer responsibilities

| Layer | Responsibility | Allowed dependencies |
|---|---|---|
| Domain | Entities, repository interfaces, error codes | None |
| Application | Handlers, validators, DTOs, mappers | Domain |
| Infrastructure | Repository implementations | Domain |
| IoC | DI registration | All layers |
| Api | Endpoints, middlewares, HTTP mapping | Application, Domain |

### Dependency flow

```
Api → Application → Domain ← Infrastructure
                       ↑
              IoC (wires all layers)
```

- **Domain** has no outbound dependencies — it defines interfaces, not implementations.
- **Infrastructure** implements Domain interfaces. It never references Application.
- **Application** depends only on Domain interfaces, never on Infrastructure directly.
- **IoC** is the only layer that references all others — it is the composition root.
- **Api** depends on Application (handlers) and IoC.

### Handler pattern

Every use case implements `IHandler<TRequest, TResponse>`, returning `ErrorOr<T>` instead of throwing exceptions:

```csharp
public interface IHandler<TRequest, TResponse>
{
    Task<ErrorOr<TResponse>> Handle(TRequest request, CancellationToken ct = default);
}
```

Handlers are registered explicitly in `ApplicationDependencyInjection.cs`:

```csharp
services.AddScoped<IHandler<CreateExampleRequest, ExampleEntity>, CreateExampleHandler>();
services.AddScoped<IHandler<Guid, ExampleEntity>, GetByIdExampleHandler>();
// ...
```

### Repository interfaces

Repositories use **segregated interfaces** — one interface per operation — combined into a feature-specific composite interface:

```csharp
// Segregated interfaces (Domain/Interfaces/Common)
IAddRepository<T>
IGetByIdRepository<T>
IUpdateRepository<T>
IDeleteRepository<T>
IGetAllRepository<T, TFilter>   // TFilter lives in Domain/Filters

// Composite interface per feature (Domain/Interfaces/<Feature>)
public interface IExampleRepository :
    IAddRepository<ExampleEntity>,
    IGetByIdRepository<ExampleEntity>,
    IGetAllRepository<ExampleEntity, ExampleFilter>,
    IUpdateRepository<ExampleEntity>,
    IDeleteRepository<ExampleEntity>
{ }
```

A concrete repository implements only the composite interface:

```csharp
public sealed class ExampleRepository : IExampleRepository
{ ... }
```

**Filter types** (`ExampleFilter`, etc.) live in `Domain/Filters/` — keeping the Domain layer free of Application or Infrastructure dependencies. Handlers map the HTTP request to the filter via the feature mapper:

```csharp
// Application/Features/Example/Mapper/ExampleMapper.cs
public static ExampleFilter ToFilter(GetAllExampleRequest request)
    => new(request.Page, request.PageSize, request.Name, request.IsActive);
```

Repository and interface registrations are explicit in `InfrastructureDependencyInjection.cs` — no reflection-based discovery.

### Feature organization

Features are organized by name inside `Application/Features/{Feature}/`:

```
Application/Features/Example/
├── ExampleResponse.cs
├── Mapper/ExampleMapper.cs
└── Handlers/
    ├── Create/
    │   ├── CreateExampleHandler.cs
    │   ├── Request/CreateExampleRequest.cs
    │   └── Validator/CreateExampleValidator.cs
    ├── GetById/GetByIdExampleHandler.cs
    ├── GetAll/
    │   ├── GetAllExampleHandler.cs
    │   └── Request/GetAllExampleRequest.cs
    ├── Update/
    │   ├── UpdateExampleHandler.cs
    │   ├── Request/UpdateExampleRequest.cs
    │   └── Validator/UpdateExampleValidator.cs
    └── Delete/DeleteExampleHandler.cs
```

Endpoints follow the same convention in the Api layer:

```
Api/Endpoints/Example/
├── Create.cs
├── GetById.cs
├── GetAll.cs
├── Update.cs
└── Delete.cs
```

### Endpoints

Each endpoint is a single file implementing `IEndpoint` and is auto-registered via reflection — no manual wiring:

```csharp
internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/examples", HandleAsync)
           .WithName("CreateExample")
           .WithTags(Tags.EXAMPLE);
    }
}
```

All endpoints are mounted under `/api/v1` with rate limiting applied automatically.

## Centralized Package Management

All NuGet package versions are declared once in `Directory.Packages.props` at the solution root. Individual `.csproj` files reference packages **without specifying versions** — versions are resolved centrally.

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup Label="Application">
    <PackageVersion Include="ErrorOr" Version="2.0.1" />
    <PackageVersion Include="FluentValidation" Version="12.1.1" />
  </ItemGroup>

  <ItemGroup Label="Api">
    <PackageVersion Include="Scalar.AspNetCore" Version="2.14.14" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.8" />
  </ItemGroup>

  <ItemGroup Label="Tests">
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="FluentAssertions" Version="8.2.0" />
  </ItemGroup>
  <!-- ... -->
</Project>
```

**Benefits:**
- No version conflicts between projects — a single source of truth.
- To upgrade a package, edit one line in `Directory.Packages.props`.
- PRs show version changes in one file, making upgrades easy to review.

### Shared build settings

`Directory.Build.props` at the solution root applies common MSBuild properties to every project automatically:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisMode>Recommended</AnalysisMode>
    <LangVersion>latest</LangVersion>
    <NuGetAuditMode>direct</NuGetAuditMode>
  </PropertyGroup>
</Project>
```

`03-tests/Directory.Build.props` extends the root file and suppresses analyzer rules that conflict with test conventions (underscore naming, interface-typed fields, etc.) — without touching production project settings.

## Middlewares

### CorrelationIdMiddleware

Tracks every request end-to-end across logs, responses, and error payloads.

- Reads `X-Correlation-Id` from the request header; generates a new `Guid` if absent.
- Sanitizes the value (alphanumeric + dashes, max 64 chars) to prevent header injection.
- Stores the value in `HttpContext.Items` and echoes it in the `X-Correlation-Id` response header.
- Pushes it to Serilog's `LogContext` — every log line in that request automatically includes `{CorrelationId}`.

### GlobalExceptionHandler

Catches all unhandled exceptions and returns a structured `ProblemDetails` response (RFC 7807):

```json
{
  "status": 500,
  "title": "An unexpected error occurred.",
  "instance": "/api/v1/examples",
  "extensions": {
    "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
    "exceptionType": "System.InvalidOperationException",
    "exceptionMessage": "Sequence contains no elements."
  }
}
```

`OperationCanceledException` triggered by client disconnection returns HTTP 499 and is logged as a warning, not an error.

### Rate Limiting

Fixed window policy applied globally to all versioned endpoints. Configurable via `appsettings.json`:

```json
"RateLimit": {
  "PermitLimit": 100,
  "WindowSeconds": 60,
  "QueueLimit": 0
}
```

### CORS

Configured via `appsettings.json`. Update the allowed origins before going to production:

```json
"Cors": {
  "AllowedOrigins": [ "https://your-frontend.com" ]
}
```

## API Endpoints

All endpoints are mounted under `/api/v1` (via `AddVersioning()` + `MapVersionedApi(1)` in `EndpointExtensions.cs`), except the raw health checks below. Every endpoint currently calls `.AllowAnonymous()` — no authentication/authorization middleware is enforced yet at the endpoint level, even though `isAdmin`/`ownerId` fields are asserted in request bodies and checked inside the handlers.

### Assets (`Api/Endpoints/Assets/`)

| Method | Route | Description | Auth |
|---|---|---|---|
| `POST` | `/api/v1/assets` | Creates a new asset in `Draft` status; the caller becomes the owner. | None enforced (`AllowAnonymous`) |
| `GET` | `/api/v1/assets` | Searches published assets, cursor-paginated, filterable by category, price range, and keyword. | None enforced (`AllowAnonymous`) |
| `GET` | `/api/v1/assets/{id:guid}` | Returns a single asset by id regardless of moderation status; 404 if not found. | None enforced (`AllowAnonymous`) |
| `POST` | `/api/v1/assets/{id:guid}/submit-for-moderation` | Moves a `Draft` asset owned by the caller into `PendingModeration`. | None enforced (`AllowAnonymous`); handler checks the asserted `ownerId` |
| `POST` | `/api/v1/assets/{id:guid}/admin-review` | Admin override for a `PendingModeration` asset: approve to `Active` or reject back to `Draft`, bypassing the automated moderation verdict. | None enforced (`AllowAnonymous`); handler requires `isAdmin: true` |
| `POST` | `/api/v1/assets/{id:guid}/media/upload-request` | Validates a proposed media upload's MIME type/size and returns a presigned S3 upload URL. | None enforced (`AllowAnonymous`) |
| `POST` | `/api/v1/assets/{id:guid}/media/confirm` | Confirms a completed upload (matching the `s3Key` from a prior upload-request) and attaches it to the asset. | None enforced (`AllowAnonymous`) |

### Categories (`Api/Endpoints/Categories/`)

| Method | Route | Description | Auth |
|---|---|---|---|
| `POST` | `/api/v1/categories` | Creates a root or child category (up to max depth 3). | None enforced (`AllowAnonymous`); handler requires `isAdmin: true` (ADR-AR-006) |
| `GET` | `/api/v1/categories` | Lists all categories as a flat list (with `parentCategoryId`/`depth` for client-side hierarchy reconstruction). | None enforced (`AllowAnonymous`) |
| `PATCH` | `/api/v1/categories/{id:guid}` | Renames and/or re-parents a leaf category. | None enforced (`AllowAnonymous`); handler requires `isAdmin: true` (ADR-AR-006) |

### Health (`Api/Endpoints/Health/`)

| Method | Route | Description | Auth |
|---|---|---|---|
| `GET` | `/health` | Returns `{ "status": "healthy" }`; not versioned. | None (`AllowAnonymous`) |

## Health Endpoints

| Route | Purpose |
|---|---|
| `GET /health` | All registered health checks |
| `GET /alive` | Liveness probe (checks tagged `live`) |
| `GET /api/v1/health` | Application-level health check (versioned, documented in Swagger) |

## Error Handling

Business logic never throws — it returns `ErrorOr<T>`. Endpoints map the result to HTTP responses:

```csharp
ErrorOr<ExampleEntity> result = await handler.Handle(request, ct);

return result.Match(
    entity => Results.Ok(entity.ToResponse()),
    errors => errors.ToProblem(httpContext));
```

Error types are mapped to HTTP status codes automatically:

| ErrorOr type | HTTP status |
|---|---|
| `Error.Validation` | 422 Unprocessable Entity |
| `Error.NotFound` | 404 Not Found |
| `Error.Conflict` | 409 Conflict |
| `Error.Unauthorized` | 401 Unauthorized |
| Other | 500 Internal Server Error |

## Observability

The template ships with OpenTelemetry pre-configured for traces, metrics, and logs via `.NET Aspire ServiceDefaults`.

Set the following environment variables to enable export to any OTLP-compatible collector:

| Variable | Description | Default |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Collector URL | _(empty — export disabled)_ |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `http/protobuf` or `grpc` | `http/protobuf` |
| `OTEL_EXPORTER_OTLP_HEADERS` | Auth headers (e.g. API key) | _(empty)_ |
| `OTEL_SERVICE_NAME` | Service name in traces/metrics | `RentifyxAssetRegistry.Api` |
| `OTEL_RESOURCE_ATTRIBUTES` | Additional resource metadata | `deployment.environment=production` |

Compatible platforms: Grafana Cloud, Datadog, New Relic, Honeycomb, Elastic, Jaeger, OpenTelemetry Collector.

### Serilog sinks (logs only)

If you prefer sending logs via a Serilog sink instead of OTLP:

```bash
dotnet add package Serilog.Sinks.Seq
dotnet add package Serilog.Sinks.Datadog.Logs
dotnet add package Serilog.Sinks.Elasticsearch
```

Configure in `appsettings.json` under `Serilog.WriteTo`.

## Infrastructure

### Container

The `Dockerfile` builds a multi-stage image (`dotnet/sdk:10.0` → `dotnet/aspnet:10.0`) and exposes port `8080`. Entry point: `dotnet RentifyxAssetRegistry.Api.dll`.

### Kubernetes (`k8s/`)

`k8s/` contains Kustomize manifests: a `base/` with `deployment.yaml` (1 replica, `api:latest` image, container port 8080, OTEL env vars, resource requests/limits) and `service.yaml` (ClusterIP, port 80 → targetPort 8080), plus `dev`/`prod` overlays (`dev` at 1 replica/`Development`, `prod` at 3 replicas/`Production`). These manifests are still the generic scaffold carried over from the Clean Architecture template — the image name, resource limits, and env vars are placeholders and no real cluster deployment of this service exists yet. Applying them (`kubectl apply -k k8s/overlays/dev`) does not wire up DynamoDB/S3/Kafka connectivity or the Secrets Manager-backed configuration this API actually depends on.

### Terraform / IaC (`iac/`)

`iac/README.md` documents only the intended structure (e.g. an `iac/terraform/` folder with `main.tf`/`variables.tf`/`outputs.tf`) — no actual Terraform (or other IaC) files exist in the repo yet. Per `CLAUDE.md`, the DynamoDB/S3/KMS/Secrets Manager/IAM Terraform modules are planned for a later milestone (E-06) and have not been written.

### AWS dependencies (application-level, not yet provisioned via IaC)

The application code already integrates directly with several AWS services (configured via `appsettings.json`/Secrets Manager, provisioned manually or via LocalStack today, not via the `iac/` folder):

- **DynamoDB** — single-table design; `DynamoDbAssetRepository` / `DynamoDbCategoryRepository` in `05-Infrastructure`, plus the Outbox items stored in the same table (GSI1 `OUTBOX_STATUS#Pending` partition).
- **S3** — `S3MediaStorageService` generates presigned upload URLs for asset media.
- **Secrets Manager** — `SecretsManagerConfigurationProvider` / `AddSecretsManager()` loads sensitive configuration at startup (no hardcoded secrets).

### Kafka messaging

Configured in `Program.cs` via `AddOutboxPublishing`, `AddCrossServiceConsuming`, and three hosted `BackgroundService`s. Topic names live in `Domain/Constants/KafkaTopics.cs`.

**Published (Outbox pattern, `Api/Messaging/OutboxPublisher.cs`):**

Domain events are written to a DynamoDB outbox table alongside the triggering write (same transaction), then a `PeriodicTimer`-driven `OutboxPublisher` background service polls the `Pending` GSI1 partition, publishes each entry's serialized payload to Kafka (keyed by `AssetId` for per-asset ordering), and flips the entry to `Published` (or `Failed` after `MaxRetries`). This decouples the DynamoDB write from the Kafka publish and guarantees at-least-once delivery without a distributed transaction. Topics published, resolved by event type:

| Event | Kafka topic |
|---|---|
| `AssetCreated` | `asset-registry.asset-created` |
| `AssetMediaUploaded` | `asset-registry.asset-media-uploaded` |
| `AssetPublished` | `asset-registry.asset-published` |
| `AssetSuspended` | `asset-registry.asset-suspended` |

**Consumed:**

| Topic | Consumer | Behavior |
|---|---|---|
| `asset-media-moderated` | `ModerationVerdictConsumer` | Deserializes an `AssetMediaModeratedEvent` (schema version checked, mismatches skipped as poison pills), then calls the `ApplyModerationVerdict` handler for the asset. `NotFound` results are treated as poison pills and committed; other errors are left uncommitted for Kafka redelivery. The handler's own idempotent-replay behavior (no-op if the asset isn't currently `PendingModeration`) makes duplicate redeliveries safe. |
| `user-lifecycle-events` | `OwnerStatusConsumer` | Deserializes a `UserLifecycleEventEnvelope` from `identity-api` and handles `UserSuspended`/`UserAccountDeleted` event types by upserting an owner-status cache entry (`IOwnerStatusCacheWriter`). Unrecognized event types and malformed payloads are logged and committed (poison pills); cache-write failures are left uncommitted for redelivery. |

Note: `CLAUDE.md` (written earlier in the project) still lists the owner-status/moderation consumers as not-yet-built (F-12) and references an `AssetEnrichmentSuggested` topic — neither the consumer code nor `KafkaTopics.cs` in the current codebase has an `AssetEnrichmentSuggested` topic; only `asset-media-moderated` and `user-lifecycle-events` are consumed today.

## Post-Generation Setup

After running `dotnet new clean-arch -n MyProject`, complete the following steps:

### 1. Replace the connection string

The template uses PostgreSQL (Npgsql) via EF Core. `appsettings.json` ships with a local development default:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=RentifyxAssetRegistryDb;Username=postgres;Password=postgres"
}
```

Running via the Aspire AppHost (`dotnet run` in `01-aspire/01-AppHost/RentifyxAssetRegistry.AppHost`) provisions a PostgreSQL container automatically (requires Docker running) and injects the connection string for you — no manual setup needed for local development. Replace the value above only if you run the API standalone against a different database.

### 2. Run database migrations

The template ships with EF Core (`Microsoft.EntityFrameworkCore.Tools` is already referenced on `RentifyxAssetRegistry.IoC`). `AppDbContext` lives in `RentifyxAssetRegistry.Infrastructure`, and it's wired up via `RentifyxAssetRegistry.Api` (the startup project), so every `dotnet ef` command needs both `--project` (where the context lives) and `--startup-project` (where the DI/configuration lives).

Install the tool once, if you don't have it yet:

```bash
dotnet tool install --global dotnet-ef
```

**Create a migration** whenever you change an entity or `AppDbContext.OnModelCreating`:

```bash
dotnet ef migrations add <MigrationName> \
  --project 02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure \
  --startup-project 02-src/01-Api/RentifyxAssetRegistry.Api \
  --output-dir Context/Migrations
```

**Apply migrations to your local database:**

The AppHost provisions the PostgreSQL container and gives you the connection string, but it does not apply migrations for you — run this once against it (with the AppHost, or any local PostgreSQL instance, already up):

```bash
dotnet ef database update \
  --project 02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure \
  --startup-project 02-src/01-Api/RentifyxAssetRegistry.Api
```

**Remove the last migration** (only if it hasn't been applied yet):

```bash
dotnet ef migrations remove \
  --project 02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure \
  --startup-project 02-src/01-Api/RentifyxAssetRegistry.Api
```

### 3. Implement the repository

Open `Infrastructure/Repositories/ExampleRepository.cs` and implement the methods using your chosen persistence technology (EF Core, Dapper, MongoDB, etc.):

```csharp
public sealed class ExampleRepository : IExampleRepository
{
    // Your implementation here
}
```

### 4. Update CORS origins

In `appsettings.json`, replace the placeholder with your frontend URL:

```json
"Cors": {
  "AllowedOrigins": [ "https://your-frontend.com" ]
}
```

### 5. Update OpenAPI contact info

In `appsettings.json`:

```json
"OpenApi": {
  "ContactName": "your-name",
  "ContactUrl": "https://github.com/your-handle"
}
```

### 6. Configure observability (optional)

Set `OTEL_EXPORTER_OTLP_ENDPOINT` to point to your collector. Leave it empty to disable export during local development.

### 7. Replace the Example stubs

The `Example*` files throughout the project are working stubs that demonstrate all patterns end-to-end. Use them as a reference, then replace them with your own features.

## Adding a New Feature

The workflow for adding a feature (e.g. `Product`) mirrors the existing `Example` feature:

**1. Domain** — add entity and error codes:

```
Domain/Entities/ProductEntity.cs
Domain/Constants/ProductErrorCodes.cs
```

**2. Application** — add handlers, requests, validators, mapper:

```
Application/Features/Products/
├── ProductResponse.cs
├── Mapper/ProductMapper.cs
└── Handlers/
    ├── Create/
    │   ├── CreateProductHandler.cs
    │   ├── Request/CreateProductRequest.cs
    │   └── Validator/CreateProductValidator.cs
    ├── GetById/GetByIdProductHandler.cs
    ├── GetAll/
    │   ├── GetAllProductHandler.cs
    │   └── Request/GetAllProductRequest.cs
    ├── Update/
    │   ├── UpdateProductHandler.cs
    │   ├── Request/UpdateProductRequest.cs
    │   └── Validator/UpdateProductValidator.cs
    └── Delete/DeleteProductHandler.cs
```

**3. Infrastructure** — implement the repository:

```csharp
public sealed class ProductRepository : IRepository<ProductEntity, GetAllProductRequest>
{
    // EF Core, Dapper, etc.
}
```

**4. IoC** — nenhuma alteração necessária. Handlers e repositórios são registrados automaticamente via reflection ao implementar `IHandler<,>` e `IRepository<,>`.

**5. Api** — add one file per endpoint:

```
Api/Endpoints/Products/
├── Create.cs
├── GetById.cs
├── GetAll.cs
├── Update.cs
└── Delete.cs
```

Endpoints are registered automatically via reflection — no additional wiring needed.

## Running Locally

With Aspire orchestration (recommended):

```bash
dotnet run --project "01-aspire/01-AppHost/RentifyxAssetRegistry.AppHost"
```

Or directly:

```bash
dotnet run --project "02-src/01-Api/RentifyxAssetRegistry.Api"
```

## Running with Docker

```bash
docker build -t myproject .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production myproject
```

## Running on Kubernetes

```bash
kubectl apply -k k8s/overlays/dev
kubectl apply -k k8s/overlays/prod
```

## Contributing

See [docs/](docs/) for architecture docs, ADRs, and contributor guides.

## License

MIT © eugeniobandeira
