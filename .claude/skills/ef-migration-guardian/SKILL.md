---
name: ef-migration-guardian
description: Enforce EF Core migration discipline and schema safety.
allowed-tools: Bash, Read, Grep, Glob
---

Use when models/DbContext/persistence rules change.

Workflow:
1. `dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
2. If needed: `dotnet ef migrations add <MigrationName> --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
3. Optional script: `dotnet ef migrations script --idempotent --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`

Never run destructive DB actions unless explicitly requested.
