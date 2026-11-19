## Commands

```bash
dotnet build AppName.slnx
dotnet test AppName.slnx
dotnet run --project src/AppName.<Service>.Api   # Scalar docs: http://localhost:<port>/docs
```

The services and their ports are in `.agents/SOLUTION.md`. `dotnet run --project` uses
`src/AppName.<Service>.Api` as the working folder, so the local data (`data/`) of a service is in
`src/AppName.<Service>.Api/data/`.
<!-- @module:jwt -->
The JWT key file is the one exception: in Development every service uses `data/keys/jwt-secret` at
the solution root (`Jwt:SecretFile` in `appsettings.Development.json`), so all services share one key.
<!-- @end -->
<!-- @module:auth -->

<!-- [setup:database-commands] The database step adds the commands to add and apply a migration here. -->
<!-- @end -->
<!-- @module:docker -->

```bash
docker compose up --build   # each service on its port, data in named volumes (compose.yaml)
```
<!-- @end -->

## Architecture

Clean Architecture with CQRS through MediatR. The solution holds one or more services. Each service has
its own projects `AppName.<Service>.*`, and all services share the projects in the list below. The
dependencies point inward:

```
Api → Application → Domain
Api → Api.Contracts, Api.Infrastructure, Common, Persistence
Application → Api.Contracts, Common, Domain, Persistence
Persistence → Common, Domain
Api.Infrastructure → Common
<!-- @module:jwt -->
Api → Api.Authentication
<!-- @end -->
<!-- @module:auth -->
Api, Application → Security
Security → Api.Authentication, Domain, Persistence
<!-- @end -->
```

Domain, Application and Persistence never reference `AppName.Api.Infrastructure`.

Shared projects (`src/`). Put code here only when every service uses it the same way and it has no
business logic. Code of one service stays in that service.

- **AppName.Common** — `Result` / `Result<T>` and `Error`. Every application operation returns a `Result`.
  `Errors/` holds the error types (`NotFoundError`, `ForbiddenError`, `ConflictError`, `ValidationError`,
  `ServiceUnavailableError`). `Persistence/` holds `CollectionResult<T>` and `CursorSortMismatchException`.
  `PaginationParameters` is the cursor and page size of a request. No framework dependencies.
- **AppName.Api.Infrastructure** — The API plumbing of every service: the host setup
  (`Hosting/ApiHostExtensions`: `AddApiDefaults`, `UseApiDefaults`, `MapApiDefaults`), error mapping,
  OpenAPI, versioning, `IEndpoint` discovery and the shared options.
<!-- @module:jwt -->
- **AppName.Api.Authentication** — The access token check that every service with authentication shares:
  the signing key (`JwtSecretFile`), `AddJwtAuthentication` (JwtBearer validation and the default-deny
  fallback policy), `GetUser()` / `GetTokenMetadata()` and the OpenAPI Bearer scheme.
<!-- @end -->

Projects of a service (`src/AppName.<Service>.*`):

- **AppName.<Service>.Domain** — Plain domain classes. No framework dependencies.
- **AppName.<Service>.Api.Contracts** — Request and response DTOs (contracts) of the HTTP API.
- **AppName.<Service>.Persistence** — Repository interfaces only (`Abstractions/`). No database code.
<!-- @module:auth -->
- `AppName.<Service>.Persistence.<Db>` — The repository implementations for the chosen database.
- **AppName.<Service>.Security** — Only in the service that issues the tokens: JWT access tokens, refresh
  token hashes, password hashing and token revocation.
<!-- @module:permissions -->
  It also holds the setup codes and the permission authorization handler.
<!-- @end -->
<!-- @end -->
- **AppName.<Service>.Application** — MediatR commands, queries and handlers with the business logic,
  `BusinessErrors` and `ValidationBehavior`. `IMapper<TSource, TDestination>` maps domain objects to
  contracts.
- **AppName.<Service>.Api** — ASP.NET Core minimal API host. Endpoints are discovered through `IEndpoint`.
  `Infrastructure/` holds the parts that only this service uses, for example the authorization rules.

## Key patterns

**Result pattern**: Use `Result.Success(value)` / `Result.Failure(error)`, or return the value or the
error directly (implicit conversions). Chain with `.Map()`, `.Bind()`, `.Ensure()`, `.Match()`.

**Errors**: A handler returns an error from `BusinessErrors`. Never create an error inline
(`new ConflictError(...)`) in a handler. Add it to `AppName.<Service>.Application/Errors/BusinessErrors.cs`.
`ErrorResultExtensions` maps the error type to a ProblemDetails response:

| Error | HTTP |
|---|---|
| `ValidationError` | 400 |
| `ForbiddenError` | 403 |
| `NotFoundError` | 404 |
| `ConflictError` | 409 |
| `ServiceUnavailableError` | 503 |
| any other | 500 |

`type` is the error name, `detail` is the message, and `details` holds the optional details object.
For a validation failure, `type` is the error code of the failed rule when exactly one rule failed
(`WithErrorCode(...)`, or the FluentValidation default such as `NotEmptyValidator`), and
`ValidationFailed` when more rules failed. `details` then maps each property to its messages.
An unhandled exception gives 500 ProblemDetails. Only the Development environment shows its message.

**Validation**: FluentValidation. `ValidationBehavior` runs every `IValidator<TRequest>` before the
handler and returns a failed `Result` / `Result<T>` with a `ValidationError`. Validators are found by
assembly scan (`ApplicationAssembly`). Put each validator in its own `<Request>Validator.cs` file next
to the request (`SignInCommandValidator`, `GetForecastQueryValidator`). Start each rule with
`.Cascade(CascadeMode.Stop)` when a later check needs a value, so a missing value gives 400 and not a
`NullReferenceException`.

**Endpoints**: Implement `IEndpoint` in a class under `AppName.<Service>.Api/Endpoints/`. `AddEndpoints` /
`UseEndpoints` find it by reflection. Use a route group `v{v:apiVersion}/<resource>` with
`.WithApiVersionSet(builder.CreateDefaultApiVersionSet())` and `.WithTags(...)`. Each route has
`.Produces<...>()`, `.WithSummary(...)` and `.WithDescription(...)`. Handlers are private static methods
that send a command or query and return `result.ToResponse()` or `result.Match(...)`.

```csharp
// AppName.<Service>.Api/Endpoints/ForecastsEndpoints.cs
public sealed class ForecastsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("v{v:apiVersion}/forecasts")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Forecasts");

        group.MapGet("{city}", GetForecastAsync)     // relative to the group prefix
            .AllowAnonymous()                        // the one authorization rule of the route
            .Produces<ForecastResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithSummary("Get forecast")
            .WithDescription("Returns the forecast of the city.");
    }

    private static async Task<IResult> GetForecastAsync(string city, ISender sender, CancellationToken cancellationToken) =>
        (await sender.Send(new GetForecastQuery(city), cancellationToken)).ToResponse();
}
```

The `RouteMap` key of this route is `"GET v{v:apiVersion}/forecasts/{city}"` (no leading slash, the
version stays a template). A route on the group root has a trailing slash: `"POST v{v:apiVersion}/accounts/"`.

**API versioning**: URL segment (`/v1/...`). Add a new version set in
`AppName.Api.Infrastructure/ApiVersioningHelper.cs`.

**OpenAPI and Scalar**: `/openapi/v1.json` and Scalar at `/docs` on each service. The build writes
`src/openapi-<service>.json` (build-time document generation). Code that must not run during that generation checks
`isOpenApiDocumentGeneration` in `Program.cs`.

**MediatR license**: MediatR 13 and later is commercial (Lucky Penny Software; a free community license
exists for small companies). Without a key it logs a license warning at start. Set the key with the
`MediatR__LicenseKey` environment variable (`MediatR:LicenseKey`); never commit it.

**Logging**: Serilog to the console. Log with message templates, never with string interpolation.
<!-- @module:auth -->
Security changes log one line `SecurityEvent {Event}: ...` (see `.agents/PERMISSIONS.md`).
<!-- @end -->

**Options**: Bind every options class with `AddOptions<T>().Bind(...).Validate(...).ValidateOnStart()`.
`AddApiDefaults` binds the shared options (`Cors`, `Collection`, `ForwardedHeaders`). `Program.cs` calls `IStartupValidator.Validate()` right after `Build()`, so a bad setting stops the
server before it changes data.

**Collections**: Return `CollectionResponse<T>` for a list. A request that returns a dataset uses cursor
pagination (`cursor`, `limit`, and `orderBy` when it sorts). `CollectionSettings` limits the page size.

## Request flow

Use this flow as the template for a new endpoint:

| Layer | Role | Location |
|---|---|---|
| Endpoint | HTTP route, binds parameters, sends the request, maps the result | `AppName.<Service>.Api/Endpoints/{Feature}Endpoints.cs` |
| Request contract | DTO from the client | `AppName.<Service>.Api.Contracts/{Feature}/` |
| Response contract | DTO to the client | `AppName.<Service>.Api.Contracts/{Feature}/` |
| Command / Query | MediatR request record, `IRequest<Result<T>>` | `AppName.<Service>.Application/{Feature}/Commands/{Name}/` or `Queries/{Name}/` |
| Handler | Business logic, calls repositories, maps to the contract | same file as the request |
| Validator | `AbstractValidator<TRequest>`, omit when there is nothing to check | `{Request}Validator.cs` in the same folder |
| Repository interface | Data access contract | `AppName.<Service>.Persistence/Abstractions/` |
| Repository implementation | Database code | `AppName.<Service>.Persistence.<Db>/` |
| Domain model | Plain C# class | `AppName.<Service>.Domain/{Feature}/` |
| Mapper | `IMapper<Domain, Contract>`; **register it by hand in `Program.cs`** | `AppName.<Service>.Application/{Feature}/Mapping/` |
<!-- @module:auth -->

`GET /v1/accounts/me` (`AccountsEndpoints` → `GetAccountQuery` → `IAccountRepository` →
`AccountResponseMapper`) in the auth service is a complete example in the code.
<!-- @end -->

```
HTTP request
  -> Endpoint
    -> sender.Send(query)
      -> ValidationBehavior (runs IValidator<TQuery>)
      -> Handler -> repository -> domain -> mapper -> Result<Response>
    -> result.ToResponse()
      -> success: 200 OK + JSON (or 204 for a plain Result)
      -> failure: ProblemDetails (400 / 403 / 404 / 409 / 500 / 503)
```

## Verification

- After a change, run `dotnet build AppName.slnx`. It is the source of truth, not the IDE analyzer.
- Run `dotnet test AppName.slnx` for a change in behavior.

## Common gotchas

- Every new route needs exactly one authorization rule and an entry in `RouteMap` in
  `tests/AppName.<Service>.Api.IntegrationTests/Security/EndpointAuthorizationCoverageTests.cs`.
  In a service without authentication, the rule is `.AllowAnonymous()`. The map stays the reviewed list
  of public routes.
<!-- @module:auth -->
  See `.agents/PERMISSIONS.md`.
<!-- @end -->
- New `IMapper<T, R>` implementations must be registered by hand in `AppName.<Service>.Api/Program.cs`.
- Always use `Guid` for entity keys (`Guid.CreateVersion7()` for new ids).
- Use `CollectionResponse<T>` when returning collections.
- Never instantiate errors inline. Add them to `BusinessErrors`.
- Keep the layers separate: no business logic in an endpoint, no database code in Application.
- Package versions live in `Directory.Packages.props`. A csproj `PackageReference` has no version.
