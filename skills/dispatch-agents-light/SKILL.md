---
name: dispatch-agents-light
description: Light dispatch — workers choose the approach inside their task, sonnet writes all code, a reviewer checks high-risk tasks. For small or mechanical work.
disable-model-invocation: true
---

# Dispatch agents — light

This skill replaces `dispatch-agents` for the rest of the session. Where a skill or instruction names `dispatch-agents`, use this skill. When a plan gives an Approach line for a task, copy it into the brief's facts.

A subagent knows only its brief and the instruction files it loads. You write all other context.

## 1. Split the work into tracks

A **track** is one unit of work with one goal and its own files.

- Group by cause, not by symptom. Failures that can share a cause are one track: a fix for one can fix the others.
- When the cause is unknown, run one investigation track first. Split after you know the cause.
- Read-only tracks (search, research, review) can run in parallel with any track.
- Give each writing track an **owner list**: the files only it may change. Two tracks never share a file.
- Run at most one writing track per build unit (one project, package, or app) at a time. Two tracks in one build unit write the same build output.
- Give each shared generated file (a migration snapshot, a lock file, a generated client) to one track.
- A track that needs the result of another track waits until that track reports.
- Do a track yourself when its brief costs more than the work.
- Agents work in the shared working tree. A worktree starts from the default branch and does not have uncommitted work.

Done when: each track has a goal and an owner list, and no two writing tracks share a file.

## 2. Write the brief

- **Goal**: one outcome with a done condition the agent can check ("the 3 failing tests in `<test file>` pass").
- **Facts**: paste what the agent cannot find fast: error text, test names, the agreed contract, paths with line numbers. The agent did not see your conversation.
- **Owner list**: the files it may change. All other files are read-only.
- **Hard rules**: copy the rules that the work touches (for example: no git commands, no new package). `Explore` agents do not load the instruction files.
- **Cheap fix**: name the fix that hides the symptom and ask for the cause ("Fix the race. A longer timeout is not a fix.").
- **Check**: the build or test command that the project instructions give for the files it changes. The agent runs it before it reports.
- **Report**: this shape, at most 150 words:

```text
Result: done | partial | blocked
Cause: <one sentence; fix tasks only>
Changed: <path — what changed, one line per file>
Checked: <command> -> <result>
Open: <questions and risks, or "none">
```

## 3. Choose agent type, model, and effort

Set `model` and `effort` on every call. Use the cheapest row that fits the track:

| Track | Type | Model | Effort |
|---|---|---|---|
| Find code or a fact | Explore | haiku | low |
| Run a build, tests, or screenshots and summarize the output | general-purpose | haiku | low |
| Change code inside an owner list | general-purpose | sonnet | medium |
| Review a high-risk task: auth, delete, data migration, file system writes | general-purpose | sonnet | high |
| Unknown cause, design choices, many layers | general-purpose | opus | high |

Use the last row only when the track needs it. Use `fork` only when the context is too large to write down: it runs on your model, so it costs the most.

**Other agents.** `Explore`, `fork`, `SendMessage`, `model`, `effort` and the haiku, sonnet and opus models are Claude Code features. In an agent without them:

- Model and effort: use the agent's cheapest model for haiku, its middle model for sonnet, and its strongest model for opus. When the agent has no model choice, use its default subagent for every row.
- `Explore`: use the default subagent, and write "change no files" in the brief.
- `fork`: write the context into the brief.
- `SendMessage` (section 4): start a new subagent with the old brief, its report, and the missing fact or decision.
- No subagents: do the tracks yourself, one at a time, in the order of section 1.

Before dispatch, show the user one line per track: goal, owner list, model, effort.

## 4. Integrate and verify

1. Read each report. A report is a claim: confirm the one fact your next change depends on.
2. Read the changed lines yourself.
3. Compare the changed files with each owner list. Resolve every file outside its list before you continue.
4. Look for one error repeated in many files. Agents copy a wrong pattern consistently.
5. For `partial` or `blocked`, continue the same agent with `SendMessage` and the missing fact. A new agent starts from zero.
6. After all tracks are in, run the project's full verification once, as the project instructions describe.

Done when: every report is read, every change is inside its owner list, the full run is green, and the user has one line per track with its result.
