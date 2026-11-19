# nkryp-ai-kit

A kit of skills, workflows and tools for AI-assisted development with Claude Code, Codex and other
coding agents. The repo is also a plugin marketplace (`nkryp-ai-kit`) for Claude Code
(`.claude-plugin/`) and Codex (`.codex-plugin/`; Codex reads `.claude-plugin/marketplace.json`).

## Rules

- Write each skill so that any agent can run it. Name a tool of one agent (for example
  `AskUserQuestion`) only together with what to do in an agent that does not have it.
- Before you edit a `SKILL.md` or another document that an agent reads, load the `writing-for-agents`
  skill when your agent has it.
- Increase `version` in `.claude-plugin/plugin.json` and `.codex-plugin/plugin.json` for every change
  to the plugin. Keep the two files the same in name, version and description. An installed copy
  does not see a change without a new version.

## Skills

### update-dotnet-api and add-dotnet-api-service

`update-dotnet-api` reviews a solution made by `setup-dotnet-api` against the latest templates.
`add-dotnet-api-service` adds a service to it. Both depend on `scaffold.cs` and on `.agents/setup.json`,
which `scaffold.cs` writes. When you add or rename a `scaffold.cs` command, option or module, update the
detection table in `skills/setup-dotnet-api/references/SETUP-RECORD.md` and the commands in both
skills too.

### dispatch-agents

`dispatch-agents`, `dispatch-agents-light` and `dispatch-agents-pro` are three variants of one skill.
The light and pro variants replace the base skill for the session. Keep their shared parts the same in
all three files: the track rules, the brief fields, the report shape and the "Other agents" block.
A change to one of these parts is a change to all three files.

### setup-dotnet-api

Creates a new ASP.NET Core (.NET 10) API solution from templates, with one or more API services.

- Everything under `skills/setup-dotnet-api/templates/` is template output, not instructions for this
  repo. `templates/shared/` holds the files that are the same for both architectures, for example
  `.agents/SOLUTION.md`. Edit them there, not in `clean/` or `vsa/`. `templates/root/` holds the
  root agent files (`AGENTS.md`, `CLAUDE.md`, `.agents/INSTRUCTIONS.md`). The `.md` files in
  `templates/` go into the generated solution or its root folder. Edit them as template content.
- Before you edit a template, read "Maintain the templates" in `README.md`. It defines the `AppName`
  and `SolutionPath/` placeholders, the module markers that `scaffold.cs` reads, and the `[setup:...]` anchors.

Verify a template change: generate the module combinations for both architectures with the commands
in `README.md`, then run `dotnet build` and `dotnet test` in each output folder. Write the output to
a temporary folder outside this repo.

Completion criterion: every combination builds, and every core-only service and every trust service
passes its tests. A service with `--auth` needs a database implementation before its integration tests
run (see `skills/setup-dotnet-api/references/DATABASE.md`).
