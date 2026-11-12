# Database step

The templates have repository interfaces and no database code. This step adds one implementation for
the database that the user chose. Do it after `scaffold.cs` ran.

## 1. Read the contract

Read every file in the repository interface folder. The doc comments are the contract:

| Architecture | Interfaces | Domain classes |
|---|---|---|
| Clean | `src/<Name>.Persistence/Abstractions/` | `src/<Name>.Domain/Accounts/` |
| Vertical slices | `src/<Name>.Api/Persistence/Abstractions/` | `src/<Name>.Domain/Accounts/` |

The guarantees below are the ones that are easy to get wrong. Each one is a *must*.

Every project with authentication:

- **Unique usernames**: a unique index that compares without case. `TryAddAsync` returns `false` when
  the index refuses the insert (catch the unique-violation error of the database, nothing else).
- **Refresh token rotation** (`IRefreshTokenRepository.RotateAsync`, `DeleteAsync`): delete the old row
  and insert the new one in one transaction. Report `false` when the delete removed no row. Only token
  hashes are stored; never add a column for the plain token.
- **Change password** (`IAccountRepository.ChangePasswordAsync`): in one transaction, write only the
  password hash, and delete every refresh token of the account except the kept hash.

Only with the permissions module (the interfaces below exist only then):

- **Atomic last admin** (`IAccountRepository.TrySetRoleAsync`): the check "this is the last account with
  the Admin role" and the update are one atomic statement. Two parallel demotions of the last two admins
  must not both succeed.
- **Atomic first admin** (`IAccountRepository.EnsureAdminExistsAsync`): "give the Admin role to the
  oldest account when no account has it" is one atomic statement. Store the insert time in the
  persistence entity (a `CreatedAt` column; the domain class does not need it) and order by it, then by id.
- **Setup codes** (`IAccountSetupCodeRepository`): one open code per account (a new code replaces the old
  one). `TryCompleteSetupAsync` deletes the code with a guarded delete (account id, hash, not expired)
  and, in the same transaction, sets the password hash and deletes every refresh token of the account.
  Of two parallel requests with one code, only one gets `true`. `ChangePasswordAsync` also deletes the
  open setup code.
- **Role and grants writes**: `TryAddAsync` writes the role and the grants of a new account. After that,
  only `TrySetRoleAsync` and `ReplaceGrantsAsync` change them.
- **Unique role names**: like usernames. `IRoleRepository.TryAddAsync` returns `false` and `UpdateAsync`
  returns `NameTaken` when the index refuses the name.
- **Admin role row**: the `Admin` role (`Role.AdminId`, `Role.AdminName`, no stored permissions) exists in
  every database. Seed it in the schema migration (for example EF `HasData`).
- **Stored permission codes** stay plain strings. A code that is no longer in `Permissions.All` must not
  break a read; `EffectivePermissions` ignores it.

Case-insensitive comparison: a database collation often folds only ASCII letters (SQLite `NOCASE` does).
Usernames are ASCII by their rule, but role names can hold any letter. Store a normalized column
(`ToUpperInvariant()`) with the unique index when the collation is not enough.

## 2. Create the implementation

Location:

- Clean: a project `src/<Name>.Persistence.<Db>/` (for example `<Name>.Persistence.Sqlite`), referenced
  by `<Name>.Api`. Add it to `<Name>.slnx`.
- Vertical slices: a folder `src/<Name>.Api/Persistence/<Db>/` in the Api project.

Inside, use this layout: `<Name>DbContext.cs`, `Entities/` (persistence entities with `ToDomain()` /
`ToEntity()`), the repositories, `ServiceCollectionExtensions.cs`, and `Migrations/`. Make every class
`internal` except the two extension methods below.

Packages: add versions to `Directory.Packages.props` only (a `PackageReference` has no version). Use
the newest stable release that supports `net10.0` and has no known vulnerability. Find it with
`curl -s https://api.nuget.org/v3-flatcontainer/<package id lowercase>/index.json`.

Data access:

- Relational database: EF Core with migrations. Map the domain classes through persistence entities, so
  the domain stays free of EF attributes. Express each atomic guarantee as one `ExecuteUpdateAsync` /
  `ExecuteDeleteAsync` statement, or as raw SQL when LINQ cannot express it as one statement. Filter and
  order on entities first, then project; a projection into a record followed by `Where` does not translate.
- Another database: follow its official .NET client. Keep the same guarantees.

Write two public extension methods:

- `AddPersistence(this IServiceCollection, IConfiguration)`: registers the context and every repository.
  Read the connection string from `ConnectionStrings:<Name>` and validate it with `ValidateOnStart`, so
  a missing value stops the server with a clear message. It must not open a connection: it also runs
  during the build-time OpenAPI generation.
- `InitializePersistenceAsync(this IServiceProvider)`: creates what the database needs (for a file
  database, its folder), applies the migrations, and seeds what the migration cannot.

EF Core tooling:

```bash
dotnet new tool-manifest                                  # once, in the solution folder
dotnet tool install dotnet-ef --version <EF Core version>
dotnet restore
dotnet ef migrations add Initial --project <persistence project> --output-dir Migrations
```

- Add `Microsoft.EntityFrameworkCore.Design` to the project that holds the migrations, with
  `PrivateAssets="all"`.
- Add an `IDesignTimeDbContextFactory<TContext>` next to the context, with a fixed local connection
  string. Then `dotnet ef` does not start `Program.cs` (which needs the JWT settings and creates the key file).
- `dotnet ef database update --connection "<value>"` needs an absolute path for a file database.

## 3. Fill the anchors

| Anchor | File | Replace with |
|---|---|---|
| `[setup:persistence-services]` | `src/<Name>.Api/Program.cs` | `builder.Services.AddPersistence(builder.Configuration);` and its `using` |
| `[setup:persistence-initialize]` | `src/<Name>.Api/Program.cs` | `await app.Services.InitializePersistenceAsync();` |
| `[setup:persistence-test]` | `tests/<Name>.Api.IntegrationTests/Infrastructure/ApiFactory.cs` | A separate database for each test class (see below). |
| `[setup:database-commands]` | `.agents/BACKEND.md` | The commands to add a migration and to apply it. |

Test database for each test class:

- File database: a file in `Paths.DataDirectory`. Set the connection string in `settings`. The
  `TestConfiguration.PathKeys` guard checks plain paths only; for a connection string, parse its file
  path in `CreateHost` (for example `SqliteConnectionStringBuilder.DataSource`) and check that it is under
  `Paths.Root`.
- Server database: a container (Testcontainers) or a unique database name. Drop it when the factory is
  disposed.

Connection strings: `appsettings.json` gets the production value. Use a relative path for a file
database (`Data Source=data/<name>.db`); it resolves to `/app/data` in the container and to the API
project folder in development. A server database needs a password: read it from an environment
variable (`ConnectionStrings__<Name>`), never from a committed file.

With Docker and a server database, add the database service to `compose.yaml` with a named volume, and
pass the connection string to the API as an environment variable.

## 4. SQLite

- A transaction that reads first and writes later can fail at once with `SQLITE_BUSY`. Start each
  transaction with its guarded write (`ExecuteDeleteAsync` / `ExecuteUpdateAsync`), then read.
- Turn on WAL (`PRAGMA journal_mode=WAL;`) in `InitializePersistenceAsync`.
- EF Core stores `Guid` as upper-case text and `DateTime` without a kind. Add a value converter that
  reads every `DateTime` as UTC.
- After an `ExecuteUpdateAsync` in a scope, a tracked entity of the same row is stale. Clear the change
  tracker or read without tracking.
- In the test factory, call `SqliteConnection.ClearAllPools()` in `Dispose` before the temp folder is
  deleted. Otherwise the file stays open.

## 5. Test the guarantees

Add `tests/<Name>.Api.IntegrationTests/Persistence/` with tests that run against the real database
(build a `ServiceCollection` with `AddPersistence` and a temp database):

- `TryAddAsync` with a taken username (any case) returns `false`.
- Two parallel `RotateAsync` calls with one token: exactly one returns `true`.
- A second `InitializePersistenceAsync` on the same database succeeds.
- With permissions: two parallel `TryCompleteSetupAsync` calls with one code (one `true`), parallel
  `EnsureAdminExistsAsync` calls on accounts without an admin (exactly one admin, the oldest), and the
  last-admin demotion race (one success).

## 6. Update the docs

- `.agents/INSTRUCTIONS.md`: the `[setup:database]` comment becomes one bullet under Tech.
- `.agents/BACKEND.md`: replace every `<Db>` with the real name (Clean: the project
  `<Name>.Persistence.<Db>`; vertical slices: the folder `Persistence/<Db>/`), and fill
  `[setup:database-commands]`.

Completion criterion: the build and every test pass, and a second start of the API against the same
database succeeds.
