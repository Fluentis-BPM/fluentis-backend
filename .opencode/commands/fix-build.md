---
description: Diagnose and fix .NET build failures.
agent: dotnet-build-fixer
subtask: true
---
Diagnose and fix current build failures for this repository.

Run:
!`dotnet build FluentisCore.sln --configuration Release`

If build fails:
- identify the first compile error chain,
- apply minimal safe edits,
- rerun build,
- report exactly what changed and why.

Respect AGENTS.md guardrails and avoid unrelated refactors.
