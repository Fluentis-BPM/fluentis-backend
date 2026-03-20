---
description: Validate EF migration health.
agent: ef-migration-guardian
subtask: true
---
Run EF migration sanity checks and report drift risk.

Execute:
!`dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`

If model changes are present, state whether a new migration is required and propose the exact command.
