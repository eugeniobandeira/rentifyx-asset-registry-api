# RentifyxAssetRegistry

Asset catalog microservice for the RentifyX platform. Built with .NET 10 Minimal APIs, Clean Architecture, DDD, and TDD.

## What we are building

A production-grade Asset Registry API covering:
- Asset creation, categorization, media uploads (presigned S3 URLs), search, moderation
- Event-driven moderation from `rentifyx-ai-services`, event-driven owner-status sync from `identity-api`
- AWS integration: DynamoDB, S3, KMS, Secrets Manager, EKS/Fargate
- DevSecOps: OWASP dependency-check, Trivy, coverage gate ≥80%, branch protection

The full 20-day plan is in `RentifyX_AssetRegistryAPI_Plan.md`. For current progress, active
decisions, and deferred work, `.specs/project/STATE.md` and `.specs/project/ROADMAP.md` are the
living source of truth — this file describes conventions and should be updated when they change,
but check STATE.md/ROADMAP.md before assuming something here reflects the current state.

**Resolved gap (see STATE.md D-001):** the `dotnet new clean-arch` scaffold originally generated
this repo with an EF Core + Npgsql `Infrastructure` layer (`AppDbContext`, migrations, generic
`IAddRepository<T>`/`IUnitOfWork`) and an `Example*` reference feature built against it. Both were
removed in full — the plan's stack has no relational database, and no real feature ever depended
on either. `05-Infrastructure` is currently empty of repository implementations; `IAssetRepository`/
`ICategoryRepository` await their DynamoDB implementation in E-04.

## Tech stack

- **Framework**: .NET 10, Minimal APIs, C# latest
- **Architecture**: Clean Architecture · DDD · TDD
- **Cloud**: AWS DynamoDB (single-table design) · S3 · Secrets Manager · KMS (LocalStack locally, see `.specs/project/STATE.md` for current local-dev decision)
- **Infra**: .NET Aspire (AppHost + ServiceDefaults) · Docker · Terraform · GitHub Actions
- **Observability**: OpenTelemetry · Serilog · Scalar
- **Messaging**: Kafka — `AssetCreated`/`AssetMediaUploaded`/`AssetPublished`/`AssetSuspended` out (DynamoDB Streams outbox); `UserSuspended`/`UserDeleted` in (from `identity-api`); moderation verdicts in (from `rentifyx-ai-services`)

## Solution structure

```
01-aspire/
  01-AppHost/         – .NET Aspire orchestration (starts API + future LocalStack)
  02-ServiceDefaults/ – OTel traces/metrics, health checks, service discovery
02-src/
  01-Api/             – Minimal API endpoints, middlewares, extensions
  02-Application/     – Use cases via IHandler<TRequest,TResponse>, FluentValidation validators
  03-Domain/          – Entities, value objects, domain events, repository contracts (no framework deps)
  04-IoC/             – DI wiring (ApplicationDependencyInjection, InfrastructureDependencyInjection)
  05-Infrastructure/  – Repository implementations, AWS SDK adapters (DynamoDB implementation pending, E-04)
03-tests/
  01-Common/          – Shared builders (Bogus)
  02-Validators/      – FluentValidation unit tests
  03-Handlers/        – Handler unit tests (Moq)
  04-Repositories/    – Repository integration tests (Testcontainers)
  05-Integration/     – End-to-end via CustomWebApplicationFactory
docs/                 – Architecture, ADRs, guides
iac/                  – Terraform (DynamoDB, S3, KMS, Secrets Manager, IAM modules — E-06)
k8s/                  – Kustomize/Helm base + overlays (E-06)
```

## Key conventions

### Adding a new feature (follow this order every time)

1. **Domain** – entity / value object / domain event in `02-src/03-Domain/`
2. **Contracts** – repository/service interface in `Domain/Interfaces/{Concept}/` — never loose directly under `Interfaces/`; give every interface a subfolder named after its domain concept (matches `Examples/` precedent)
   - **Repository interfaces compose generic building blocks from `Domain/Interfaces/Common/`** rather than hand-writing each CRUD method: `IGetByIdRepository<T>`, `IGetAllRepository<T>` (no filter) / `IGetAllRepository<T, TFilter>` (filtered, paged), `ISaveRepository<T>` (single upsert verb — fits DynamoDB `PutItem`, no separate insert/update tracking), `ISearchRepository<T, TFilter>` (paged, filtered query distinct from a plain `GetAll`), `ISoftDeleteRepository` (status-flip by `Guid id`, not a hard delete of an entity). Add bespoke methods (e.g. `GetByOwnerAsync`) directly on the feature interface — not everything needs a generic.
   - The older `IAddRepository<T>` / `IUpdateRepository<T>` / `IDeleteRepository<T>` / `IUnitOfWork` (EF Core's separate insert/update change-tracking, hard-delete-by-entity semantics) were removed along with the `Example*` scaffold and the EF Core/Npgsql `Infrastructure` layer — they no longer exist in this repo. All repository interfaces use the `ISaveRepository`/`ISoftDeleteRepository`/`ISearchRepository` set above.
3. **Application** – feature folder under `Application/Features/{Feature}/Handlers/{Action}/`
   - `Request/{Action}Request.cs` → request record
   - `Validator/{Action}Validator.cs` → FluentValidation validator
   - `{Action}Handler.cs` → implements `IHandler<TRequest, TResponse>` (returns `ErrorOr<T>`), the only file loose at the `{Action}/` root
   - `{Feature}Response.cs` + `{Feature}Mapper.cs` at the `{Feature}/` level (response type takes the `Response` suffix, e.g. `SearchAssetsResponse`, not `Result`/`Outcome`)
4. **Infrastructure** – implement repository/service in `Infrastructure/`
5. **IoC** – register in `ApplicationDependencyInjection` or `InfrastructureDependencyInjection`
6. **API** – add endpoint file implementing `IEndpoint` in `Api/Endpoints/{Group}/`
   - No manual wiring needed for endpoints: reflection auto-discovers all `IEndpoint` implementations
   - Validators and handlers are registered explicitly in `ApplicationDependencyInjection` (one `AddScoped<IValidator<T>, ...>` / `AddScoped<IHandler<...>>` line per feature)
   - All endpoints land under `/v1/api/` via versioned routing
7. **Tests** – unit tests in `03-Handlers/` and `02-Validators/`; integration tests in `05-Integration/`

### Result type

All handlers return `ErrorOr<T>`. Map to HTTP with `result.Match(success => ..., errors => errors.ToProblem(httpContext))`.

### Naming

- **Entities get an `Entity` suffix**: `AssetEntity`, `CategoryEntity`. Applies to any Domain type with identity that a repository persists — not to value objects, enums, or events.
- **Value objects, enums, and domain events do NOT take a suffix** describing their kind (no `...VO`, `...Event`): `AssetTitle`, `AssetStatus`, `AssetCreated`.
- **Interfaces are prefixed `I`**: `IAssetRepository`, `IMediaStorageService`.
- **Every async method gets an `Async` suffix** — including interface members (`IHandler<TRequest, TResponse>.HandleAsync(...)`). No `Handle`/`Send`/`Process`-without-suffix methods, on interfaces or implementations. Test methods (`[Fact]`/`[Theory]`) are exempt — they follow test-naming convention (`HappyPath_...`, `InvalidTitle_...`), not this rule.

### Constructors

**Prefer primary constructors (C# 12+)** for classes whose constructor only assigns injected dependencies to fields/properties — do not hand-write a constructor body that just does `_x = x;` for every parameter.

```csharp
// Preferred
public sealed class S3MediaStorageService(
    IAmazonS3 client,
    IOptions<MediaStorageOptions> options) : IMediaStorageService
{
    public Task<string> GeneratePresignedUploadUrlAsync(...) => ...;
}
```

**Exception:** aggregates/entities with invariants to enforce at construction time (e.g. `AssetEntity.Create(...)`) keep a `private` parameterless-or-full constructor plus a `static Create(...)` factory — primary constructors don't fit a type that must validate before allowing construction to succeed.

### Method bodies

**Constructors only: use a block body (`{ }`), not an expression body (`=>`)** — even a one-line assignment. This applies specifically to explicit (non-primary) constructors; regular methods, factory methods, and property getters may freely use expression bodies where that reads well.

### Code style

**Multi-line parameter lists for records, primary constructors, and factory methods with more than one parameter.** Put each parameter on its own line. Single-parameter records/constructors can stay on one line.

### Error handling

- Application/Domain methods that can fail on **expected, runtime business outcomes** (validation, business rules, external call failures) return `ErrorOr<T>` — never throw for these cases.
- Constructors/guard clauses that protect against **programmer error** may still throw (`ArgumentException.ThrowIfNullOrWhiteSpace`, etc.) — this is a narrower case and should stay rare.

### No magic numbers or strings

Error codes, validation limits, and repeated string keys go in `Domain/Constants/` (e.g. `AssetErrorCodes.InvalidTitle`, `ValidationConstants.AssetRules.TitleMaxLength`), never inlined at each call site. Applies to Kafka topic names, config keys, and any other value referenced from more than one place.

### Configuration binding

**Use `IOptions<T>` only where its actual benefit applies:** a class the DI container constructs (registered singleton/scoped service or hosted service) that needs the config value via constructor injection. For a one-shot startup read inside an `AddX(this IServiceCollection services, IConfiguration configuration)` extension method called directly from `Program.cs` and never resolved again, bind a plain typed record once with `configuration.GetSection("X").Get<T>()` — don't also register it with `services.Configure<T>()` just for consistency.

### Enum persistence

**Never persist an enum as its underlying numeric value.** Always store/serialize the string name (`"Active"`, `"PendingModeration"`), never the `int` — applies to `AssetStatus` once the DynamoDB repository lands (E-04).

### Endpoint pattern

```csharp
internal sealed class MyAction : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/resource", HandleAsync)
           .WithName("...").WithDescription("...").WithTags(Tags.XXX);
    }

    private static async Task<IResult> HandleAsync(
        MyRequest request,
        IHandler<MyRequest, MyResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return result.Match(r => Results.Ok(r), e => e.ToProblem(httpContext));
    }
}
```

### Domain entity pattern

Static factory `Create(...)`, private setters, no public constructor. Use `ArgumentException.ThrowIfNullOrWhiteSpace` for guards.

### Build rules (enforced globally via Directory.Build.props)

- `Nullable=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`
- SonarAnalyzer.CSharp wired on every project
- NuGet versions centralized in `Directory.Packages.props`
- Git hooks path set to `.hooks/` (pre-commit)

## Running locally

```bash
# Start API via Aspire (Dashboard + Scalar UI)
dotnet run --project 01-aspire/01-AppHost/RentifyxAssetRegistry.AppHost

# Run all tests
dotnet test RentifyxAssetRegistry.slnx

# Build release
dotnet build RentifyxAssetRegistry.slnx --configuration Release
```

## CI/CD

GitHub Actions (`ci.yml`) triggers on PRs to `main`:
1. **Build & Test** – restore → build Release → test
2. **Coverage gate** – ≥80% (coverlet + ReportGenerator) — planned, not yet added (E-01)
3. **OWASP dependency-check** – NuGet vulnerability scan, fails on CVSS ≥ 7 — planned (E-01)
4. **Trivy container scan** – blocks on CRITICAL/HIGH — planned (E-01)
5. **Branch protection** – CI green + 1 PR review before merge to main — planned (E-01)

## Security rules

- **Never** hardcode secrets. All sensitive config comes from AWS Secrets Manager, loaded via a custom `ConfigurationProvider` (see `SecretsManagerConfigurationProvider`/`AddSecretsManager()` — copied from `identity-api`'s pattern, not a DI-injected `ISecretsProvider` service; that abstraction was planned in `identity-api` but never actually built there).
- No stack traces in error responses (`GlobalExceptionHandler` strips them).
- Category creation/mutation is **admin-only** (ADR-AR-006).
- File size/MIME validation happens **before** presigned URL generation, not after upload (ADR-AR-005).
- JWT validation is **RS256** (asymmetric), validating against `identity-api`'s public key fetched from Secrets Manager — no API Gateway JWT authorizer (ADR-AR-001). Corrected 2026-07-22: originally documented here as HS256, but `identity-api`'s actual ADR-006 chose RS256 specifically so downstream services (like this one) can validate signatures without holding the signing secret; this repo only ever needs the public key, never a shared symmetric secret.

## Test structure conventions

- **Validators** (`03-tests/02-Validators/`) – test all valid/invalid combinations, no mocks needed
- **Handlers** (`03-tests/03-Handlers/`) – mock repository/service interfaces with Moq
- **Repositories** (`03-tests/04-Repositories/`) – Testcontainers (LocalStack/DynamoDB); no mocks
- **Integration** (`03-tests/05-Integration/`) – `CustomWebApplicationFactory` + `Microsoft.AspNetCore.Mvc.Testing`
- Test data: Bogus via builder classes in `Tests.Common/Builders/`
- Assertions: FluentAssertions
