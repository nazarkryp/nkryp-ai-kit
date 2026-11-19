---
name: add-dotnet-api-service
description: Add an API service to an existing .NET solution made by setup-dotnet-api, with its auth (own accounts, the tokens of the auth service, or none), database, port and Docker entry. Use when the user wants to add another API, service or project to a .NET API solution.
---

# Add a service to a .NET API solution

A solution made by `setup-dotnet-api` holds shared projects (`<Name>.Common`, `<Name>.Api.Infrastructure`,
and `<Name>.Api.Authentication` when a service checks tokens) and one set of projects for each service
(`<Name>.<Service>.Api` and its layers). This skill adds one service: it renders the service templates,
adds missing shared files, and merges the shared files that change with the user's approval.

**Ownership.** The new service's files are yours to write. The existing code in `src/` and `tests/`
belongs to the user: change it only with the user's approval. The config files in the solution folder and
`.agents/*.md` are kit-owned: change them only with the user's approval for each file.

Ask each question with `AskUserQuestion` (at most 4 questions per call, the recommended option first
with "(Recommended)"). When your agent has no `AskUserQuestion` tool, ask in the chat: number the
options, put the recommended option first, then stop and wait for the answer.

Prerequisite: the .NET 10 SDK (`dotnet --list-sdks` shows a `10.0.x`).

## 1. Read the setup record

The script is `${CLAUDE_PLUGIN_ROOT}/skills/setup-dotnet-api/scaffold.cs`. When the kit is not installed
as a plugin, use `../setup-dotnet-api/scaffold.cs` next to the base directory of this skill. When neither
exists, stop and tell the user to install the `setup-dotnet-api` skill.

The solution folder holds `*.slnx`. Get a format-2 setup record as
[`../setup-dotnet-api/references/SETUP-RECORD.md`](../setup-dotnet-api/references/SETUP-RECORD.md) says.

Completion criterion: `.agents/setup.json` has format 2, with every existing service in it.

## 2. Ask the service questions

| Question | Options |
|---|---|
| Service name | One PascalCase word that no service has yet, for example `Web`. The projects are `<Name>.<Service>.Api` and so on. |
| Auth | **Tokens of `<auth service>`** (`trust`): the service has no accounts and accepts the access tokens of the auth service. Offer it only when a service has `auth`. **Own accounts** (`auth`): offer it only when no service has `auth`, because all services share one signing key. **None**. |
| Database | **None**. **Own database on the existing server**: offer it when `compose.yaml` or a connection string shows a database server. **Another database**: the user names it. |
| Port | The highest port in the setup record + 1 (Recommended), or a number the user names. Check that no `launchSettings.json` and no `compose.yaml` port of the solution uses it. |

With **Own accounts**, ask a second round: permissions and roles, and self-registration, with the
options of `setup-dotnet-api` step 1.

Completion criterion: you have the service name, its modules, the database answer, and a free port.

## 3. Render the service

Run, with a new empty staging folder outside the solution (the session's scratchpad when your agent has
one):

```bash
dotnet run <scaffold.cs> -- add --solution <solution folder> --service <Service> --staging <empty folder> --port <port> [--auth | --trust] [--permissions] [--signup]
```

The script writes the service projects, adds the shared files that the solution does not have, adds the
new projects to the `.slnx`, and updates `.agents/setup.json`. It prints "Shared files to merge". It
writes nothing when a service file already exists or an argument is wrong; fix the cause, then run it
again.

Completion criterion: the script printed its summary.

## 4. Merge the shared files

For each file under "Shared files to merge", compare `<staging>/old/<file>` with `<staging>/new/<file>`.
The difference is what the new service needs, for example its `compose.yaml` entry and volume, or its
bullet in `.agents/SOLUTION.md`. Apply that difference to the solution's file and keep everything
else in the solution's file. A file with no `old/` copy is new for this solution: compare it with the
solution's file.

Ask for each file with two options, "Apply (Recommended)" and "Skip", with the change in the `preview`
of "Apply".

Completion criterion: each listed file is applied or skipped by the user.

## 5. Connect the auth (trust only)

The new service accepts a token only when its `Jwt:Issuer`, `Jwt:Audience` and signing key match the
auth service. Read the `Jwt` section of the auth service's `appsettings.json` and
`appsettings.Development.json`. When a value differs from the new service's, set the auth service's
value in the new service. The key file (`Jwt:SecretFile`) must point to the same file in development,
and both services must mount the same key volume in `compose.yaml`.

Completion criterion: the issuer, the audience and the key location of the new service equal the auth
service's.

## 6. Add the database

With a database, follow [`../setup-dotnet-api/references/DATABASE.md`](../setup-dotnet-api/references/DATABASE.md)
for the new service.

Completion criterion: the completion criterion of `DATABASE.md` holds for the new service.

## 7. Fill in the docs

In `.agents/SOLUTION.md`, section Services, the bullet of the new service: replace the
`[setup:service-description]` comment with one sentence about the service (ask the user when you do not
know it), and the `[setup:service-database]` comment with one bullet, for example
`- Database: none`.

Completion criterion: a search for `[setup:` in the new service's files and in `.agents/` finds nothing.

## 8. Verify

Run, in the solution folder:

1. `dotnet build <Name>.slnx`: no errors, and no `NU1901`-`NU1904` vulnerability warnings.
2. `dotnet test <Name>.slnx`: every test passes.
3. Start the new service in the background (`dotnet run --project src/<Name>.<Service>.Api`), check
   `http://localhost:<port>/health` (200) and `/docs` (200 after a redirect), then stop it. Write logs
   to the session's scratchpad.
4. Trust only: start the auth service first, sign in as in `setup-dotnet-api` step 5, and call
   `GET http://localhost:<port>/v1/me` with the access token (200). Without the token it returns 401.

A solution that the user changed by hand can have shared projects that differ from the templates, so
the new service may call a shared method that the solution does not have. Change the new service's code
to use what the shared projects have. Change a shared project only with the user's approval.

Completion criterion: steps 1-3 (and 4 for trust) pass in one run, after your last change.

## 9. Report

Tell the user: the service, its projects, modules, port and database, the shared files added, applied
and skipped, the test totals, and how to run the service. For trust, say that a signed-out access token
stays valid in the new service until it expires, because the new service cannot read the revocation
list of the auth service.
