## Commands

```bash
dotnet build AppName.slnx
dotnet test AppName.slnx
dotnet run --project src/AppName.Api      # Scalar docs: http://localhost:5070/docs
```
<!-- @module:auth -->

`dotnet run --project` uses `src/AppName.Api` as the working folder, so the local data (`data/`, for
example the JWT key file) is in `src/AppName.Api/data/`.

<!-- [setup:database-commands] The database step adds the commands to add and apply a migration here. -->
<!-- @end -->
<!-- @module:docker -->

```bash
docker compose up --build                 # API on http://localhost:8080, data in ./data
```
<!-- @end -->

## Architecture

Vertical slices with CQRS through MediatR. One slice is one folder with one use case. The projects:

```
Api → Common, Domain
```

- **AppName.Common** — `Result` / `Result<T>` and `Error`. Every application operation returns a `Result`.
- **AppName.Domain** — Plain domain classes. No framework dependencies.
- **AppName.Api** — ASP.NET Core minimal API host with every slice. Its folders:
  - `Features/<Area>/<UseCase>/` — the slices. See "Slices" below.
  - `Features/<Area>/Shared/` — code that several slices of the area use: response contracts, mappers,
    validation rules, settings and helpers.
  - `Common/` — `ValidationBehavior`, `IMapper<TSource, TDestination>`, `CollectionResponse<T>`,
    `PageResponse<T>`, `PaginationParameters`, and `Errors/`: the error types (`NotFoundError`,
    `ForbiddenError`, `ConflictError`, `ValidationError`, `ServiceUnavailableError`) and `BusinessErrors`.
  - `Persistence/Abstractions/` — Repository interfaces only. `Persistence/Common/` — `CollectionResult<T>`.
<!-- @module:auth -->
  - `Persistence/<Db>/` — The repository implementations for the chosen database.
<!-- @end -->
  - `Infrastructure/` — Error mapping, OpenAPI, versioning, options and endpoint discovery.
<!-- @module:auth -->
  - `Infrastructure/Security/` — JWT access tokens, refresh token hashes, password hashing and token
    revocation.
<!-- @module:permissions -->
    It also holds the setup codes and the permission authorization handler.
<!-- @end -->
<!-- @end -->

A slice may use `Common/`, `Persistence/Abstractions/`, `Infrastructure/`, `Domain`, the route group
of its area and the `Shared/` folders. A slice never uses another slice. When two slices need the same
code, move it to the `Shared/` folder of their area.

## Slices

A slice folder `Features/<Area>/<UseCase>/` holds:

| File | Content |
|---|---|
| `<UseCase>Endpoint.cs` | A class that implements `IEndpoint` and maps exactly one route on the area group |
| `<UseCase>Request.cs` | The request contract, when the route has a body |
| `<UseCase>Command.cs` / `<UseCase>Query.cs` | The MediatR request record and its handler |
| `<Request>Validator.cs` | `AbstractValidator<TRequest>` named after the request (`SignInCommandValidator`, `GetForecastQueryValidator`); omit when there is nothing to check |
| `<UseCase>Response.cs` | The response contract, when only this slice returns it |

Each area has one `<Area>Routes.cs` with a `Map<Area>Group()` extension. It holds the group settings in one
place: the route prefix `v{v:apiVersion}/<resource>`, `.WithApiVersionSet(builder.CreateDefaultApiVersionSet())`
and `.WithTags(...)`. The endpoint calls the extension, then maps its route with `.Produces<...>()`,
`.WithSummary(...)`, `.WithDescription(...)` and its authorization rule. The handler of the route is a
private static method that sends the command or query and returns `result.ToResponse()` or
`result.Match(...)`.

Example: a slice `GET /v1/forecasts/{city}` in a new area `Forecasts`.

```csharp
// Features/Forecasts/ForecastsRoutes.cs
public static class ForecastsRoutes
{
    public static RouteGroupBuilder MapForecastsGroup(this IEndpointRouteBuilder builder) =>
        builder.MapGroup("v{v:apiVersion}/forecasts")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Forecasts");
}

// Features/Forecasts/GetForecast/GetForecastEndpoint.cs
public sealed class GetForecastEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapForecastsGroup()
            .MapGet("{city}", GetForecastAsync)      // relative to the group prefix
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

## Key patterns

**Result pattern**: Use `Result.Success(value)` / `Result.Failure(error)`, or return the value or the
error directly (implicit conversions). Chain with `.Map()`, `.Bind()`, `.Ensure()`, `.Match()`.

**Errors**: A handler returns an error from `BusinessErrors`. Never create an error inline
(`new ConflictError(...)`) in a handler. Add it to `AppName.Api/Common/Errors/BusinessErrors.cs`.
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
assembly scan (`typeof(Program).Assembly`). Put each validator in its own `<Request>Validator.cs` file
in the slice folder. Start each rule with `.Cascade(CascadeMode.Stop)` when a later check needs a value,
so a missing value gives 400 and not a `NullReferenceException`.

**Endpoints**: `AddEndpoints` / `UseEndpoints` find every `IEndpoint` by reflection. One endpoint class
maps one route.

**API versioning**: URL segment (`/v1/...`). Add a new version set in `ApiVersioningHelper`.

**OpenAPI and Scalar**: `/openapi/v1.json` and Scalar at `/docs`. The build writes `src/openapi.json`
(build-time document generation). Code that must not run during that generation checks
`isOpenApiDocumentGeneration` in `Program.cs`.

**MediatR license**: MediatR 13 and later is commercial (Lucky Penny Software; a free community license
exists for small companies). Without a key it logs a license warning at start. Set the key with the
`MediatR__LicenseKey` environment variable (`MediatR:LicenseKey`); never commit it.

**Logging**: Serilog to the console. Log with message templates, never with string interpolation.
<!-- @module:auth -->
Security changes log one line `SecurityEvent {Event}: ...` (see `.agents/PERMISSIONS.md`).
<!-- @end -->

**Options**: Bind every options class with `AddOptions<T>().Bind(...).Validate(...).ValidateOnStart()`.
`Program.cs` calls `IStartupValidator.Validate()` right after `Build()`, so a bad setting stops the
server before it changes data.

**Collections**: Return `CollectionResponse<T>` for a list. A request that returns a dataset uses cursor
pagination (`cursor`, `limit`, and `orderBy` when it sorts). `CollectionSettings` limits the page size.

## Request flow

Use this flow as the template for a new endpoint:

| Part | Role | Location |
|---|---|---|
| Route group | Prefix, version set, tags of the area | `AppName.Api/Features/{Area}/{Area}Routes.cs` |
| Endpoint | HTTP route, binds parameters, sends the request, maps the result | `AppName.Api/Features/{Area}/{UseCase}/{UseCase}Endpoint.cs` |
| Request contract | DTO from the client | `{UseCase}Request.cs` in the slice folder |
| Response contract | DTO to the client | the slice folder, or `Features/{Area}/Shared/` when several slices return it |
| Command / Query | MediatR request record, `IRequest<Result<T>>` | `{UseCase}Command.cs` or `{UseCase}Query.cs` in the slice folder |
| Handler | Business logic, calls repositories, maps to the contract | same file as the request |
| Validator | `AbstractValidator<TRequest>`, omit when there is nothing to check | `{Request}Validator.cs` in the slice folder |
| Repository interface | Data access contract | `AppName.Api/Persistence/Abstractions/` |
| Repository implementation | Database code | `AppName.Api/Persistence/<Db>/` (the database folder) |
| Domain model | Plain C# class | `AppName.Domain/{Area}/` |
| Mapper | `IMapper<Domain, Contract>`; **register it by hand in `Program.cs`** | `Features/{Area}/Shared/` |
<!-- @module:auth -->

`GET /v1/accounts/me` (`Features/Accounts/GetAccount/`: `GetAccountEndpoint` → `GetAccountQuery` →
`IAccountRepository` → `Features/Accounts/Shared/AccountResponseMapper`) is a complete example in the code.
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
  `tests/AppName.Api.IntegrationTests/Security/EndpointAuthorizationCoverageTests.cs`.
<!-- @module:!auth -->
  Without authentication, the rule is `.AllowAnonymous()`. The map stays the reviewed list of public routes.
<!-- @end -->
<!-- @module:auth -->
  See `.agents/PERMISSIONS.md`.
<!-- @end -->
- New `IMapper<T, R>` implementations must be registered by hand in `AppName.Api/Program.cs`.
- Always use `Guid` for entity keys (`Guid.CreateVersion7()` for new ids).
- Use `CollectionResponse<T>` when returning collections.
- Never instantiate errors inline. Add them to `BusinessErrors`.
- No business logic in an endpoint: it binds, sends and maps the result. No database code in a slice:
  the handler calls a repository interface.
- A slice folder name is also a namespace. When it has the name of a type, the other slices of the area
  see the namespace first. Use an alias for the type.
<!-- @module:auth -->
  Example: `Accounts/RefreshToken/` and the domain class `RefreshToken` →
  `using DomainRefreshToken = AppName.Domain.Accounts.RefreshToken;`.
<!-- @end -->
- The endpoints map in reflection order, so the order of the routes in `openapi.json` does not follow
  the folders.
- Package versions live in `Directory.Packages.props`. A csproj `PackageReference` has no version.
