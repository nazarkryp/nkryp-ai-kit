# nkryp-ai-kit

Skills, workflows and tools for AI-assisted development with Claude Code, Codex and other coding
agents.

The repository is also a Claude Code and Codex plugin marketplace, `nkryp-ai-kit`. It holds one plugin with the
same name, and the plugin holds all skills.

## Contents

| Skill | What it does |
|---|---|
| [`setup-dotnet-api`](skills/setup-dotnet-api/SKILL.md) | Creates a new ASP.NET Core (.NET 10) API solution from templates. |

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

The skill creates a new ASP.NET Core (.NET 10) API solution. The templates include: Result pattern,
ProblemDetails errors, Scalar and OpenAPI, URL versioning, Serilog, MediatR and FluentValidation, and
optional JWT accounts, roles and permissions.

### Use

Ask your agent to create a new .NET API solution. In Claude Code, you can also run
`/nkryp-ai-kit:setup-dotnet-api`. The skill asks for the name, the architecture (Clean or vertical
slices), the modules (authentication, permissions, self-registration, Docker) and the database. Then
it generates and verifies the solution.

Prerequisite: the .NET 10 SDK.

### Layout

```
skills/setup-dotnet-api/
  SKILL.md                   the steps the agent follows
  scaffold.cs                copies a template, keeps the chosen modules, renames AppName
  references/DATABASE.md     the contract for the database step
  templates/clean/           Clean Architecture template
  templates/vsa/             vertical slice template
```

### Maintain the templates

The templates are normal .NET solutions with the placeholder `AppName` (and `appname` for lowercase
names). Module markers decide what `scaffold.cs` keeps:

| Marker | Meaning |
|---|---|
| `// @file-module:auth` on the first line | The whole file belongs to the module. (`#` and `<!-- -->` work too.) |
| `// @module:auth` ... `// @end` | Keep the lines only when the module is on. Regions can be nested. |
| `// @module:!permissions` ... `// @end` | Keep the lines only when the module is off. |

Modules: `auth`, `permissions` (needs `auth`), `signup` (needs `auth`), `docker`. A marker must be alone
on its line. JSON files can hold `//` marker lines, because the scaffold removes them.

Anchors `[setup:...]` mark the places that the agent fills during the setup (database, project
description).

After a template change, generate and test the combinations for both architectures:

```bash
dotnet run skills/setup-dotnet-api/scaffold.cs -- --name Sample --out /tmp/s1 --arch clean
dotnet run skills/setup-dotnet-api/scaffold.cs -- --name Sample --out /tmp/s2 --arch clean --auth
dotnet run skills/setup-dotnet-api/scaffold.cs -- --name Sample --out /tmp/s3 --arch clean --auth --permissions --signup --docker
# the same with --arch vsa
```

Core-only solutions build and test as they are. A solution with `--auth` needs a database
implementation before its integration tests run (see `references/DATABASE.md`).
