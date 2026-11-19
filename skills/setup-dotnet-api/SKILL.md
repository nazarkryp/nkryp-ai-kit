---
name: setup-dotnet-api
description: Create a new ASP.NET Core (.NET 10) API solution (one or more API services) with Result pattern, ProblemDetails errors, Scalar, Serilog, MediatR and FluentValidation, and optional JWT accounts, roles and permissions. Use when the user wants to start, scaffold or bootstrap a new .NET API solution or backend.
---

# Set up a .NET API solution

This skill creates a new solution with its first API service from the templates in `templates/` next
to this file. `scaffold.cs` copies them, keeps only the chosen modules, and renames `AppName` to the
solution name and `ServiceName` to the service name. You then add the parts that need a decision: the
database and the project description.

The solution holds shared projects (`<Name>.Common`, `<Name>.Api.Infrastructure`) and one set of
projects for each service (`<Name>.<Service>.Api` and its layers). To add a service to an existing
solution, use the `add-dotnet-api-service` skill instead.

The user decides every choice below. Ask, then act.

Prerequisite: the .NET 10 SDK (`dotnet --list-sdks` shows a `10.0.x`). `scaffold.cs` is a file-based app
and needs it.

## 1. Ask the setup questions

Ask with `AskUserQuestion` (at most 4 questions per call; put the recommended option first and add
"(Recommended)"). When your agent has no `AskUserQuestion` tool, ask in the chat: number the options,
put the recommended option first, then stop and wait for the answer. Ask in two rounds, because round 2
depends on round 1.

Round 1:

| Question | Options |
|---|---|
| Solution name and folder | PascalCase name, dots allowed (`Acme.Shop`). Default folder: `./<Name>` in the current directory. The folder must not exist or must be empty. |
| Architecture | **Clean Architecture** (layer projects for each service: Domain, Api.Contracts, Persistence, Security, Application, Api) or **Vertical slices** (Domain and one Api project with `Features/<Area>/<UseCase>/` slices for each service). Both use MediatR, Result and repository interfaces. |
| Authentication and accounts | **Yes**: the first service gets JWT access tokens + rotating refresh token in an HttpOnly cookie, sign-in, sign-out, change password, account registration endpoint. Later services can accept its tokens. **No**: no auth, no accounts, no database. |
| Docker | **Yes**: a Dockerfile for each service, `.dockerignore`, `compose.yaml` with one data volume for each service. **No**. |

Round 2:

| Question | Options |
|---|---|
| First service name | One PascalCase word, for example `Admin` or `Web`. The projects are `<Name>.<Service>.Api` and so on. Its port in development is 5070. |
| Permissions and roles (authentication only) | **Yes**: roles, direct grants, a permission catalog, admin account and role management, setup codes, the first account becomes Admin. **No**: every signed-in account is equal. Without permissions and without self-registration, only the first account can exist (a single-user app). |
| Self-registration (authentication only) | **Off** (Recommended): only the first account registers itself; later accounts come from an admin. **On**: anyone can create an account. It only sets `AccountSettings:AllowUserRegistration`. |
| Database (authentication only) | The user names the database (for example SQLite, PostgreSQL, SQL Server). Ask about the data access library too (EF Core is the default for a relational database). |

Then find the root folder. It holds the root agent files: `AGENTS.md`, `CLAUDE.md` and
`.agents/INSTRUCTIONS.md`, with the general rules for agents. Look for a `.git` entry (a folder or a
file) in the solution folder and in each of its parent folders, with a file search. When the nearest
`.git` is in a parent folder, ask:

| Question | Options |
|---|---|
| Root agent files | **Repository root** (Recommended): Codex reads `AGENTS.md` files only from the repository root down to the folder where it starts, so a session started at the repository root does not see these files in the solution folder. **Solution folder**: all agent files stay in the solution folder. |

Otherwise the root folder is the solution folder.

Completion criterion: you have the name, folder, architecture, the first service name, every module
answer, the database when authentication is yes, and the root folder.

## 2. Generate the solution

```bash
dotnet run "${CLAUDE_PLUGIN_ROOT}/skills/setup-dotnet-api/scaffold.cs" -- new --name <Name> --out <folder> --arch <clean|vsa> --service <Service> [--auth] [--permissions] [--signup] [--docker] [--root <root folder> --staging <empty folder>]
```

When the skill is not installed as a plugin, use the base directory of this skill instead of
`${CLAUDE_PLUGIN_ROOT}/skills/setup-dotnet-api`. `--signup` turns self-registration on. When the root
folder is not the solution folder, pass `--root` and a new empty `--staging` folder outside the
repository (the session's scratchpad when your agent has one). The script prints the enabled modules
and the file count. It exits with an error and writes nothing when an argument or a template is wrong;
fix the cause, then run it again. It writes `.agents/setup.json` (see
[`references/SETUP-RECORD.md`](references/SETUP-RECORD.md)).

The script writes a root file only when the root folder does not have it. It lists each root file that
exists under "Root files to merge" and writes the kit's version to `<staging>/new/<file>`. For each of
these files, add the kit's text that the root file does not have yet, and keep the rest of the root
file. In `.agents/INSTRUCTIONS.md`, the lines that point to this solution's `.agents/` files are the
minimum. Ask for each file with two options, "Apply (Recommended)" and "Skip", with the change in the
`preview` of "Apply".

Completion criterion: the script printed the summary, the folder has `<Name>.slnx`, and each root file
to merge is applied or skipped by the user.

## 3. Add the database (authentication only)

Read [`references/DATABASE.md`](references/DATABASE.md) and follow it for the chosen database and the
auth service. It lists the
repository contract, the guarantees each method must keep, the anchors to fill in `Program.cs` and in
the test factory, and the docs to update.

Completion criterion: a search for `[setup:persistence` and `[setup:database-commands` in the solution
finds nothing, and a search for `<Db>` in `.agents/` finds nothing.

## 4. Fill in the project docs

- `.agents/SOLUTION.md`: replace the `[setup:project-description]` comment with two or three
  sentences about the project. Ask the user what the project does when you do not know it.
- `.agents/SOLUTION.md`, section Services: replace the `[setup:service-description]` comment with one
  sentence about the service, and the `[setup:service-database]` comment with one bullet, for example
  `- Database: SQLite with EF Core` or `- Database: none`.

Completion criterion: a search for `[setup:` in the solution finds nothing.

## 5. Verify

Run, in the solution folder:

1. `dotnet build <Name>.slnx`: no errors, and no `NU1901`-`NU1904` vulnerability warnings.
2. `dotnet test <Name>.slnx`: every test passes.
3. Start the API in the background (`dotnet run --project src/<Name>.<Service>.Api`, it listens on
   `http://localhost:5070`), run the checks below, then stop the process. Write logs to the session's
   scratchpad, not to `/tmp`.

   ```bash
   curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/health        # 200
   curl -sL -o /dev/null -w "%{http_code}\n" http://localhost:5070/docs         # 200 (after a redirect)
   ```

   With authentication (the access token lives 60 s, so call `/me` right after the sign-in):

   ```bash
   curl -s -X POST http://localhost:5070/v1/accounts -H "Content-Type: application/json" \
     -d '{"username":"firstuser","emailAddress":"first@example.test","password":"Sup3r!Secret"}'   # 201
   TOKEN=$(curl -s -X POST http://localhost:5070/v1/accounts/signin -H "Content-Type: application/json" \
     -d '{"username":"firstuser","password":"Sup3r!Secret"}' | sed -E 's/.*"access_token":"([^"]+)".*/\1/')
   curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" http://localhost:5070/v1/accounts/me   # 200
   ```

   The password rules need upper case, lower case, a digit and a special character. The token JSON uses
   snake_case (`access_token`). The refresh token is only in the `refresh_token` cookie.
4. With a database: start the API a second time against the same database. It starts without an error.
5. With Docker: `docker compose build` succeeds. When Docker is not running, say so in the report.

Expected log noise, not a failure: "Failed to determine the https port" (the http profile has no https
port), and, in a container, the DataProtection key warnings.

A failure is a bug in your step 3 or 4 code, or in the templates. Fix the cause and run the step again.
When the cause is in the templates, tell the user which template file is wrong.

Completion criterion: steps 1-3 (and 4 and 5 when they apply) pass in one run, after your last change.

## 6. Report

Tell the user: the folder, the root folder and the root files that were written, merged or skipped,
the architecture, the service and its modules, the database, the test
totals, how to run the API, and the known gaps listed at the end of `.agents/PERMISSIONS.md`
(authentication only). Say that `.agents/setup.json` records the setup: keep it in the repository,
because the `add-dotnet-api-service` skill uses it to add a service and the `update-dotnet-api` skill
uses it to bring the solution up to newer templates.
