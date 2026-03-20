# AI Agent Quickstart (Backend)

This is the simple guide for how to use commands, skills, and subagents in this repo.

## Big Picture

- Commands (`/...`) are the fastest way to start a task.
- Skills are reusable playbooks the agent uses to do specific work safely.
- Subagents are specialized workers for build, test, migration, and API review.
- `AGENTS.md` is always the source of truth for repo rules.

## What to Use First

For most tasks, follow this default flow:

1. Run one task command (`/feature-workflow`, `/fix-build`, `/fix-test`, or `/migration-check`).
2. Run `/preflight`.
3. Run `/pr-ready`.

If `/pr-ready` says "Not Ready", fix blockers and run it again.

## Commands (Simple Usage)

### `/feature-workflow <request>`

Use for new backend features or medium changes.

Example:
`/feature-workflow add endpoint to return active workflows by department`

What it does:
- Implements controller/service/DTO changes with minimal scope.
- Adds/updates tests.
- Checks migration posture if persistence changed.

### `/fix-build`

Use when the project fails to compile.

What it does:
- Runs a Release build.
- Finds the first useful compile error chain.
- Applies minimal safe fixes.
- Rebuilds to verify.

### `/fix-test <target>`

Use when a test is failing.

Example:
`/fix-test FluentisCore.Tests.UsuarioTests.CanAddAndRetrieveUsuario`

What it does:
- Reproduces with targeted filters.
- Finds root cause.
- Applies minimal deterministic fix.
- Re-runs targeted test and CI-like tests.

### `/test-one <target>`

Use for quick local test loops.

Example:
`/test-one FluentisCore.Tests.UsuarioTests.CanAddAndRetrieveUsuario`

What it does:
- Tries exact FullyQualifiedName first.
- Falls back to method name fragment if needed.

### `/test-class <class>`

Use when a full test class might be impacted.

Example:
`/test-class FluentisCore.Tests.UsuarioTests`

### `/migration-check`

Use when models, DbContext, relations, or persistence mapping changed.

What it does:
- Checks EF migrations list and drift risk.
- Tells you if a new migration is required.
- Gives exact next EF command.

### `/preflight`

Use before PR or after meaningful code changes.

What it does:
- Tool restore
- Solution restore
- Release build
- CI-like tests excluding integration tests
- EF migration list check

### `/pr-ready`

Use as final quality gate before opening PR.

What it returns:
- `Ready` or `Not Ready`
- Blocking items
- Exact next commands

## Skills (What They Are For)

- `dotnet-ci-parity`: standard CI-like backend validation flow.
- `dotnet-single-test`: deterministic single-test execution patterns.
- `ef-migration-guardian`: migration discipline for EF changes.
- `api-contract-safety`: protect DTO/API contracts and status codes.
- `azure-auth-safe-changes`: safe auth/claims/Graph changes.
- `conventional-commit-guard`: commit message + hook compatibility.

How to use:
- Usually automatic.
- You can explicitly request one: "Use `api-contract-safety` for this change."

## Subagents (Who Handles What)

- `dotnet-build-fixer`: build and compile failures.
- `dotnet-test-triage`: failing tests and deterministic fixes.
- `ef-migration-guardian`: schema/migration drift and safety.
- `api-contract-reviewer`: read-only API/DTO/status-code risk review.
- `pr-readiness-auditor`: final readiness audit.

## Recommended Daily Flows

### New feature

1. `/feature-workflow <request>`
2. `/preflight`
3. `/pr-ready`

### Build is broken

1. `/fix-build`
2. `/preflight`
3. `/pr-ready`

### Test is failing

1. `/test-one <target>`
2. `/fix-test <target>`
3. `/preflight`
4. `/pr-ready`

### Model/EF change

1. Implement change
2. `/migration-check`
3. `/preflight`
4. `/pr-ready`

## Safety Rules (Always On)

- No destructive git commands unless explicitly requested.
- No destructive DB operations unless explicitly requested.
- No secrets/tokens/connection strings in logs or responses.
- Keep API/DTO contracts stable unless explicitly approved.
- Integration tests stay excluded by default in CI-like local checks.

## One-Minute Starter

If you are not sure what to run:

1. `/feature-workflow <what you need>`
2. `/preflight`
3. `/pr-ready`

That path is the safe default for this backend repo.
