---
name: dispatch-agents
description: Dispatch subagents — the lead decides each task's approach and writes high-risk tasks itself, workers write the rest. Use before an Agent call, and when a plan or a set of failures splits into parts that can run in parallel.
---

# Dispatch agents

You are the **lead**. You decide the plan, the approach of each task, the debugging strategy, and the `blocked` choices. You write the high-risk tasks and investigate unknown causes yourself. **Workers** write all other code.

When a plan gives an Approach line for each task, use it. Otherwise write the Approach line of each task yourself before you dispatch.

A worker knows only its brief and the instruction files it loads. You write all other context.

## 1. Split the work into tracks

A **track** is one unit of work with one goal and its own files.

- Group by cause, not by symptom. Failures that can share a cause are one track: a fix for one can fix the others.
- When the cause is unknown, investigate it yourself. Split the fix after you know the cause.
- Read-only tracks (search, research, review) can run in parallel with any track.
- Give each writing track an **owner list**: the files only it may change. Two tracks never share a file. Your own high-risk tasks count as tracks.
- Give each shared resource to one track at a time: a build unit (one project, package, or app), a shared generated file (a migration snapshot, a lock file, a generated client), and shared runtime state (a database, a dev-server port, a test container). Two tracks on one resource collide.
- A track that needs the result of another track waits until that track reports.
- Do a track yourself when its brief costs more than the work.
- Workers work in the shared working tree. A worktree starts from the default branch and does not have uncommitted work.

Done when: each track has a goal, an owner list, and a decided approach, and no two tracks share a resource.

## 2. Write the brief

- **Target**: one outcome with an observable done condition ("`GET /items/{id}` returns 404 for an unknown id", "the 3 failing tests in `<test file>` pass").
- **Approach**: the exact change: the existing code to copy as the example, where the logic goes, and the decisions that apply. When the worker meets a choice that the approach does not cover, it stops and reports `blocked` with the options.
- **Facts**: paste what the worker cannot find fast: error text, test names, the agreed contract, paths with line numbers. The worker did not see your conversation.
- **Owner list**: the files it may change. All other files are read-only.
- **Hard rules**: copy the rules that the work touches (for example: no git commands, no new package). `Explore` agents do not load the instruction files.
- **Cheap fix**: name the fix that hides the symptom and ask for the cause ("Fix the race. A longer timeout is not a fix.").
- **Check**: the build or test command that the project instructions give for the files it changes. The worker runs it before it reports.
- **Report**: this shape, at most 150 words:

```text
Result: done | partial | blocked
Cause: <one sentence; fix tasks only>
Changed: <path:lines — what changed, one line per range>
Checked: <command> -> <result>
Open: <questions, options for a blocked choice, and risks, or "none">
```

## 3. Choose the worker

High-risk tasks (auth, security, delete, data migration, file system writes, concurrency) and investigations of unknown causes: do them yourself. For all other tracks, set `model` and `effort` on every call:

| Track | Type | Model | Effort |
|---|---|---|---|
| Find code or a fact; summarize a file | Explore | haiku | low |
| Run a build, tests, or screenshots and summarize the output | general-purpose | haiku | low |
| Mechanical edit: rename, move, repeat a pattern | general-purpose | sonnet | low |
| Bounded change with a decided approach | general-purpose | sonnet | medium; high when the change has real logic |

Use `fork` only when the context is too large to write down: it runs on your model, so it costs the most.

**Other agents.** `Explore`, `fork`, `SendMessage`, `model`, `effort` and the haiku, sonnet and opus models are Claude Code features. In an agent without them:

- Model and effort: use the agent's cheapest model for haiku, its middle model for sonnet, and its strongest model for opus. When the agent has no model choice, use its default subagent for every row.
- `Explore`: use the default subagent, and write "change no files" in the brief.
- `fork`: write the context into the brief.
- `SendMessage` (section 4): start a new subagent with the old brief, its report, and the missing fact or decision.
- No subagents: do the tracks yourself, one at a time, in the order of section 1.

Before dispatch, show the user one line per track: goal, owner list, model, effort. Show your own tracks as "lead".

## 4. Integrate and verify

1. Read each report. A report is a claim: confirm the one fact your next change depends on.
2. Read the changed line ranges that the report names. Compare them with the Approach line. Read a whole file only when a range does not explain the change.
3. Compare the changed files with each owner list. Resolve every file outside its list before you continue.
4. Look for one error repeated in many files. Workers copy a wrong pattern consistently.
5. For `blocked`: decide a small choice inside the approved design, and send the decision to the same worker with `SendMessage`. For a change of behaviour or scope, ask the user. For `partial`: send the missing fact to the same worker.
6. On a design error, or the same kind of bug twice, send the task to an opus worker (high) with a corrected brief.
7. After all tracks are in, a haiku worker runs the project's full verification once and reports the result.

Done when: every report is read, every change is inside its owner list, the full run is green, and the user has one line per track: result, model, effort.
