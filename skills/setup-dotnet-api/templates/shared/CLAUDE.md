@.agents/INSTRUCTIONS.md

## MODEL USE

- Keep planning, architecture, debugging strategy, integration decisions, and final review in the starting reasoning model (aka agent).
- Dispatch only bounded implementation tasks with an already-decided design to cheaper models.
- Every worker task must state the target, exact change, constraints, file ownership, and observable acceptance criteria.
- Do not delegate ambiguous debugging, authentication, security, migrations, concurrency, or cross-layer design to a cheaper model.
- Prefer a cheaper model with medium or high effort. Use low effort only for mechanical edits.
- Do not accept a worker report as proof. The starting model must inspect the diff and run or verify the relevant tests.
- If the first review finds a design error or repeated correctness problems, stop using the cheap worker and dispatch the task to a stronger model.
- Never let parallel workers edit the same files or shared state.
- Report in chat subagent model name and effort.
