# Setup record

`.agents/setup.json` in the solution folder records how `scaffold.cs` made the solution. The skills
`add-dotnet-api-service` and `update-dotnet-api` read it. `scaffold.cs add` and `render` accept only
format 2.

## Format 2

```json
{
  "kit": "nkryp-ai-kit",
  "kitVersion": "1.4.0",
  "format": 2,
  "name": "Acme.Shop",
  "arch": "vsa",
  "solutionPath": "backend",
  "modules": ["docker"],
  "services": [
    { "name": "Admin", "port": 5070, "modules": ["auth", "permissions"] },
    { "name": "Web", "port": 5071, "modules": ["trust"] }
  ]
}
```

| Field | Meaning |
|---|---|
| `kitVersion` | The kit version of the templates that the solution matches. |
| `name` | The solution name. A service project is `src/<name>.<service>.Api`. |
| `arch` | `clean` or `vsa`. |
| `solutionPath` | The path from the root folder to the solution folder, with `/`, or `.` when they are the same folder. The root folder holds the root agent files: `AGENTS.md`, `CLAUDE.md` and `.agents/INSTRUCTIONS.md`. A record without this field means `.`. |
| `modules` | Solution modules: `docker`. |
| `services[].modules` | `auth` (own accounts, issues tokens), `trust` (accepts the tokens of an auth service), `permissions` and `signup` (both need `auth`). |
| `services[].port` | The HTTP port in development. |

## Get a format-2 record

Read `.agents/setup.json`. When it has `"format": 2`, use it. Settings from a format-2 record need no
confirmation.

Otherwise (format 1 has no `format` and no `services`, and an old solution has no file), detect the
settings from the solution:

| Setting | Rule |
|---|---|
| `name` | The file name of `*.slnx` in the solution folder, without the extension. |
| `arch` | `clean` when a folder `src/<name>.*Application/` exists. Otherwise `vsa`. |
| Layout | Multi-service when the API projects are `src/<name>.<Service>.Api/`. Single-service when the API project is `src/<name>.Api/` (see below). |
| Services | One per project `src/<name>.<Service>.Api/<name>.<Service>.Api.csproj` with `Sdk="Microsoft.NET.Sdk.Web"`. Other projects (shared projects, workers, tools) are not services. |
| `auth` | The service's `appsettings.json` has `Jwt:AccessTokenExpiresIn`. |
| `trust` | The service's `appsettings.json` has a `Jwt` section without `AccessTokenExpiresIn`. |
| `permissions` | A file `InsufficientPermissionsDetails.cs` exists in a project of the service. |
| `signup` | The service's `appsettings.json` has `"AllowUserRegistration": true`. |
| `port` | The port of the `http` profile in the service's `Properties/launchSettings.json`. |
| `docker` | `compose.yaml` exists in the solution folder. |
| `solutionPath` | `.`. Older kits put all agent files in the solution folder. |
| `kitVersion` | The value of the old `setup.json`, else `unknown`. |

Show the detected record to the user: the name, the architecture, and one line for each service with
its port and modules. Ask the user to confirm it or to correct it. Then write it to
`.agents/setup.json` in format 2.

**Single-service layout.** A solution made by kit 1.3.0 or older has `src/<name>.Api/` and keeps all
API infrastructure in that project. The current templates cannot render it. Stop and tell the user:
to use these skills, the solution needs the multi-service layout. That means:

1. Rename the service projects to `<name>.<Service>.*`.
2. Move the shared API code (error handling, OpenAPI, versioning, hosting, pagination) into
   `<name>.Api.Infrastructure`.

Offer to make this change as a separate task with its own plan.

Completion criterion: `.agents/setup.json` has format 2, and the user confirmed every detected value.
