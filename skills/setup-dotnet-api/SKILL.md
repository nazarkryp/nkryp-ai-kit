---
name: setup-dotnet-api
description: Create a new ASP.NET Core (.NET 10) API solution with Result pattern, ProblemDetails errors, Scalar, Serilog, MediatR and FluentValidation, and optional JWT accounts, roles and permissions. Use when the user wants to start, scaffold or bootstrap a new .NET API solution or backend.
---

# Set up a .NET API solution

This skill creates a new solution from proven templates in `templates/shared/` and `templates/<arch>/`
next to this file.
`scaffold.cs` copies them, keeps only the chosen modules, and renames `AppName` to the solution name.
You then add the parts that need a decision: the database and the project description.

The user decides every choice below. Ask, then act.

Prerequisite: the .NET 10 SDK (`dotnet --list-sdks` shows a `10.0.x`). `scaffold.cs` is a file-based app
and needs it.

## 1. Ask the setup questions

Ask with `AskUserQuestion` (at most 4 questions per call; put the recommended option first and add
"(Recommended)"). Ask in two rounds, because round 2 depends on round 1.

Round 1:

| Question | Options |
|---|---|
| Solution name and folder | PascalCase name, dots allowed (`Acme.Shop`). Default folder: `./<Name>` in the current directory. The folder must not exist or must be empty. |
| Architecture | **Clean Architecture** (layer projects: Common, Domain, Api.Contracts, Persistence, Security, Application, Api) or **Vertical slices** (Common, Domain, and one Api project with `Features/<Area>/<UseCase>/` slices). Both use MediatR, Result and repository interfaces. |
| Authentication and accounts | **Yes**: JWT access token + rotating refresh token in an HttpOnly cookie, sign-in, sign-out, change password, account registration endpoint. **No**: no auth, no accounts, no database. |
| Docker | **Yes**: Dockerfile, `.dockerignore`, `compose.yaml` with a data volume. **No**. |

Round 2, only when authentication is yes:

| Question | Options |
|---|---|
| Permissions and roles | **Yes**: roles, direct grants, a permission catalog, admin account and role management, setup codes, the first account becomes Admin. **No**: every signed-in account is equal. Without permissions and without self-registration, only the first account can exist (a single-user app). |
| Self-registration | **Off** (Recommended): only the first account registers itself; later accounts come from an admin. **On**: anyone can create an account. It only sets `AccountSettings:AllowUserRegistration`. |
| Database | The user names the database (for example SQLite, PostgreSQL, SQL Server). Ask about the data access library too (EF Core is the default for a relational database). |

Completion criterion: you have the name, folder, architecture, and every module answer, and the
database when authentication is yes.

## 2. Generate the solution

```bash
dotnet run "${CLAUDE_PLUGIN_ROOT}/skills/setup-dotnet-api/scaffold.cs" -- --name <Name> --out <folder> --arch <clean|vsa> [--auth] [--permissions] [--signup] [--docker]
```

When the skill is not installed as a plugin, use the base directory of this skill instead of
`${CLAUDE_PLUGIN_ROOT}/skills/setup-dotnet-api`. `--signup` turns self-registration on. The script
prints the enabled modules and the file count. It exits with an error and writes nothing when an
argument or a template is wrong; fix the cause, then run it again.

Completion criterion: the script printed the summary, and the folder has `<Name>.slnx`.

## 3. Add the database (authentication only)

Read [`references/DATABASE.md`](references/DATABASE.md) and follow it for the chosen database. It lists the
repository contract, the guarantees each method must keep, the anchors to fill in `Program.cs` and in
the test factory, and the docs to update.

Completion criterion: a search for `[setup:persistence` and `[setup:database-commands` in the solution
finds nothing, and a search for `<Db>` in `.agents/` finds nothing.

## 4. Fill in the project docs

- `.agents/INSTRUCTIONS.md`: replace the `[setup:project-description]` comment with two or three
  sentences about the project. Ask the user what the project does when you do not know it.
- `.agents/INSTRUCTIONS.md`: replace the `[setup:database]` comment with one bullet, for example
  `- SQLite with EF Core`. Without a database, delete the comment.

Completion criterion: a search for `[setup:` in the solution finds nothing.

## 5. Verify

Run, in the solution folder:

1. `dotnet build <Name>.slnx`: no errors, and no `NU1901`-`NU1904` vulnerability warnings.
2. `dotnet test <Name>.slnx`: every test passes.
3. Start the API in the background (`dotnet run --project src/<Name>.Api`, it listens on
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

Tell the user: the folder, the architecture and modules, the database, the test totals, how to run
the API, and the known gaps listed at the end of `.agents/PERMISSIONS.md` (authentication only).
Say that `.agents/setup.json` records the setup: keep it in the repository, because the
`update-dotnet-api` skill uses it to bring the solution up to newer templates.
