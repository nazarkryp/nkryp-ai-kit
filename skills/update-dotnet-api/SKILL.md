---
name: update-dotnet-api
description: Review a solution made by setup-dotnet-api against the latest kit templates and update its agent docs and root config files. Use after the nkryp-ai-kit plugin is updated, or when the user asks whether a .NET API solution matches the latest kit conventions.
---

# Update a .NET API solution to the latest kit

The `setup-dotnet-api` skill copies its templates once. This skill renders the latest templates with the
same settings and brings the **kit-owned files** of the solution up to date.

**Kit-owned files** are all files in the solution folder except `src/`, `tests/`, `*.slnx` and
`.agents/setup.json`. They are the agent docs (`CLAUDE.md`, `AGENTS.md`, `.agents/*.md`) and the
root config files. You change kit-owned files only, and only with the user's approval for each file.
The code in `src/` and `tests/` belongs to the user. You never change it.

Ask each question with `AskUserQuestion` (at most 4 questions per call). When your agent has no
`AskUserQuestion` tool, ask in the chat: number the options, put the recommended option first, then
stop and wait for the answer.

## 1. Find the setup settings

Read `.agents/setup.json` in the solution folder. It holds `kitVersion`, `name`, `arch` and `modules`.

When the file does not exist, the solution is older than the setup record. Detect the settings:

| Setting | Rule |
|---|---|
| `name` | The file name of `*.slnx` in the solution folder, without the extension. |
| `arch` | `clean` when `src/<name>.Application/` exists. Otherwise `vsa` (a new vsa solution has no `Features/` folder yet). |
| `auth` | `.agents/PERMISSIONS.md` exists. |
| `permissions` | A file `InsufficientPermissionsDetails.cs` exists under `src/`. |
| `signup` | `src/<name>.Api/appsettings.json` has `"AllowUserRegistration": true`. |
| `docker` | `compose.yaml` exists in the solution folder. |

Show the detected settings to the user and ask them to confirm. Treat `kitVersion` as `unknown`.
Settings read from `setup.json` need no confirmation.

Completion criterion: you have `name`, `arch` and the module list, read from `setup.json` or confirmed
by the user.

## 2. Render the latest templates

The script is `${CLAUDE_PLUGIN_ROOT}/skills/setup-dotnet-api/scaffold.cs`. When the kit is not
installed as a plugin, use `../setup-dotnet-api/scaffold.cs` next to the base directory of this skill.
When neither exists, stop and tell the user to install the `setup-dotnet-api` skill.

Render into a new, empty folder outside the solution (the session's scratchpad when your agent has one):

```bash
dotnet run <scaffold.cs> -- --name <name> --out <empty folder> --arch <arch> [--auth] [--permissions] [--signup] [--docker]
```

Read `kitVersion` from the rendered `.agents/setup.json`. When it equals the solution's `kitVersion`,
tell the user that the solution is up to date and stop, unless the user asked for a full compare.

Completion criterion: the script printed its summary, and you know the old and the new kit version.

## 3. Compare the kit-owned files

Compare each kit-owned file of the rendered folder with the same file in the solution. Note each file
that is new, changed, or missing in the rendered folder. Line endings are not a difference: compare
with them ignored (for example `diff --strip-trailing-cr`), and keep the line endings of the solution
file when you write it.

Each difference in a changed file is one of two kinds:

- **Kit change**: the template is different now. Take the rendered text.
- **User text**: text that the user or the setup agent wrote. Keep it. User text includes:
  - text in place of a `[setup:...]` comment, and a database name in place of `<Db>`;
  - rules, sections, packages and settings that the user added;
  - a package version that is higher than the rendered one.

When you cannot tell the kind of a difference, show the passage and ask the user.

Completion criterion: each difference in each kit-owned file is a kit change, user text, or a question
for the user.

## 4. Update with approval

For each file with a kit change, write the proposed result: the rendered file with the user text put
back. Ask for each file with two options: "Apply (Recommended)" and "Skip". Put a short list of the
kit changes in the `preview` of the "Apply" option. Ask about a file that is missing in the rendered
folder (the kit no longer has it) with the options "Keep (Recommended)" and "Delete".

Write the approved files. Then set `kitVersion` in `.agents/setup.json` to the new version. When the
file does not exist, create it with the settings from step 1.

When an applied file is not a `.md` file, run `dotnet build <name>.slnx` and `dotnet test <name>.slnx`
in the solution folder. A failure caused by your change is your bug: fix it, or restore the file and
tell the user.

Completion criterion: each file with a kit change is applied or skipped by the user, `setup.json` holds
the new version, and the build and the tests pass when they ran.

## 5. Report

Tell the user: the old and the new kit version, the applied files, the skipped files, the questions that
are still open, and the build and test result. Say that `src/` and `tests/` were not reviewed.
