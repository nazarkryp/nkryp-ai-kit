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

### setup-dotnet-api

Creates a new ASP.NET Core (.NET 10) API solution from templates.

- Everything under `skills/setup-dotnet-api/templates/` is template output, not instructions for this
  repo. The `CLAUDE.md`, `AGENTS.md` and `.agents/*.md` files there go into the generated solution.
  Edit them as template content.
- Before you edit a template, read "Maintain the templates" in `README.md`. It defines the `AppName`
  placeholder, the module markers that `scaffold.cs` reads, and the `[setup:...]` anchors.

Verify a template change: generate the module combinations for both architectures with the commands
in `README.md`, then run `dotnet build` and `dotnet test` in each output folder. Write the output to
a temporary folder outside this repo.

Completion criterion: every combination builds, and every core-only combination passes its tests. A
combination with `--auth` needs a database implementation before its integration tests run (see
`skills/setup-dotnet-api/references/DATABASE.md`).
