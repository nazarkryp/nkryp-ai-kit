# nkryp-ai-kit

Skills, workflows and tools for AI-assisted development with Claude Code, Codex and other coding
agents.

The repository is also a Claude Code and Codex plugin marketplace, `nkryp-ai-kit`. It holds one plugin with the
same name, and the plugin holds all skills.

## Contents

| Skill | What it does |
|---|---|
| [`setup-dotnet-api`](skills/setup-dotnet-api/SKILL.md) | Creates a new ASP.NET Core (.NET 10) API solution from templates. |
| [`add-dotnet-api-service`](skills/add-dotnet-api-service/SKILL.md) | Adds an API service to a generated solution: its projects, auth, database, port and Docker entry. |
| [`update-dotnet-api`](skills/update-dotnet-api/SKILL.md) | Brings the agent docs and root config of a generated solution up to the latest templates. |
| [`dispatch-agents`](skills/dispatch-agents/SKILL.md) | Splits work into tracks for subagents. The lead decides the approach and writes high-risk tasks; workers write the rest. |
| [`dispatch-agents-light`](skills/dispatch-agents-light/SKILL.md) | Variant for small or mechanical work: workers choose the approach, a reviewer checks high-risk tasks. Manual only. |
| [`dispatch-agents-pro`](skills/dispatch-agents-pro/SKILL.md) | Variant for long plans and risky work: the lead only decides and reviews, workers write all code. Manual only. |
| [`mockups`](skills/mockups/SKILL.md) | Builds one HTML page that shows the current UI and three design options as working copies, before the app code changes. |

## Install

### Claude Code

```bash
claude plugin marketplace add nazarkryp/nkryp-ai-kit
claude plugin install nkryp-ai-kit@nkryp-ai-kit
```

To install from a local clone, give the path to the clone instead of `nazarkryp/nkryp-ai-kit`.

Update after a new version:

```bash
claude plugin marketplace update nkryp-ai-kit
claude plugin update nkryp-ai-kit@nkryp-ai-kit
```

### Codex

```bash
codex plugin marketplace add nazarkryp/nkryp-ai-kit
codex plugin add nkryp-ai-kit@nkryp-ai-kit
```

Update after a new version:

```bash
codex plugin marketplace upgrade nkryp-ai-kit
```

### One skill, or other agents

Install one skill into Claude Code, Codex or another agent with [skills.sh](https://skills.sh):

```bash
npx skills add nazarkryp/nkryp-ai-kit --skill setup-dotnet-api
```

## Layout

```
.claude-plugin/              Claude Code plugin.json, marketplace.json (Codex reads the marketplace too)
.codex-plugin/               Codex plugin.json
skills/<skill>/              one folder for each skill
AGENTS.md                    instructions for agents that work on this repository
CLAUDE.md                    imports AGENTS.md for Claude Code
```

## Contribute

- Read `AGENTS.md`. It has the rules for agents and people.
- Increase `version` in `.claude-plugin/plugin.json` and `.codex-plugin/plugin.json` for every change.
  An installed copy does not see a change without a new version.

## setup-dotnet-api

The skill creates a new ASP.NET Core (.NET 10) API solution with its first API service. A solution can
hold more services: they share `<Name>.Common` and `<Name>.Api.Infrastructure`, and each service has
its own projects `<Name>.<Service>.*`. The templates include: Result pattern,
ProblemDetails errors, Scalar and OpenAPI, URL versioning, Serilog, MediatR and FluentValidation, and
optional JWT accounts, roles and permissions.

### Use

Ask your agent to create a new .NET API solution. In Claude Code, you can also run
`/nkryp-ai-kit:setup-dotnet-api`. The skill asks for the name, the architecture (Clean or vertical
slices), the first service name, the modules (authentication, permissions, self-registration, Docker)
and the database. Then it generates and verifies the solution.

The root agent files (`AGENTS.md`, `CLAUDE.md`, `.agents/INSTRUCTIONS.md`) hold the general rules for
agents. When the solution folder is inside a larger git repository, the skill asks whether these files
go to the repository root or to the solution folder. It never overwrites a root file that exists: it
merges the kit's text into it with your approval. The solution's own docs (`.agents/SOLUTION.md`,
`.agents/BACKEND.md`) always stay in the solution folder.

### Add a service

Ask your agent to add an API service to the solution, or run `/nkryp-ai-kit:add-dotnet-api-service`.
The skill asks for the service name, its auth (own accounts, the tokens of the auth service, or none),
its database and its port. It adds the service projects and the shared files that are missing, and it
merges the changes to `compose.yaml` and the agent docs with your approval. A solution has at most one
service with own accounts, because all services share one signing key.

Prerequisite: the .NET 10 SDK.

### Update a generated solution

The solution keeps its settings in `.agents/setup.json`. Keep this file in the repository. After you
update the plugin, ask your agent to update the solution to the latest kit, or run
`/nkryp-ai-kit:update-dotnet-api`. The skill renders the latest templates with the same settings and
proposes changes to the agent docs and the root config files, one file at a time. It keeps your own
text and never changes `src/` or `tests/`. A solution without `setup.json`, or with the format of kit
1.3.0 or older, works too: the skill detects the settings and asks you to confirm them (see
`references/SETUP-RECORD.md`).

### Layout

```
skills/setup-dotnet-api/
  SKILL.md                   the steps the agent follows
  scaffold.cs                renders the templates: new, add (a service), render (from setup.json)
  references/DATABASE.md     the contract for the database step
  references/SETUP-RECORD.md the format of .agents/setup.json and how to detect it
  templates/root/            root agent files: AGENTS.md, CLAUDE.md, .agents/INSTRUCTIONS.md
  templates/shared/          solution files that both architectures use
  templates/<arch>/solution/ shared projects of the solution (clean or vsa)
  templates/<arch>/service/  the projects of one service (clean or vsa)
```

### Maintain the templates

The templates are normal .NET projects with placeholders: `AppName` (and `appname` in lowercase) for
the solution name, in service templates `ServiceName` (`servicename`) and `ServicePort`, and in root
templates `SolutionPath/`: the path from the root folder to the solution folder (`backend/`), or
nothing when they are the same folder. `scaffold.cs` renders three kinds of templates:

- **Root templates**: `templates/root/`, rendered once into the root folder (`--root`, default the
  solution folder). A root file that exists is not overwritten: `new` writes the kit's version to
  `--staging` and the agent merges it. `render` keeps the layout: the root files in `--out`, the
  solution in `--out/<solutionPath>`.

- **Solution templates**: `templates/shared/` and `templates/<arch>/solution/`, rendered once. Put a
  file that is the same for both architectures in `shared/`. A path must not be in `shared/` and in
  `<arch>/solution/`: `scaffold.cs` stops with an error.
- **Service templates**: `templates/<arch>/service/`, rendered once for each service.

`AppName.slnx` lists no projects: `scaffold.cs` adds each rendered `.csproj`.

Module markers decide what `scaffold.cs` keeps:

| Marker | Meaning |
|---|---|
| `// @file-module:auth` on the first line | The whole file belongs to the module. (`#` and `<!-- -->` work too.) |
| `// @module:auth` ... `// @end` | Keep the lines only when the module is on. Regions can be nested. |
| `// @module:!permissions` ... `// @end` | Keep the lines only when the module is off. |
| `# @each-service` ... `# @end` | Solution templates only: repeat the lines for each service, with the service placeholders and modules. |

Modules: `auth`, `trust`, `permissions` (needs `auth`), `signup` (needs `auth`) for each service;
`docker` for the solution; `jwt` is on when a service has `auth` or `trust`. A solution or root template sees
a module as on when any service has it. A marker must be alone on its line. JSON files can hold `//`
marker lines, because the scaffold removes them.

Keep module code out of regions in the shared projects (`templates/<arch>/solution/src/`): put it in
whole files with `@file-module`, or in the service. When `add` turns a module on, it adds the missing
whole files to the user's shared projects; it never edits an existing file there.

Anchors `[setup:...]` mark the places that the agent fills during the setup (database, project and
service descriptions).

After a template change, generate and test the combinations for both architectures (write the output
outside this repository):

```bash
S=<scratch folder>
dotnet run skills/setup-dotnet-api/scaffold.cs -- new --name Sample --out $S/s1 --arch clean --service Main
dotnet run skills/setup-dotnet-api/scaffold.cs -- new --name Sample --out $S/s2 --arch clean --service Admin --auth
dotnet run skills/setup-dotnet-api/scaffold.cs -- new --name Sample --out $S/s3 --arch clean --service Admin --auth --permissions --signup --docker
dotnet run skills/setup-dotnet-api/scaffold.cs -- add --solution $S/s3 --service Web --trust --staging $S/s3-staging
dotnet run skills/setup-dotnet-api/scaffold.cs -- new --name Sample --out $S/s4 --arch clean --service Main
dotnet run skills/setup-dotnet-api/scaffold.cs -- add --solution $S/s4 --service Admin --auth --staging $S/s4-staging
dotnet run skills/setup-dotnet-api/scaffold.cs -- new --name Sample --out $S/s5/backend --root $S/s5 --arch clean --service Main
# the same with --arch vsa
```

Core-only solutions and trust services build and test as they are. A service with `--auth` needs a
database implementation before its integration tests run (see `references/DATABASE.md`).
